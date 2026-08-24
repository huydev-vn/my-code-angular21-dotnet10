using Application.Common.Results;
using Application.Common.Security;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Errors;
using Domain.Authorization;

namespace Application.Features.Authorization;

/// <summary>
/// Guards membership and high-risk permission changes for privileged (bootstrap) groups.
/// </summary>
internal static class PrivilegedGroupGuard
{
    public static async Task<Result?> EnsureActorCanManagePrivilegedAsync(
        ICurrentActor actor,
        IAuthorizationAdminStore store,
        CancellationToken cancellationToken)
    {
        if (actor.UserId is null)
        {
            return Result.Failure(AuthorizationErrors.PrivilegedGroupMutationForbidden);
        }

        if (await store.IsMemberOfAnyPrivilegedGroupAsync(actor.UserId.Value, cancellationToken))
        {
            return null;
        }

        return Result.Failure(AuthorizationErrors.PrivilegedGroupMutationForbidden);
    }

    public static async Task<Result<T>?> EnsureActorCanManagePrivilegedAsync<T>(
        ICurrentActor actor,
        IAuthorizationAdminStore store,
        CancellationToken cancellationToken)
    {
        var failure = await EnsureActorCanManagePrivilegedAsync(actor, store, cancellationToken);
        return failure is null
            ? null
            : Result<T>.Failure(failure.Error!);
    }

    public static Result? EnsurePrivilegedPermissionAssignable(UserGroup group, string permissionCode)
    {
        if (!SystemPermissions.IsPrivilegedCatalogPermission(permissionCode))
        {
            return null;
        }

        if (group.IsPrivileged)
        {
            return null;
        }

        return Result.Failure(AuthorizationErrors.PrivilegedPermissionRequiresPrivilegedGroup);
    }

    /// <summary>
    /// Prevents removing the last active member of a privileged group (break-glass lockout).
    /// </summary>
    public static async Task<Result?> EnsureNotLastPrivilegedMemberAsync(
        UserGroup group,
        IAuthorizationAdminStore store,
        CancellationToken cancellationToken)
    {
        if (!group.IsPrivileged || !group.IsActive)
        {
            return null;
        }

        var activeMembers = await store.CountActiveMembersInGroupAsync(
            group.Id,
            cancellationToken);
        if (activeMembers <= 1)
        {
            return Result.Failure(AuthorizationErrors.LastPrivilegedMemberRequired);
        }

        return null;
    }
}
