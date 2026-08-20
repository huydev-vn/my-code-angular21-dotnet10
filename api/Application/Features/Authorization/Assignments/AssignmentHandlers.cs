using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Time;
using Application.Common.Validation;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Errors;
using Domain.Authorization;
using FluentValidation;

namespace Application.Features.Authorization.Assignments;

public sealed class AssignGroupPermission(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    IClock clock,
    IValidator<AssignGroupPermissionRequest> validator)
{
    public async Task<Result<Contracts.AssignmentResponse>> HandleAsync(
        AssignGroupPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<Contracts.AssignmentResponse>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        if (await store.FindGroupByIdAsync(request.GroupId, cancellationToken) is null)
        {
            return Result<Contracts.AssignmentResponse>.Failure(AuthorizationErrors.GroupNotFound);
        }

        if (await store.FindPermissionByIdAsync(request.PermissionId, cancellationToken) is null)
        {
            return Result<Contracts.AssignmentResponse>.Failure(
                AuthorizationErrors.PermissionNotFound);
        }

        if (await store.GroupPermissionExistsAsync(
                request.GroupId,
                request.PermissionId,
                cancellationToken))
        {
            return Result<Contracts.AssignmentResponse>.Failure(
                AuthorizationErrors.AssignmentAlreadyExists);
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

        return Result<Contracts.AssignmentResponse>.Success(
            new Contracts.AssignmentResponse(request.GroupId, request.PermissionId, assignedAt));
    }
}

public sealed class AssignUserToGroup(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    IClock clock,
    IValidator<AssignUserToGroupRequest> validator)
{
    public async Task<Result<Contracts.AssignmentResponse>> HandleAsync(
        AssignUserToGroupRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<Contracts.AssignmentResponse>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        if (await store.FindGroupByIdAsync(request.GroupId, cancellationToken) is null)
        {
            return Result<Contracts.AssignmentResponse>.Failure(AuthorizationErrors.GroupNotFound);
        }

        if (await store.UserGroupMembershipExistsAsync(
                request.UserId,
                request.GroupId,
                cancellationToken))
        {
            return Result<Contracts.AssignmentResponse>.Failure(
                AuthorizationErrors.AssignmentAlreadyExists);
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

        return Result<Contracts.AssignmentResponse>.Success(
            new Contracts.AssignmentResponse(request.GroupId, request.UserId, assignedAt));
    }
}

public sealed class AssignGroupOrganizationUnit(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    IClock clock,
    IValidator<AssignGroupOrganizationUnitRequest> validator)
{
    public async Task<Result<Contracts.AssignmentResponse>> HandleAsync(
        AssignGroupOrganizationUnitRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<Contracts.AssignmentResponse>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        if (await store.FindGroupByIdAsync(request.GroupId, cancellationToken) is null)
        {
            return Result<Contracts.AssignmentResponse>.Failure(AuthorizationErrors.GroupNotFound);
        }

        if (await store.FindOrganizationUnitByIdAsync(
                request.OrganizationUnitId,
                cancellationToken) is null)
        {
            return Result<Contracts.AssignmentResponse>.Failure(
                AuthorizationErrors.OrganizationUnitNotFound);
        }

        if (await store.GroupOrganizationUnitExistsAsync(
                request.GroupId,
                request.OrganizationUnitId,
                cancellationToken))
        {
            return Result<Contracts.AssignmentResponse>.Failure(
                AuthorizationErrors.AssignmentAlreadyExists);
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

        return Result<Contracts.AssignmentResponse>.Success(
            new Contracts.AssignmentResponse(
                request.GroupId,
                request.OrganizationUnitId,
                assignedAt));
    }
}
