using Application.Common.Settings;

namespace Infrastructure.Identity;

public sealed class IdentitySettings : IIdentitySettings
{
    public const string SectionName = "Identity";

    public bool AllowRegistration { get; init; }

    public bool RunSeeders { get; init; }

    /// <summary>
    /// When true, newly provisioned accounts are treated as email-confirmed.
    /// Intended for organization-provisioned accounts that are handed to users
    /// without a self-service email-verification flow.
    /// </summary>
    public bool ConfirmEmailOnProvision { get; init; } = true;
}
