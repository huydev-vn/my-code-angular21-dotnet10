namespace Application.Features.Authorization.Groups;

/// <summary>Payload for creating a business user group.</summary>
public sealed record CreateUserGroupRequest(string Name, string? Description);
