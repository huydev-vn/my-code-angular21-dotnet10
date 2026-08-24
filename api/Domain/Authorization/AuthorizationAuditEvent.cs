namespace Domain.Authorization;

/// <summary>
/// Nhật ký thay đổi quản trị authorization (tạo permission/group/đơn vị, gán/gỡ assignment).
/// ActorUserId có thể null (hành động hệ thống); không FK cứng tới user để giữ lịch sử sau khi xóa tài khoản.
/// </summary>
public sealed class AuthorizationAuditEvent
{
    private AuthorizationAuditEvent()
    {
    }

    private AuthorizationAuditEvent(
        Guid id,
        Guid? actorUserId,
        string action,
        string entityType,
        Guid entityId,
        string? data,
        string? traceId,
        DateTimeOffset occurredAt)
    {
        Id = id;
        ActorUserId = actorUserId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Data = data;
        TraceId = traceId;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string Action { get; private set; } = null!;

    public string EntityType { get; private set; } = null!;

    public Guid EntityId { get; private set; }

    public string? Data { get; private set; }

    public string? TraceId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static AuthorizationAuditEvent Create(
        Guid? actorUserId,
        string action,
        string entityType,
        Guid entityId,
        string? data,
        string? traceId,
        DateTimeOffset occurredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        return new AuthorizationAuditEvent(
            Guid.NewGuid(),
            actorUserId,
            action.Trim(),
            entityType.Trim(),
            entityId,
            string.IsNullOrWhiteSpace(data) ? null : data.Trim(),
            string.IsNullOrWhiteSpace(traceId) ? null : traceId.Trim(),
            occurredAt);
    }
}
