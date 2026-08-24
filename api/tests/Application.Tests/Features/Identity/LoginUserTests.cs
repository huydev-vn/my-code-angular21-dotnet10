using Application.Common.Pagination;
using Application.Common.Results;
using Application.Common.Time;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Errors;
using Application.Features.Identity.Login;
using FluentValidation;

namespace Application.Tests.Features.Identity;

public sealed class LoginUserTests
{
    [Fact]
    public async Task HandleAsync_WhenCredentialsInvalid_RecordsFailedMetric()
    {
        var metrics = new CountingAuthMetrics();
        var handler = new LoginUser(
            new PassThroughLoginValidator(),
            new FailingUserAccounts(),
            tokenIssuer: null!,
            mfaChallengeStore: new NoOpMfaStore(),
            identitySettings: new FixedIdentitySettings(),
            clock: new FixedClock(DateTimeOffset.UtcNow),
            metrics);

        var result = await handler.HandleAsync(
            new LoginUserRequest { Email = "a@b.com", Password = "Password123!@#" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidCredentials, result.Error);
        Assert.Equal(1, metrics.LoginFailedCount);
        Assert.Equal(0, metrics.LoginSucceededCount);
    }

    private sealed class PassThroughLoginValidator : AbstractValidator<LoginUserRequest>;

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FixedIdentitySettings : Application.Common.Settings.IIdentitySettings
    {
        public bool AllowRegistration => false;

        public bool RunSeeders => false;

        public bool ConfirmEmailOnProvision => true;

        public bool RequireMfaForPrivileged => true;

        public int MfaChallengeMinutes => 5;

        public string AuthenticatorIssuer => "test";
    }

    private sealed class NoOpMfaStore : IMfaChallengeStore
    {
        public Task StoreAsync(
            string ticket,
            Guid userId,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<Guid?> ConsumeAsync(string ticket, CancellationToken cancellationToken) =>
            Task.FromResult<Guid?>(null);
    }

    private sealed class FailingUserAccounts : IUserAccountService
    {
        public Task<Result<UserAccount>> RegisterAsync(
            string email,
            string password,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<UserAccount>> AuthenticateAsync(
            string email,
            string password,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result<UserAccount>.Failure(IdentityErrors.InvalidCredentials));

        public Task<UserAccount?> FindByIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<UserAccount?>(null);

        public Task<PageResult<UserAccount>> ListAsync(
            PageRequest page,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<AuthenticatorSetup>> BeginAuthenticatorSetupAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result> ConfirmAuthenticatorSetupAsync(
            Guid userId,
            string code,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result> DisableAuthenticatorAsync(
            Guid userId,
            string code,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result> VerifyAuthenticatorCodeAsync(
            Guid userId,
            string code,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class CountingAuthMetrics : IAuthMetrics
    {
        public int LoginFailedCount { get; private set; }

        public int LoginSucceededCount { get; private set; }

        public void LoginSucceeded() => LoginSucceededCount++;

        public void LoginFailed() => LoginFailedCount++;

        public void RefreshSucceeded()
        {
        }

        public void RefreshFailed()
        {
        }

        public void RefreshReuseDetected()
        {
        }

        public void RateLimited()
        {
        }

        public void MfaChallengeIssued()
        {
        }

        public void MfaSucceeded()
        {
        }

        public void MfaFailed()
        {
        }
    }
}
