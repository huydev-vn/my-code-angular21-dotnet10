namespace Domain.Authorization;

/// <summary>
/// How a user relates to an organization unit as organizational membership metadata.
/// This is not a permission grant and does not confer data access by itself.
/// </summary>
public enum OrganizationUnitRelationship
{
    /// <summary>Primary home OU for the user. At most one active Primary per user.</summary>
    Primary = 0,

    /// <summary>Additional organizational affiliation; may coexist with Primary and other Additional rows.</summary>
    Additional = 1
}
