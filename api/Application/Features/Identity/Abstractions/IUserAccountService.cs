using Application.Common.Pagination;
using Application.Common.Results;

namespace Application.Features.Identity.Abstractions;

public interface IUserAccountService
{
    Task<Result<UserAccount>> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<Result<UserAccount>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<PageResult<UserAccount>> ListAsync(
        PageRequest page,
        CancellationToken cancellationToken);

    Task<Result<AuthenticatorSetup>> BeginAuthenticatorSetupAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<Result> ConfirmAuthenticatorSetupAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken);

    Task<Result> DisableAuthenticatorAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken);

    Task<Result> VerifyAuthenticatorCodeAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken);
}

/// <summary>Shared TOTP secret for authenticator app enrollment.</summary>
public sealed record AuthenticatorSetup(
    string SharedKey,
    string AuthenticatorUri);
