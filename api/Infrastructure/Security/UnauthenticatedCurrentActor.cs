using Application.Common.Security;

namespace Infrastructure.Security;

internal sealed class UnauthenticatedCurrentActor : ICurrentActor
{
    public Guid? UserId => null;

    public string? TraceId => null;
}
