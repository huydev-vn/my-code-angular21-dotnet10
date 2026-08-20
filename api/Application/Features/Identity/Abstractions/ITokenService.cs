namespace Application.Features.Identity.Abstractions;

public interface ITokenService
{
    AccessToken CreateAccessToken(UserAccount user);

    IssuedRefreshToken CreateRefreshToken();

    string HashRefreshToken(string refreshToken);
}
