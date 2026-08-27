using Application.Common.Results;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Errors;
using Domain.Authorization;

namespace Application.Features.Authorization;

/// <summary>
/// Enforces delegated-admin grant containment: subset of the actor's effective permissions
/// and accessible organization units, excluding privileged/Critical escalation.
/// </summary>
public sealed class DelegationAuthorityService(
    IAuthorizationDecisionService decisionService,
    IAuthorizationAdminStore store)
    : IDelegationAuthorityService
{
    public async Task<Result?> EnsureCanDelegatePermissionAsync(
        Guid? actorUserId,
        PermissionDefinition permission,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(permission);

        var authority = await ResolveAsync(actorUserId, cancellationToken);
        if (authority is null)
        {
            return Result.Failure(AuthorizationErrors.DelegationPermissionForbidden);
        }

        if (authority.IsPrivileged)
        {
            return null;
        }

        if (SystemPermissions.IsPrivilegedCatalogPermission(permission))
        {
            return Result.Failure(AuthorizationErrors.DelegationPermissionForbidden);
        }

        if (!authority.PermissionCodes.Contains(permission.Code))
        {
            return Result.Failure(AuthorizationErrors.DelegationPermissionForbidden);
        }

        return null;
    }

    public async Task<Result?> EnsureCanAssignOrganizationUnitScopeAsync(
        Guid? actorUserId,
        Guid organizationUnitId,
        CancellationToken cancellationToken)
    {
        var authority = await ResolveAsync(actorUserId, cancellationToken);
        if (authority is null)
        {
            return Result.Failure(AuthorizationErrors.DelegationScopeForbidden);
        }

        if (authority.IsPrivileged)
        {
            return null;
        }

        if (authority.AccessibleOrganizationUnitIds.Count == 0 ||
            !authority.AccessibleOrganizationUnitIds.Contains(organizationUnitId))
        {
            return Result.Failure(AuthorizationErrors.DelegationScopeForbidden);
        }

        return null;
    }

    public async Task<Result?> EnsureCanManageGroupUserAssignmentAsync(
        Guid? actorUserId,
        UserGroup group,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(group);

        var authority = await ResolveAsync(actorUserId, cancellationToken);
        if (authority is null)
        {
            return Result.Failure(AuthorizationErrors.DelegationGroupForbidden);
        }

        if (authority.IsPrivileged)
        {
            return null;
        }

        if (group.IsPrivileged)
        {
            return Result.Failure(AuthorizationErrors.PrivilegedGroupMutationForbidden);
        }

        var groupOuRoots = await store.ListGroupOrganizationUnitIdsAsync(
            group.Id,
            cancellationToken);
        if (groupOuRoots.Count == 0)
        {
            return Result.Failure(AuthorizationErrors.DelegationGroupForbidden);
        }

        foreach (var rootId in groupOuRoots)
        {
            if (!authority.AccessibleOrganizationUnitIds.Contains(rootId))
            {
                return Result.Failure(AuthorizationErrors.DelegationGroupForbidden);
            }
        }

        return null;
    }

    private async Task<ActorAuthority?> ResolveAsync(
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        if (actorUserId is null)
        {
            return null;
        }

        var isPrivileged = await store.IsMemberOfAnyPrivilegedGroupAsync(
            actorUserId.Value,
            cancellationToken);
        if (isPrivileged)
        {
            return new ActorAuthority(
                IsPrivileged: true,
                PermissionCodes: new HashSet<string>(StringComparer.Ordinal),
                AccessibleOrganizationUnitIds: new HashSet<Guid>());
        }

        var context = await decisionService.GetContextAsync(actorUserId.Value, cancellationToken);
        if (context is null)
        {
            return new ActorAuthority(
                IsPrivileged: false,
                PermissionCodes: new HashSet<string>(StringComparer.Ordinal),
                AccessibleOrganizationUnitIds: new HashSet<Guid>());
        }

        return new ActorAuthority(
            IsPrivileged: false,
            PermissionCodes: context.PermissionCodes.ToHashSet(StringComparer.Ordinal),
            AccessibleOrganizationUnitIds: context.AccessibleOrganizationUnitIds.ToHashSet());
    }

    private sealed record ActorAuthority(
        bool IsPrivileged,
        HashSet<string> PermissionCodes,
        HashSet<Guid> AccessibleOrganizationUnitIds);
}
