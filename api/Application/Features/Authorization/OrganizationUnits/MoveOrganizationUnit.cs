using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Security;
using Application.Common.Validation;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Errors;
using Domain.Authorization;
using FluentValidation;

namespace Application.Features.Authorization.OrganizationUnits;

/// <summary>Moves an organization unit under a new parent (null = root).</summary>
public sealed record MoveOrganizationUnitRequest(Guid? ParentId);

internal sealed class MoveOrganizationUnitValidator
    : AbstractValidator<MoveOrganizationUnitRequest>
{
    public MoveOrganizationUnitValidator()
    {
        RuleFor(request => request.ParentId)
            .NotEmpty()
            .When(request => request.ParentId.HasValue);
    }
}

/// <summary>
/// Relocates an organization unit in the tree. Requires organization-units.write at HTTP;
/// non-privileged actors are further limited to accessible OUs for both the unit and new parent.
/// </summary>
public sealed class MoveOrganizationUnit(
    IAuthorizationAdminStore store,
    IAuthorizationAuditor auditor,
    IUnitOfWork unitOfWork,
    ICurrentActor actor,
    IDelegationAuthorityService delegationAuthority,
    IValidator<MoveOrganizationUnitRequest> validator)
{
    public async Task<Result<Contracts.OrganizationUnitResponse>> HandleAsync(
        Guid organizationUnitId,
        MoveOrganizationUnitRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<Contracts.OrganizationUnitResponse>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var unit = await store.FindOrganizationUnitByIdAsync(
            organizationUnitId,
            cancellationToken);
        if (unit is null)
        {
            return Result<Contracts.OrganizationUnitResponse>.Failure(
                AuthorizationErrors.OrganizationUnitNotFound);
        }

        var sourceScopeFailure =
            await delegationAuthority.EnsureCanAssignOrganizationUnitScopeAsync(
                actor.UserId,
                organizationUnitId,
                cancellationToken);
        if (sourceScopeFailure is not null)
        {
            return Result<Contracts.OrganizationUnitResponse>.Failure(
                sourceScopeFailure.Error!);
        }

        if (request.ParentId is Guid parentId)
        {
            var parent = await store.FindOrganizationUnitByIdAsync(
                parentId,
                cancellationToken);
            if (parent is null)
            {
                return Result<Contracts.OrganizationUnitResponse>.Failure(
                    AuthorizationErrors.ParentOrganizationUnitNotFound);
            }

            if (!parent.IsActive)
            {
                return Result<Contracts.OrganizationUnitResponse>.Failure(
                    AuthorizationErrors.OrganizationUnitInactive);
            }

            var parentScopeFailure =
                await delegationAuthority.EnsureCanAssignOrganizationUnitScopeAsync(
                    actor.UserId,
                    parentId,
                    cancellationToken);
            if (parentScopeFailure is not null)
            {
                return Result<Contracts.OrganizationUnitResponse>.Failure(
                    parentScopeFailure.Error!);
            }
        }

        if (await store.WouldCreateOrganizationUnitCycleAsync(
                organizationUnitId,
                request.ParentId,
                cancellationToken))
        {
            return Result<Contracts.OrganizationUnitResponse>.Failure(
                AuthorizationErrors.OrganizationUnitCycle);
        }

        var parentById = await BuildParentMapAsync(
            store,
            organizationUnitId,
            request.ParentId,
            cancellationToken);

        unit.Move(request.ParentId, parentById);

        await auditor.RecordAsync(
            AuthorizationAuditActions.OrganizationUnitMoved,
            nameof(OrganizationUnit),
            unit.Id,
            $"code={unit.Code};parentId={unit.ParentId}",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Contracts.OrganizationUnitResponse>.Success(unit.ToResponse());
    }

    private static async Task<IReadOnlyDictionary<Guid, Guid?>> BuildParentMapAsync(
        IAuthorizationAdminStore store,
        Guid organizationUnitId,
        Guid? newParentId,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<Guid, Guid?>();
        await WalkAncestorsAsync(store, organizationUnitId, map, cancellationToken);
        if (newParentId is Guid parentId)
        {
            await WalkAncestorsAsync(store, parentId, map, cancellationToken);
        }

        return map;
    }

    private static async Task WalkAncestorsAsync(
        IAuthorizationAdminStore store,
        Guid startId,
        Dictionary<Guid, Guid?> map,
        CancellationToken cancellationToken)
    {
        Guid? current = startId;
        var guard = 0;
        while (current is Guid id)
        {
            if (map.ContainsKey(id))
            {
                break;
            }

            var unit = await store.FindOrganizationUnitByIdAsync(id, cancellationToken);
            if (unit is null)
            {
                break;
            }

            map[unit.Id] = unit.ParentId;
            current = unit.ParentId;
            if (++guard > 10_000)
            {
                break;
            }
        }
    }
}
