using System.Text.Json;
using Application.Features.Authorization.Abstractions;
using Domain.Authorization;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Authorization;

/// <summary>
/// Resolves authorization context from PostgreSQL with an optional Redis-backed
/// (or distributed-memory) cache keyed by user + shared authorization version.
/// On cache read failures, falls back to PostgreSQL instead of serving stale data.
/// </summary>
internal sealed class AuthorizationDecisionService(
    AppDbContext dbContext,
    IDistributedCache distributedCache,
    IAuthorizationStateVersion stateVersion,
    IOptions<AuthorizationCacheOptions> cacheOptions,
    ILogger<AuthorizationDecisionService> logger)
    : IAuthorizationDecisionService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private Guid? _cachedUserId;
    private long _cachedVersion = -1;
    private UserAuthorizationContext? _cachedContext;
    private bool _cachedMissingUser;

    public async Task<UserAuthorizationContext?> GetContextAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var version = await stateVersion.GetCurrentAsync(cancellationToken);
        if (version is null)
        {
            // Shared version store unavailable — skip scoped and distributed cache
            // so we never serve a stale entry keyed under a fallback version.
            logger.LogWarning(
                "Authorization version unavailable; loading context from PostgreSQL without cache.");
            return await LoadContextAsync(userId, cancellationToken);
        }

        if (_cachedUserId == userId && _cachedVersion == version.Value)
        {
            return _cachedMissingUser ? null : _cachedContext;
        }

        var cacheKey = $"authz-ctx:{userId}:{version.Value}";
        var cached = await TryGetFromDistributedCacheAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            StoreScoped(userId, version.Value, cached, missing: false);
            return cached;
        }

        var context = await LoadContextAsync(userId, cancellationToken);
        StoreScoped(userId, version.Value, context, missing: context is null);

        if (context is not null)
        {
            await TrySetDistributedCacheAsync(cacheKey, context, cancellationToken);
        }

        return context;
    }

    public async Task<AuthorizationDecision> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        var context = await GetContextAsync(userId, cancellationToken);
        if (context is null)
        {
            return AuthorizationDecision.Unauthenticated();
        }

        return PermissionMatcher.Grants(context.PermissionCodes, permissionCode)
            ? AuthorizationDecision.Allowed()
            : AuthorizationDecision.MissingPermission();
    }

    public async Task<AuthorizationDecision> HasPermissionOnUnitAsync(
        Guid userId,
        string permissionCode,
        Guid organizationUnitId,
        CancellationToken cancellationToken)
    {
        var permissionDecision = await HasPermissionAsync(
            userId,
            permissionCode,
            cancellationToken);

        if (!permissionDecision.IsAllowed)
        {
            return permissionDecision;
        }

        var canAccessUnit = await CanAccessOrganizationUnitAsync(
            userId,
            organizationUnitId,
            cancellationToken);

        return canAccessUnit
            ? AuthorizationDecision.Allowed()
            : AuthorizationDecision.OutsideUnitScope();
    }

    public async Task<bool> CanAccessOrganizationUnitAsync(
        Guid userId,
        Guid organizationUnitId,
        CancellationToken cancellationToken)
    {
        var context = await GetContextAsync(userId, cancellationToken);
        return context?.AccessibleOrganizationUnitIds.Contains(organizationUnitId) == true;
    }

    private async Task<UserAuthorizationContext?> TryGetFromDistributedCacheAsync(
        string cacheKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await distributedCache.GetAsync(cacheKey, cancellationToken);
            if (bytes is null || bytes.Length == 0)
            {
                return null;
            }

            var deserialized = JsonSerializer.Deserialize<UserAuthorizationContext>(
                bytes,
                SerializerOptions);
            return deserialized?.WithNormalizedScopes();
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Authorization cache read failed for {CacheKey}; falling back to PostgreSQL.",
                cacheKey);
            return null;
        }
    }

    private async Task TrySetDistributedCacheAsync(
        string cacheKey,
        UserAuthorizationContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var ttlSeconds = Math.Clamp(cacheOptions.Value.AbsoluteExpirationSeconds, 1, 300);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(context, SerializerOptions);
            await distributedCache.SetAsync(
                cacheKey,
                bytes,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds)
                },
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Authorization cache write failed for {CacheKey}; continuing without cache.",
                cacheKey);
        }
    }

    private void StoreScoped(
        Guid userId,
        long version,
        UserAuthorizationContext? context,
        bool missing)
    {
        _cachedUserId = userId;
        _cachedVersion = version;
        _cachedMissingUser = missing;
        _cachedContext = context;
    }

    private async Task<UserAuthorizationContext?> LoadContextAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, cancellationToken);

        if (!userExists)
        {
            return null;
        }

        // Agent C: UserOrganizationUnit is organizational membership metadata only.
        // Do not merge it into AccessibleOrganizationUnitIds — group→OU scope remains the sole source.

        var memberships = await dbContext.UserGroupMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId)
            .Select(membership => membership.GroupId)
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0)
        {
            return new UserAuthorizationContext(
                userId,
                [],
                [],
                [],
                UserAuthorizationContext.EmptyPermissionScopes);
        }

        var groups = await dbContext.UserGroups
            .AsNoTracking()
            .Where(group => memberships.Contains(group.Id) && group.IsActive)
            .Select(group => new { group.Id, group.Name })
            .ToListAsync(cancellationToken);

        var activeGroupIds = groups.Select(group => group.Id).ToArray();
        if (activeGroupIds.Length == 0)
        {
            return new UserAuthorizationContext(
                userId,
                [],
                [],
                [],
                UserAuthorizationContext.EmptyPermissionScopes);
        }

        var permissionRows = await dbContext.GroupPermissions
            .AsNoTracking()
            .Where(assignment => activeGroupIds.Contains(assignment.GroupId))
            .Join(
                dbContext.PermissionDefinitions.AsNoTracking()
                    .Where(permission => permission.IsActive),
                assignment => assignment.PermissionId,
                permission => permission.Id,
                (_, permission) => new { permission.Code, permission.ScopeMode })
            .Distinct()
            .ToListAsync(cancellationToken);

        var permissionScopeByCode = permissionRows
            .GroupBy(row => row.Code, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First().ScopeMode,
                StringComparer.Ordinal);

        var permissions = permissionScopeByCode.Keys
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        var scopeRoots = await dbContext.GroupOrganizationUnits
            .AsNoTracking()
            .Where(assignment => activeGroupIds.Contains(assignment.GroupId))
            .Select(assignment => assignment.OrganizationUnitId)
            .Distinct()
            .ToListAsync(cancellationToken);

        IReadOnlyList<Guid> accessibleUnitIds = [];
        if (scopeRoots.Count > 0)
        {
            accessibleUnitIds = await OrganizationUnitQueries.CollectAccessibleIdsAsync(
                dbContext,
                scopeRoots,
                activeOnly: true,
                cancellationToken);
        }

        return new UserAuthorizationContext(
            userId,
            groups.Select(group => group.Name).OrderBy(name => name).ToArray(),
            permissions,
            accessibleUnitIds,
            permissionScopeByCode);
    }
}
