using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Time;
using Application.Common.Validation;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Errors;
using Domain.Authorization;
using FluentValidation;

namespace Application.Features.Authorization.OrganizationUnits;

public sealed class CreateOrganizationUnit(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    IClock clock,
    IValidator<CreateOrganizationUnitRequest> validator)
{
    public async Task<Result<Contracts.OrganizationUnitResponse>> HandleAsync(
        CreateOrganizationUnitRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<Contracts.OrganizationUnitResponse>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await store.FindOrganizationUnitByCodeAsync(normalizedCode, cancellationToken) is not null)
        {
            return Result<Contracts.OrganizationUnitResponse>.Failure(
                AuthorizationErrors.OrganizationUnitCodeTaken);
        }

        if (request.ParentId is Guid parentId &&
            await store.FindOrganizationUnitByIdAsync(parentId, cancellationToken) is null)
        {
            return Result<Contracts.OrganizationUnitResponse>.Failure(
                AuthorizationErrors.ParentOrganizationUnitNotFound);
        }

        var unit = OrganizationUnit.Create(
            request.Name,
            normalizedCode,
            request.ParentId,
            clock.UtcNow);

        await store.AddOrganizationUnitAsync(unit, cancellationToken);
        await auditor.RecordAsync(
            AuthorizationAuditActions.OrganizationUnitCreated,
            nameof(OrganizationUnit),
            unit.Id,
            $"code={unit.Code}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Contracts.OrganizationUnitResponse>.Success(unit.ToResponse());
    }
}
