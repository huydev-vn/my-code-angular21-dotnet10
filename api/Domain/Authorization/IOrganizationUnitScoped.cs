namespace Domain.Authorization;

/// <summary>
/// Marks a resource whose data access is constrained by organization-unit scope.
/// Implement on entities (or query projections) that carry an <see cref="OrganizationUnitId"/>.
/// </summary>
public interface IOrganizationUnitScoped
{
    Guid OrganizationUnitId { get; }
}
