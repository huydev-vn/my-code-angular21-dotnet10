namespace Application.Features.Authorization.Abstractions;

public interface IAuthorizationAuditor
{
    Task RecordAsync(
        string action,
        string entityType,
        Guid entityId,
        string? data,
        CancellationToken cancellationToken);
}
