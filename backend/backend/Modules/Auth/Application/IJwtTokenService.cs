using backend.Modules.Auth.Infrastructure;

namespace backend.Modules.Auth.Application;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(AccountRecord account);

    string CreateRefreshToken();

    string HashRefreshToken(string refreshToken);
}
