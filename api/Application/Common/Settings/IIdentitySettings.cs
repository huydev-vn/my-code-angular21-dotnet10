namespace Application.Common.Settings;

public interface IIdentitySettings
{
    bool AllowRegistration { get; }

    bool RunSeeders { get; }
}
