using Application.Common.Settings;

namespace Infrastructure.Identity;

public sealed class IdentitySettings : IIdentitySettings
{
    public const string SectionName = "Identity";

    public const int DefaultRefreshTokenRetentionDays = 30;

    public const int DefaultRefreshTokenCleanupBatchSize = 500;

    public const int DefaultMfaChallengeMinutes = 5;

    public const string DefaultAuthenticatorIssuer = "Net10Angular19";

    public bool AllowRegistration { get; init; }

    public bool RunSeeders { get; init; }

    /// <summary>
    /// When true, newly provisioned accounts are treated as email-confirmed.
    /// Intended for organization-provisioned accounts that are handed to users
    /// without a self-service email-verification flow.
    /// </summary>
    public bool ConfirmEmailOnProvision { get; init; } = true;

    /// <summary>
    /// How long revoked or expired refresh tokens are retained for replay
    /// detection before background cleanup deletes them.
    /// </summary>
    public int RefreshTokenRetentionDays { get; init; } = DefaultRefreshTokenRetentionDays;

    /// <summary>Max rows deleted per cleanup cycle.</summary>
    public int RefreshTokenCleanupBatchSize { get; init; } =
        DefaultRefreshTokenCleanupBatchSize;

    public bool RequireMfaForPrivileged { get; init; } = true;

    public int MfaChallengeMinutes { get; init; } = DefaultMfaChallengeMinutes;

    public string AuthenticatorIssuer { get; init; } = DefaultAuthenticatorIssuer;
}
