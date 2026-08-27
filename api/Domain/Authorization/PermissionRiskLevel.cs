namespace Domain.Authorization;

/// <summary>
/// Relative risk of assigning or exercising a permission. Used for admin UX and
/// privileged-assignment guards; does not invent executable policies.
/// </summary>
public enum PermissionRiskLevel
{
    Low = 0,
    Medium = 1,
    High = 2,

    /// <summary>
    /// Assignable only to privileged groups (same guard as hard-coded bootstrap writes).
    /// </summary>
    Critical = 3
}
