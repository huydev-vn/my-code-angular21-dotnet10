namespace Application.Common.Settings;

public interface IIdentitySettings
{
    bool AllowRegistration { get; }

    bool RunSeeders { get; }

    /// <summary>
    /// When true, newly provisioned accounts are marked email-confirmed because
    /// accounts are issued by the organization rather than self-verified.
    /// </summary>
    bool ConfirmEmailOnProvision { get; }

    /// <summary>
    /// When true, members of privileged groups cannot disable authenticator MFA
    /// and should enroll after their first password login.
    /// </summary>
    bool RequireMfaForPrivileged { get; }

    /// <summary>Time-to-live for password→TOTP login tickets.</summary>
    int MfaChallengeMinutes { get; }

    /// <summary>Issuer label embedded in otpauth:// URIs.</summary>
    string AuthenticatorIssuer { get; }
}
