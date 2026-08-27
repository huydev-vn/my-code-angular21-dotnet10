namespace Domain.Authorization;

/// <summary>
/// Declares how a permission's data access is expected to be scoped at enforcement time.
/// Catalog metadata only — does not auto-create query filters or policies.
/// </summary>
public enum PermissionScopeMode
{
    /// <summary>No resource scope; grant is an on/off capability (e.g. button visibility).</summary>
    None = 0,

    /// <summary>Enforcement should restrict data to accessible organization units (and descendants).</summary>
    OrganizationUnit = 1,

    /// <summary>Grant applies globally; no OU filtering (typical for authorization admin APIs).</summary>
    Global = 2
}
