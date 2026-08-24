using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Application.Common.Pagination;
using Application.Common.Results;
using Application.Common.Settings;
using Application.Common.Time;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Errors;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

internal sealed class UserAccountService(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    IClock clock,
    IIdentitySettings identitySettings) : IUserAccountService
{
    public async Task<Result<UserAccount>> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return Result<UserAccount>.Failure(IdentityErrors.EmailTaken);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            // Organization-provisioned accounts are handed to users without
            // a self-service email verification flow.
            EmailConfirmed = identitySettings.ConfirmEmailOnProvision,
            CreatedAt = clock.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            if (createResult.Errors.Any(error =>
                    error.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                return Result<UserAccount>.Failure(IdentityErrors.EmailTaken);
            }

            var message = string.Join(
                " ",
                createResult.Errors.Select(error => error.Description));

            return Result<UserAccount>.Failure(
                Application.Common.Errors.Error.Validation(
                    IdentityErrors.RegistrationFailed.Code,
                    string.IsNullOrWhiteSpace(message)
                        ? IdentityErrors.RegistrationFailed.Message
                        : message));
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Result<UserAccount>.Success(await MapAsync(user, cancellationToken));
    }

    public async Task<Result<UserAccount>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Result<UserAccount>.Failure(IdentityErrors.InvalidCredentials);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return Result<UserAccount>.Failure(IdentityErrors.InvalidCredentials);
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            var failed = await userManager.AccessFailedAsync(user);
            if (!failed.Succeeded)
            {
                return Result<UserAccount>.Failure(IdentityErrors.InvalidCredentials);
            }

            return Result<UserAccount>.Failure(IdentityErrors.InvalidCredentials);
        }

        var reset = await userManager.ResetAccessFailedCountAsync(user);
        if (!reset.Succeeded)
        {
            return Result<UserAccount>.Failure(IdentityErrors.InvalidCredentials);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Result<UserAccount>.Success(await MapAsync(user, cancellationToken));
    }

    public async Task<UserAccount?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        cancellationToken.ThrowIfCancellationRequested();
        return user is null ? null : await MapAsync(user, cancellationToken);
    }

    public async Task<PageResult<UserAccount>> ListAsync(
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Users.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);

        // Directory listing omits live lockout checks (N+1). Callers that need
        // lockout status use FindByIdAsync / AuthenticateAsync.
        var users = await query
            .OrderBy(user => user.Email)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(user => new UserAccount(
                user.Id,
                user.Email ?? string.Empty,
                user.CreatedAt,
                IsLockedOut: false,
                user.TwoFactorEnabled))
            .ToListAsync(cancellationToken);

        return new PageResult<UserAccount>(
            users,
            totalCount,
            page.Page,
            page.PageSize);
    }

    public async Task<Result<AuthenticatorSetup>> BeginAuthenticatorSetupAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<AuthenticatorSetup>.Failure(IdentityErrors.UserNotFound);
        }

        if (await userManager.GetTwoFactorEnabledAsync(user))
        {
            return Result<AuthenticatorSetup>.Failure(IdentityErrors.MfaAlreadyEnabled);
        }

        await userManager.ResetAuthenticatorKeyAsync(user);
        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result<AuthenticatorSetup>.Failure(IdentityErrors.RegistrationFailed);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var email = user.Email ?? user.UserName ?? userId.ToString();
        var issuer = string.IsNullOrWhiteSpace(identitySettings.AuthenticatorIssuer)
            ? IdentitySettings.DefaultAuthenticatorIssuer
            : identitySettings.AuthenticatorIssuer.Trim();

        return Result<AuthenticatorSetup>.Success(
            new AuthenticatorSetup(
                FormatKey(key),
                BuildAuthenticatorUri(issuer, email, key)));
    }

    public async Task<Result> ConfirmAuthenticatorSetupAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        if (await userManager.GetTwoFactorEnabledAsync(user))
        {
            return Result.Failure(IdentityErrors.MfaAlreadyEnabled);
        }

        var isValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            NormalizeCode(code));
        if (!isValid)
        {
            return Result.Failure(IdentityErrors.InvalidMfaCode);
        }

        var enabled = await userManager.SetTwoFactorEnabledAsync(user, true);
        cancellationToken.ThrowIfCancellationRequested();
        return enabled.Succeeded
            ? Result.Success()
            : Result.Failure(IdentityErrors.InvalidMfaCode);
    }

    public async Task<Result> DisableAuthenticatorAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        if (!await userManager.GetTwoFactorEnabledAsync(user))
        {
            return Result.Failure(IdentityErrors.MfaNotEnabled);
        }

        var isValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            NormalizeCode(code));
        if (!isValid)
        {
            return Result.Failure(IdentityErrors.InvalidMfaCode);
        }

        var disabled = await userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disabled.Succeeded)
        {
            return Result.Failure(IdentityErrors.InvalidMfaCode);
        }

        await userManager.ResetAuthenticatorKeyAsync(user);
        cancellationToken.ThrowIfCancellationRequested();
        return Result.Success();
    }

    public async Task<Result> VerifyAuthenticatorCodeAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        if (!await userManager.GetTwoFactorEnabledAsync(user))
        {
            return Result.Failure(IdentityErrors.MfaNotEnabled);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return Result.Failure(IdentityErrors.InvalidMfaCode);
        }

        var isValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            NormalizeCode(code));
        cancellationToken.ThrowIfCancellationRequested();
        if (!isValid)
        {
            await userManager.AccessFailedAsync(user);
            return Result.Failure(IdentityErrors.InvalidMfaCode);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return Result.Success();
    }

    private async Task<UserAccount> MapAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var isLockedOut = await userManager.IsLockedOutAsync(user);
        cancellationToken.ThrowIfCancellationRequested();
        return new UserAccount(
            user.Id,
            user.Email ?? string.Empty,
            user.CreatedAt,
            isLockedOut,
            user.TwoFactorEnabled);
    }

    private static string NormalizeCode(string code) =>
        code.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        var currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }

        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    private static string BuildAuthenticatorUri(string issuer, string email, string unformattedKey)
    {
        const string authenticatorUriFormat =
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        return string.Format(
            CultureInfo.InvariantCulture,
            authenticatorUriFormat,
            UrlEncoder.Default.Encode(issuer),
            UrlEncoder.Default.Encode(email),
            unformattedKey);
    }
}
