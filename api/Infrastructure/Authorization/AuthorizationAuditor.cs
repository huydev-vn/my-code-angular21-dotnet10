using Application.Common.Security;
using Application.Common.Time;
using Application.Features.Authorization.Abstractions;
using Domain.Authorization;
using Infrastructure.Persistence;

namespace Infrastructure.Authorization;

internal sealed class AuthorizationAuditor(
    AppDbContext dbContext,
    ICurrentActor actor,
    IClock clock) : IAuthorizationAuditor
{
    public Task RecordAsync(
        string action,
        string entityType,
        Guid entityId,
        string? data,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.AuthorizationAuditEvents.Add(
            AuthorizationAuditEvent.Create(
                actor.UserId,
                action,
                entityType,
                entityId,
                data,
                actor.TraceId,
                clock.UtcNow));
        return Task.CompletedTask;
    }
}
