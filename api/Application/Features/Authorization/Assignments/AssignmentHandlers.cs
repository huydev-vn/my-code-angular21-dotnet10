using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Security;
using Application.Common.Time;
using Application.Common.Validation;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Errors;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Errors;
using Domain.Authorization;
using FluentValidation;

namespace Application.Features.Authorization.Assignments;

public sealed class AssignGroupPermission(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentActor actor,
    IValidator<AssignGroupPermissionRequest> validator)
{
    public async Task<Result> HandleAsync(
        AssignGroupPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var group = await store.FindGroupByIdAsync(request.GroupId, cancellationToken);
        if (group is null)
        {
            return Result.Failure(AuthorizationErrors.GroupNotFound);
        }

        if (!group.IsActive)
        {
            return Result.Failure(AuthorizationErrors.GroupInactive);
        }

        var permission = await store.FindPermissionByIdAsync(
            request.PermissionId,
            cancellationToken);
        if (permission is null)
        {
            return Result.Failure(AuthorizationErrors.PermissionNotFound);
        }

        if (!permission.IsActive)
        {
            return Result.Failure(AuthorizationErrors.PermissionInactive);
        }

        var privilegedPermissionFailure =
            PrivilegedGroupGuard.EnsurePrivilegedPermissionAssignable(group, permission.Code);
        if (privilegedPermissionFailure is not null)
        {
            return privilegedPermissionFailure;
        }

        if (group.IsPrivileged ||
            SystemPermissions.IsPrivilegedCatalogPermission(permission.Code))
        {
            var privilegedActorFailure =
                await PrivilegedGroupGuard.EnsureActorCanManagePrivilegedAsync(
                    actor,
                    store,
                    cancellationToken);
            if (privilegedActorFailure is not null)
            {
                return privilegedActorFailure;
            }
        }

        if (await store.GroupPermissionExistsAsync(
                request.GroupId,
                request.PermissionId,
                cancellationToken))
        {
            return Result.Failure(AuthorizationErrors.AssignmentAlreadyExists);
        }

        var assignedAt = clock.UtcNow;
        await store.AddGroupPermissionAsync(
            GroupPermission.Create(request.GroupId, request.PermissionId, assignedAt),
            cancellationToken);
        await auditor.RecordAsync(
            AuthorizationAuditActions.GroupPermissionAssigned,
            nameof(GroupPermission),
            request.GroupId,
            $"permissionId={request.PermissionId}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class AssignUserToGroup(
    IAuthorizationAdminStore store,
    IUserAccountService userAccountService,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentActor actor,
    IValidator<AssignUserToGroupRequest> validator)
{
    public async Task<Result> HandleAsync(
        AssignUserToGroupRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var group = await store.FindGroupByIdAsync(request.GroupId, cancellationToken);
        if (group is null)
        {
            return Result.Failure(AuthorizationErrors.GroupNotFound);
        }

        if (!group.IsActive)
        {
            return Result.Failure(AuthorizationErrors.GroupInactive);
        }

        if (group.IsPrivileged)
        {
            var privilegedActorFailure =
                await PrivilegedGroupGuard.EnsureActorCanManagePrivilegedAsync(
                    actor,
                    store,
                    cancellationToken);
            if (privilegedActorFailure is not null)
            {
                return privilegedActorFailure;
            }
        }

        if (await userAccountService.FindByIdAsync(request.UserId, cancellationToken) is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        if (await store.UserGroupMembershipExistsAsync(
                request.UserId,
                request.GroupId,
                cancellationToken))
        {
            return Result.Failure(AuthorizationErrors.AssignmentAlreadyExists);
        }

        var assignedAt = clock.UtcNow;
        await store.AddUserGroupMembershipAsync(
            UserGroupMembership.Create(request.UserId, request.GroupId, assignedAt),
            cancellationToken);
        await auditor.RecordAsync(
            AuthorizationAuditActions.UserGroupAssigned,
            nameof(UserGroupMembership),
            request.GroupId,
            $"userId={request.UserId}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class AssignGroupOrganizationUnit(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    IClock clock,
    IValidator<AssignGroupOrganizationUnitRequest> validator)
{
    public async Task<Result> HandleAsync(
        AssignGroupOrganizationUnitRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var group = await store.FindGroupByIdAsync(request.GroupId, cancellationToken);
        if (group is null)
        {
            return Result.Failure(AuthorizationErrors.GroupNotFound);
        }

        if (!group.IsActive)
        {
            return Result.Failure(AuthorizationErrors.GroupInactive);
        }

        // Privileged groups remain global; OU scope is for delegated resource access only.
        if (group.IsPrivileged)
        {
            return Result.Failure(AuthorizationErrors.PrivilegedGroupOrganizationUnitForbidden);
        }

        var unit = await store.FindOrganizationUnitByIdAsync(
            request.OrganizationUnitId,
            cancellationToken);
        if (unit is null)
        {
            return Result.Failure(AuthorizationErrors.OrganizationUnitNotFound);
        }

        if (!unit.IsActive)
        {
            return Result.Failure(AuthorizationErrors.OrganizationUnitInactive);
        }

        if (await store.GroupOrganizationUnitExistsAsync(
                request.GroupId,
                request.OrganizationUnitId,
                cancellationToken))
        {
            return Result.Failure(AuthorizationErrors.AssignmentAlreadyExists);
        }

        var assignedAt = clock.UtcNow;
        await store.AddGroupOrganizationUnitAsync(
            GroupOrganizationUnit.Create(
                request.GroupId,
                request.OrganizationUnitId,
                assignedAt),
            cancellationToken);
        await auditor.RecordAsync(
            AuthorizationAuditActions.GroupOrganizationUnitAssigned,
            nameof(GroupOrganizationUnit),
            request.GroupId,
            $"organizationUnitId={request.OrganizationUnitId}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class RevokeGroupPermission(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    ICurrentActor actor,
    IValidator<RevokeGroupPermissionRequest> validator)
{
    public async Task<Result> HandleAsync(
        RevokeGroupPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var group = await store.FindGroupByIdAsync(request.GroupId, cancellationToken);
        if (group is null)
        {
            return Result.Failure(AuthorizationErrors.GroupNotFound);
        }

        var permission = await store.FindPermissionByIdAsync(
            request.PermissionId,
            cancellationToken);
        if (permission is not null &&
            (group.IsPrivileged ||
             SystemPermissions.IsPrivilegedCatalogPermission(permission.Code)))
        {
            var privilegedActorFailure =
                await PrivilegedGroupGuard.EnsureActorCanManagePrivilegedAsync(
                    actor,
                    store,
                    cancellationToken);
            if (privilegedActorFailure is not null)
            {
                return privilegedActorFailure;
            }
        }

        // Tracked delete + audit share one SaveChanges so they cannot diverge.
        var removed = await store.RemoveGroupPermissionAsync(
            request.GroupId,
            request.PermissionId,
            cancellationToken);
        if (!removed)
        {
            return Result.Failure(AuthorizationErrors.AssignmentNotFound);
        }

        await auditor.RecordAsync(
            AuthorizationAuditActions.GroupPermissionRevoked,
            nameof(GroupPermission),
            request.GroupId,
            $"permissionId={request.PermissionId}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class RevokeUserFromGroup(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    ICurrentActor actor,
    IValidator<RevokeUserFromGroupRequest> validator)
{
    public async Task<Result> HandleAsync(
        RevokeUserFromGroupRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var group = await store.FindGroupByIdAsync(request.GroupId, cancellationToken);
        if (group is null)
        {
            return Result.Failure(AuthorizationErrors.GroupNotFound);
        }

        if (group.IsPrivileged)
        {
            var privilegedActorFailure =
                await PrivilegedGroupGuard.EnsureActorCanManagePrivilegedAsync(
                    actor,
                    store,
                    cancellationToken);
            if (privilegedActorFailure is not null)
            {
                return privilegedActorFailure;
            }

            var lastMemberFailure =
                await PrivilegedGroupGuard.EnsureNotLastPrivilegedMemberAsync(
                    group,
                    store,
                    cancellationToken);
            if (lastMemberFailure is not null)
            {
                return lastMemberFailure;
            }
        }

        var removed = await store.RemoveUserGroupMembershipAsync(
            request.UserId,
            request.GroupId,
            cancellationToken);
        if (!removed)
        {
            return Result.Failure(AuthorizationErrors.AssignmentNotFound);
        }

        await auditor.RecordAsync(
            AuthorizationAuditActions.UserGroupRevoked,
            nameof(UserGroupMembership),
            request.GroupId,
            $"userId={request.UserId}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class RevokeGroupOrganizationUnit(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    IValidator<RevokeGroupOrganizationUnitRequest> validator)
{
    public async Task<Result> HandleAsync(
        RevokeGroupOrganizationUnitRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var removed = await store.RemoveGroupOrganizationUnitAsync(
            request.GroupId,
            request.OrganizationUnitId,
            cancellationToken);
        if (!removed)
        {
            return Result.Failure(AuthorizationErrors.AssignmentNotFound);
        }

        await auditor.RecordAsync(
            AuthorizationAuditActions.GroupOrganizationUnitRevoked,
            nameof(GroupOrganizationUnit),
            request.GroupId,
            $"organizationUnitId={request.OrganizationUnitId}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
