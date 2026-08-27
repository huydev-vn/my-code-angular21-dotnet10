using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Validation;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Errors;
using Domain.Authorization;
using FluentValidation;

namespace Application.Features.Authorization.Permissions;

/// <summary>Payload for updating permission catalog metadata (Code remains immutable).</summary>
public sealed record UpdatePermissionDefinitionRequest(
    string Name,
    string? Module,
    string? Action,
    string? Resource,
    PermissionScopeMode ScopeMode,
    PermissionRiskLevel RiskLevel);

internal sealed class UpdatePermissionDefinitionValidator
    : AbstractValidator<UpdatePermissionDefinitionRequest>
{
    public UpdatePermissionDefinitionValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(256);
        RuleFor(request => request.Module).MaximumLength(128);
        RuleFor(request => request.Action).MaximumLength(128);
        RuleFor(request => request.Resource).MaximumLength(128);
        RuleFor(request => request.ScopeMode).IsInEnum();
        RuleFor(request => request.RiskLevel).IsInEnum();
    }
}

public sealed class UpdatePermissionDefinition(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    IValidator<UpdatePermissionDefinitionRequest> validator)
{
    public async Task<Result<Contracts.PermissionDefinitionResponse>> HandleAsync(
        Guid id,
        UpdatePermissionDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<Contracts.PermissionDefinitionResponse>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var permission = await store.FindPermissionByIdAsync(id, cancellationToken);
        if (permission is null)
        {
            return Result<Contracts.PermissionDefinitionResponse>.Failure(
                AuthorizationErrors.PermissionNotFound);
        }

        permission.Update(
            request.Name,
            request.Module,
            request.Action,
            request.Resource,
            request.ScopeMode,
            request.RiskLevel);
        await auditor.RecordAsync(
            AuthorizationAuditActions.PermissionUpdated,
            nameof(PermissionDefinition),
            permission.Id,
            $"code={permission.Code}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Contracts.PermissionDefinitionResponse>.Success(permission.ToResponse());
    }
}

public sealed class SetPermissionDefinitionActive(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<Contracts.PermissionDefinitionResponse>> HandleAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var permission = await store.FindPermissionByIdAsync(id, cancellationToken);
        if (permission is null)
        {
            return Result<Contracts.PermissionDefinitionResponse>.Failure(
                AuthorizationErrors.PermissionNotFound);
        }

        if (isActive)
        {
            permission.Activate();
        }
        else
        {
            permission.Deactivate();
        }

        await auditor.RecordAsync(
            isActive
                ? AuthorizationAuditActions.PermissionActivated
                : AuthorizationAuditActions.PermissionDeactivated,
            nameof(PermissionDefinition),
            permission.Id,
            $"code={permission.Code}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Contracts.PermissionDefinitionResponse>.Success(permission.ToResponse());
    }
}
