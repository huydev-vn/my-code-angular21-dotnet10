namespace Application.Features.Identity.Contracts;

/// <summary>Collection of users returned by the identity directory.</summary>
public sealed record UserListResponse(IReadOnlyList<UserResponse> Items);
