using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Validation;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Errors;
using Domain.Authorization;
using FluentValidation;

namespace Application.Features.Authorization.OrganizationUnits;

public sealed record UpdateOrganizationUnitRequest(string Name, string Code);

internal sealed class UpdateOrganizationUnitValidator
    : AbstractValidator<UpdateOrganizationUnitRequest>
{
    public UpdateOrganizationUnitValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(256);
        RuleFor(request => request.Code).NotEmpty().MaximumLength(64);
    }
}

public sealed class UpdateOrganizationUnit(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    IValidator<UpdateOrganizationUnitRequest> validator)
{
    public async Task<Result<Contracts.OrganizationUnitResponse>> HandleAsync(
        Guid id,
        UpdateOrganizationUnitRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<Contracts.OrganizationUnitResponse>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var unit = await store.FindOrganizationUnitByIdAsync(id, cancellationToken);
        if (unit is null)
        {
            return Result<Contracts.OrganizationUnitResponse>.Failure(
                AuthorizationErrors.OrganizationUnitNotFound);
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var existing = await store.FindOrganizationUnitByCodeAsync(
            normalizedCode,
            cancellationToken);
        if (existing is not null && existing.Id != id)
        {
            return Result<Contracts.OrganizationUnitResponse>.Failure(
                AuthorizationErrors.OrganizationUnitCodeTaken);
        }

        unit.Update(request.Name, request.Code);
        await auditor.RecordAsync(
            AuthorizationAuditActions.OrganizationUnitUpdated,
            nameof(OrganizationUnit),
            unit.Id,
            $"code={unit.Code}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Contracts.OrganizationUnitResponse>.Success(unit.ToResponse());
    }
}

public sealed class SetOrganizationUnitActive(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<Contracts.OrganizationUnitResponse>> HandleAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var unit = await store.FindOrganizationUnitByIdAsync(id, cancellationToken);
        if (unit is null)
        {
            return Result<Contracts.OrganizationUnitResponse>.Failure(
                AuthorizationErrors.OrganizationUnitNotFound);
        }

        if (isActive)
        {
            unit.Activate();
        }
        else
        {
            unit.Deactivate();
        }

        await auditor.RecordAsync(
            isActive
                ? AuthorizationAuditActions.OrganizationUnitActivated
                : AuthorizationAuditActions.OrganizationUnitDeactivated,
            nameof(OrganizationUnit),
            unit.Id,
            $"code={unit.Code}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Contracts.OrganizationUnitResponse>.Success(unit.ToResponse());
    }
}
