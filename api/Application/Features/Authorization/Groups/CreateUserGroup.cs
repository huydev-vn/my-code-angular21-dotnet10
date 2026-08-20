using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Time;
using Application.Common.Validation;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Errors;
using Domain.Authorization;
using FluentValidation;

namespace Application.Features.Authorization.Groups;

public sealed class CreateUserGroup(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    IClock clock,
    IValidator<CreateUserGroupRequest> validator)
{
    public async Task<Result<Contracts.UserGroupResponse>> HandleAsync(
        CreateUserGroupRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<Contracts.UserGroupResponse>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        if (await store.FindGroupByNameAsync(request.Name.Trim(), cancellationToken) is not null)
        {
            return Result<Contracts.UserGroupResponse>.Failure(
                AuthorizationErrors.GroupNameTaken);
        }

        var group = UserGroup.Create(request.Name, request.Description, clock.UtcNow);
        await store.AddGroupAsync(group, cancellationToken);
        await auditor.RecordAsync(
            AuthorizationAuditActions.GroupCreated,
            nameof(UserGroup),
            group.Id,
            $"name={group.Name}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Contracts.UserGroupResponse>.Success(group.ToResponse());
    }
}
