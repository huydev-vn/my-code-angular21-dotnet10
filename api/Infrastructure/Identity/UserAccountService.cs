using Application.Common.Results;
using Application.Common.Time;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Errors;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

internal sealed class UserAccountService(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    IClock clock) : IUserAccountService
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
            EmailConfirmed = true,
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
        return Result<UserAccount>.Success(Map(user));
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
            await userManager.AccessFailedAsync(user);
            return Result<UserAccount>.Failure(IdentityErrors.InvalidCredentials);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        cancellationToken.ThrowIfCancellationRequested();
        return Result<UserAccount>.Success(Map(user));
    }

    public async Task<UserAccount?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        cancellationToken.ThrowIfCancellationRequested();
        return user is null ? null : Map(user);
    }

    public async Task<IReadOnlyList<UserAccount>> ListAsync(
        CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        return users.Select(Map).ToArray();
    }

    private static UserAccount Map(ApplicationUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.CreatedAt);
}
