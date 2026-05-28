using backend.Repositories.Auth;

namespace backend.Services.Auth;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(AccountRecord account);

    string CreateRefreshToken();

    string HashRefreshToken(string refreshToken);
}
