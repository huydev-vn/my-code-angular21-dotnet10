namespace Application.Features.Identity.Contracts;

/// <summary>Paged collection of users returned by the identity directory.</summary>
public sealed record UserListResponse(
    IReadOnlyList<UserResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
