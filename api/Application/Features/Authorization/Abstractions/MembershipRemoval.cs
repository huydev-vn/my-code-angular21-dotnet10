namespace Application.Features.Authorization.Abstractions;

/// <summary>Outcome of an atomic user-group membership removal.</summary>
public enum MembershipRemoval
{
    Removed = 0,
    NotFound = 1,
    LastPrivilegedMember = 2
}
