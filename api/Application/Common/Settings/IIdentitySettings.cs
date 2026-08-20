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
}
