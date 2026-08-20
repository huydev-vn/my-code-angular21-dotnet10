namespace Application.Features.Authorization.Abstractions;

public enum AuthorizationDecisionReason
{
    Allowed,
    Unauthenticated,
    MissingPermission,
    OutsideUnitScope
}

public sealed record AuthorizationDecision(
    bool IsAllowed,
    AuthorizationDecisionReason Reason)
{
    public static AuthorizationDecision Allowed() =>
        new(true, AuthorizationDecisionReason.Allowed);

    public static AuthorizationDecision Unauthenticated() =>
        new(false, AuthorizationDecisionReason.Unauthenticated);

    public static AuthorizationDecision MissingPermission() =>
        new(false, AuthorizationDecisionReason.MissingPermission);

    public static AuthorizationDecision OutsideUnitScope() =>
        new(false, AuthorizationDecisionReason.OutsideUnitScope);
}
