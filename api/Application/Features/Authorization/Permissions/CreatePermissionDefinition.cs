using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Time;
using Application.Common.Validation;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Errors;
using Domain.Authorization;
using FluentValidation;

namespace Application.Features.Authorization.Permissions;

public sealed class CreatePermissionDefinition(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    IClock clock,
    IValidator<CreatePermissionDefinitionRequest> validator)
{
    public async Task<Result<Contracts.PermissionDefinitionResponse>> HandleAsync(
        CreatePermissionDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<Contracts.PermissionDefinitionResponse>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var normalizedCode = PermissionMatcher.Normalize(request.Code);
        if (await store.FindPermissionByCodeAsync(normalizedCode, cancellationToken) is not null)
        {
            return Result<Contracts.PermissionDefinitionResponse>.Failure(
                AuthorizationErrors.PermissionCodeTaken);
        }

        var permission = PermissionDefinition.Create(
            normalizedCode,
            request.Name,
            request.Module,
            request.Action,
            clock.UtcNow);

        await store.AddPermissionAsync(permission, cancellationToken);
        await auditor.RecordAsync(
            AuthorizationAuditActions.PermissionCreated,
            nameof(PermissionDefinition),
            permission.Id,
            $"code={permission.Code}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Contracts.PermissionDefinitionResponse>.Success(
            permission.ToResponse());
    }
}
