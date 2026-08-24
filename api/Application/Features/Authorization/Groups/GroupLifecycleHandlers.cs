using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Security;
using Application.Common.Validation;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Errors;
using Domain.Authorization;
using FluentValidation;

namespace Application.Features.Authorization.Groups;

public sealed record UpdateUserGroupRequest(string Name, string? Description);

internal sealed class UpdateUserGroupValidator : AbstractValidator<UpdateUserGroupRequest>
{
    public UpdateUserGroupValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(256);
        RuleFor(request => request.Description).MaximumLength(1024);
    }
}

public sealed class UpdateUserGroup(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    ICurrentActor actor,
    IValidator<UpdateUserGroupRequest> validator)
{
    public async Task<Result<Contracts.UserGroupResponse>> HandleAsync(
        Guid id,
        UpdateUserGroupRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<Contracts.UserGroupResponse>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var group = await store.FindGroupByIdAsync(id, cancellationToken);
        if (group is null)
        {
            return Result<Contracts.UserGroupResponse>.Failure(AuthorizationErrors.GroupNotFound);
        }

        if (group.IsPrivileged)
        {
            var privilegedActorFailure =
                await PrivilegedGroupGuard.EnsureActorCanManagePrivilegedAsync<Contracts.UserGroupResponse>(
                    actor,
                    store,
                    cancellationToken);
            if (privilegedActorFailure is not null)
            {
                return privilegedActorFailure;
            }
        }

        var existing = await store.FindGroupByNameAsync(request.Name.Trim(), cancellationToken);
        if (existing is not null && existing.Id != id)
        {
            return Result<Contracts.UserGroupResponse>.Failure(AuthorizationErrors.GroupNameTaken);
        }

        group.Update(request.Name, request.Description);
        await auditor.RecordAsync(
            AuthorizationAuditActions.GroupUpdated,
            nameof(UserGroup),
            group.Id,
            $"name={group.Name}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Contracts.UserGroupResponse>.Success(group.ToResponse());
    }
}

public sealed class SetUserGroupActive(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    ICurrentActor actor)
{
    public async Task<Result<Contracts.UserGroupResponse>> HandleAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var group = await store.FindGroupByIdAsync(id, cancellationToken);
        if (group is null)
        {
            return Result<Contracts.UserGroupResponse>.Failure(AuthorizationErrors.GroupNotFound);
        }

        if (group.IsPrivileged && !isActive)
        {
            return Result<Contracts.UserGroupResponse>.Failure(
                AuthorizationErrors.PrivilegedGroupDeactivateForbidden);
        }

        if (group.IsPrivileged)
        {
            var privilegedActorFailure =
                await PrivilegedGroupGuard.EnsureActorCanManagePrivilegedAsync<Contracts.UserGroupResponse>(
                    actor,
                    store,
                    cancellationToken);
            if (privilegedActorFailure is not null)
            {
                return privilegedActorFailure;
            }
        }

        if (isActive)
        {
            group.Activate();
        }
        else
        {
            group.Deactivate();
        }

        await auditor.RecordAsync(
            isActive
                ? AuthorizationAuditActions.GroupActivated
                : AuthorizationAuditActions.GroupDeactivated,
            nameof(UserGroup),
            group.Id,
            $"name={group.Name}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Contracts.UserGroupResponse>.Success(group.ToResponse());
    }
}
