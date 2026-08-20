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

    Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken);
}
