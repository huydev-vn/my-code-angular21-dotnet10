using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Security;
using Application.Common.Time;
using Application.Common.Validation;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Contracts;
using Application.Features.Authorization.Errors;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Errors;
using Domain.Authorization;
using FluentValidation;

namespace Application.Features.Authorization.Assignments;

/// <summary>
/// Assigns or updates user↔OU organizational membership.
/// Does not grant permissions or accessible OU scope.
/// Non-privileged actors may only target OUs in their accessible set.
/// </summary>
public sealed class AssignUserOrganizationUnit(
    IAuthorizationAdminStore store,
    IUserAccountService userAccountService,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentActor actor,
    IDelegationAuthorityService delegationAuthority,
    IValidator<AssignUserOrganizationUnitRequest> validator)
{
    public async Task<Result<UserOrganizationUnitResponse>> HandleAsync(
        AssignUserOrganizationUnitRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<UserOrganizationUnitResponse>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        if (await userAccountService.FindByIdAsync(request.UserId, cancellationToken) is null)
        {
            return Result<UserOrganizationUnitResponse>.Failure(IdentityErrors.UserNotFound);
        }

        var unit = await store.FindOrganizationUnitByIdAsync(
            request.OrganizationUnitId,
            cancellationToken);
        if (unit is null)
        {
            return Result<UserOrganizationUnitResponse>.Failure(
                AuthorizationErrors.OrganizationUnitNotFound);
        }

        if (!unit.IsActive)
        {
            return Result<UserOrganizationUnitResponse>.Failure(
                AuthorizationErrors.OrganizationUnitInactive);
        }

        var delegationFailure =
            await delegationAuthority.EnsureCanAssignOrganizationUnitScopeAsync(
                actor.UserId,
                request.OrganizationUnitId,
                cancellationToken);
        if (delegationFailure is not null)
        {
            return Result<UserOrganizationUnitResponse>.Failure(delegationFailure.Error!);
        }

        var existing = await store.FindUserOrganizationUnitAsync(
            request.UserId,
            request.OrganizationUnitId,
            cancellationToken);

        if (request.Relationship == OrganizationUnitRelationship.Primary)
        {
            var activePrimary = await store.FindActivePrimaryUserOrganizationUnitAsync(
                request.UserId,
                cancellationToken);
            if (activePrimary is not null &&
                activePrimary.OrganizationUnitId != request.OrganizationUnitId)
            {
                return Result<UserOrganizationUnitResponse>.Failure(
                    AuthorizationErrors.PrimaryOrganizationUnitAlreadyAssigned);
            }
        }

        var assignedAt = clock.UtcNow;
        if (existing is null)
        {
            var membership = UserOrganizationUnit.Create(
                request.UserId,
                request.OrganizationUnitId,
                request.Relationship,
                assignedAt);
            await store.AddUserOrganizationUnitAsync(membership, cancellationToken);
            await auditor.RecordAsync(
                AuthorizationAuditActions.UserOrganizationUnitAssigned,
                nameof(UserOrganizationUnit),
                request.UserId,
                $"organizationUnitId={request.OrganizationUnitId};relationship={request.Relationship}",
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<UserOrganizationUnitResponse>.Success(membership.ToResponse());
        }

        if (existing.IsActive && existing.Relationship == request.Relationship)
        {
            return Result<UserOrganizationUnitResponse>.Failure(
                AuthorizationErrors.AssignmentAlreadyExists);
        }

        if (!existing.IsActive)
        {
            existing.Reactivate(request.Relationship, assignedAt);
        }
        else
        {
            existing.SetRelationship(request.Relationship);
        }

        await auditor.RecordAsync(
            AuthorizationAuditActions.UserOrganizationUnitAssigned,
            nameof(UserOrganizationUnit),
            request.UserId,
            $"organizationUnitId={request.OrganizationUnitId};relationship={request.Relationship}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<UserOrganizationUnitResponse>.Success(existing.ToResponse());
    }
}

/// <summary>
/// Deactivates user↔OU membership. Does not change permissions or group OU scope.
/// Non-privileged actors may only revoke memberships for OUs in their accessible set.
/// </summary>
public sealed class RevokeUserOrganizationUnit(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    ICurrentActor actor,
    IDelegationAuthorityService delegationAuthority,
    IValidator<RevokeUserOrganizationUnitRequest> validator)
{
    public async Task<Result> HandleAsync(
        RevokeUserOrganizationUnitRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var delegationFailure =
            await delegationAuthority.EnsureCanAssignOrganizationUnitScopeAsync(
                actor.UserId,
                request.OrganizationUnitId,
                cancellationToken);
        if (delegationFailure is not null)
        {
            return delegationFailure;
        }

        var membership = await store.FindUserOrganizationUnitAsync(
            request.UserId,
            request.OrganizationUnitId,
            cancellationToken);
        if (membership is null || !membership.IsActive)
        {
            return Result.Failure(AuthorizationErrors.AssignmentNotFound);
        }

        membership.Deactivate();
        await auditor.RecordAsync(
            AuthorizationAuditActions.UserOrganizationUnitRevoked,
            nameof(UserOrganizationUnit),
            request.UserId,
            $"organizationUnitId={request.OrganizationUnitId}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Lists organizational OU memberships for a user (admin).</summary>
public sealed class ListUserOrganizationUnits(IAuthorizationAdminStore store)
{
    public async Task<IReadOnlyList<UserOrganizationUnitResponse>> HandleAsync(
        Guid userId,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var items = await store.ListUserOrganizationUnitsAsync(
            userId,
            activeOnly,
            cancellationToken);
        return items.Select(item => item.ToResponse()).ToArray();
    }
}
