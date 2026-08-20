using Application.Features.Identity.Errors;
using Application.Features.Identity.Register;
using FluentValidation;

namespace Application.Tests.Features.Identity;

public sealed class RegisterUserTests
{
    [Fact]
    public async Task HandleAsync_WhenRegistrationDisabled_ReturnsForbidden()
    {
        var handler = new RegisterUser(
            new NoOpValidator(),
            new FakeIdentitySettings(AllowRegistration: false),
            userAccountService: null!,
            tokenIssuer: null!,
            unitOfWork: null!);

        var result = await handler.HandleAsync(
            new RegisterUserRequest
            {
                Email = "user@example.com",
                Password = "Password123!@#"
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.RegistrationDisabled, result.Error);
    }

    private sealed class FakeIdentitySettings(
        bool AllowRegistration,
        bool RunSeeders = false,
        bool ConfirmEmailOnProvision = true)
        : Application.Common.Settings.IIdentitySettings
    {
        public bool AllowRegistration { get; } = AllowRegistration;

        public bool RunSeeders { get; } = RunSeeders;

        public bool ConfirmEmailOnProvision { get; } = ConfirmEmailOnProvision;
    }

    private sealed class NoOpValidator : AbstractValidator<RegisterUserRequest>;
}
