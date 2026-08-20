using Application.Common.Settings;

namespace Infrastructure.Identity;

public sealed class IdentitySettings : IIdentitySettings
{
    public const string SectionName = "Identity";

    public bool AllowRegistration { get; init; }

    public bool RunSeeders { get; init; }
}
