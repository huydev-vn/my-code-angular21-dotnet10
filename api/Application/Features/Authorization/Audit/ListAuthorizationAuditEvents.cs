using Application.Common.Pagination;
using Application.Features.Authorization.Abstractions;

namespace Application.Features.Authorization.Audit;

/// <summary>One authorization administration audit entry.</summary>
public sealed record AuthorizationAuditEventResponse(
    Guid Id,
    Guid? ActorUserId,
    string Action,
    string EntityType,
    Guid EntityId,
    string? Data,
    string? TraceId,
    DateTimeOffset OccurredAt);

/// <summary>Paged authorization audit log.</summary>
public sealed record AuthorizationAuditEventListResponse(
    IReadOnlyList<AuthorizationAuditEventResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed class ListAuthorizationAuditEvents(IAuthorizationAdminStore store)
{
    public async Task<AuthorizationAuditEventListResponse> HandleAsync(
        PageRequest page,
        string? action,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var result = await store.ListAuditEventsAsync(
            page,
            action,
            actorUserId,
            cancellationToken);

        return new AuthorizationAuditEventListResponse(
            result.Items.Select(entry => new AuthorizationAuditEventResponse(
                entry.Id,
                entry.ActorUserId,
                entry.Action,
                entry.EntityType,
                entry.EntityId,
                entry.Data,
                entry.TraceId,
                entry.OccurredAt)).ToArray(),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }
}
