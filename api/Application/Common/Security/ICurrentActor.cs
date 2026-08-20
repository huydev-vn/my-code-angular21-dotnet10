namespace Application.Common.Security;

public interface ICurrentActor
{
    Guid? UserId { get; }

    string? TraceId { get; }
}
