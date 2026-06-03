using backend.Modules.Auth.Contracts;
using backend.Infrastructure.Auth;
using backend.Modules.Auth.Infrastructure;
using backend.Modules.Auth.Application;
using Microsoft.Extensions.Options;

namespace Tests.Services;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Register_ReturnsTokensAndStoresRefreshHash()
    {
        var repository = new FakeAuthRepository();
        var service = CreateService(repository);

        var response = await service.RegisterAsync(
            new RegisterRequest
            {
                Email = "hero@example.com",
                Username = "hero",
                Password = "password-123",
                DisplayName = "Hero"
            },
            "127.0.0.1",
            "test-agent",
            CancellationToken.None);

        Assert.NotEmpty(response.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        Assert.Equal("hero@example.com", response.Account.Email);
        Assert.Equal("user", response.Account.Role);

        var refreshToken = Assert.Single(repository.RefreshTokens);
        Assert.NotEmpty(refreshToken.TokenHash);
        Assert.Equal("127.0.0.1", refreshToken.CreatedByIp);
        Assert.Equal("test-agent", refreshToken.UserAgent);
        Assert.Null(refreshToken.RevokedAt);
        Assert.Null(refreshToken.RevokedByIp);
        Assert.Null(refreshToken.ReplacedByTokenHash);
    }

    [Fact]
    public async Task Login_ReturnsNull_WhenPasswordIsInvalid()
    {
        var repository = new FakeAuthRepository();
        var service = CreateService(repository);

        await service.RegisterAsync(
            new RegisterRequest
            {
                Email = "hero@example.com",
                Username = "hero",
                Password = "password-123"
            },
            null,
            "register-agent",
            CancellationToken.None);

        var response = await service.LoginAsync(
            new LoginRequest { EmailOrUsername = "hero", Password = "wrong-password" },
            null,
            "login-agent",
            CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task Refresh_RotatesRefreshToken()
    {
        var repository = new FakeAuthRepository();
        var service = CreateService(repository);

        var registered = await service.RegisterAsync(
            new RegisterRequest
            {
                Email = "hero@example.com",
                Username = "hero",
                Password = "password-123"
            },
            "127.0.0.1",
            "register-agent",
            CancellationToken.None);

        var refreshed = await service.RefreshAsync(
            new RefreshRequest { RefreshToken = registered.RefreshToken },
            "127.0.0.2",
            "refresh-agent",
            CancellationToken.None);

        Assert.NotNull(refreshed);
        Assert.NotEqual(registered.RefreshToken, refreshed!.RefreshToken);
        Assert.Equal(2, repository.RefreshTokens.Count);

        var revokedToken = repository.RefreshTokens.Single(token => token.RevokedAt.HasValue);
        var activeToken = repository.RefreshTokens.Single(token => token.RevokedAt is null);

        Assert.Equal("127.0.0.2", revokedToken.RevokedByIp);
        Assert.Equal(activeToken.TokenHash, revokedToken.ReplacedByTokenHash);

        Assert.Equal("127.0.0.2", activeToken.CreatedByIp);
        Assert.Equal("refresh-agent", activeToken.UserAgent);
        Assert.Null(activeToken.RevokedAt);
        Assert.Null(activeToken.ReplacedByTokenHash);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var repository = new FakeAuthRepository();
        var service = CreateService(repository);

        var registered = await service.RegisterAsync(
            new RegisterRequest
            {
                Email = "hero@example.com",
                Username = "hero",
                Password = "password-123"
            },
            "127.0.0.1",
            "register-agent",
            CancellationToken.None);

        var loggedOut = await service.LogoutAsync(
            new LogoutRequest { RefreshToken = registered.RefreshToken },
            "127.0.0.2",
            CancellationToken.None);

        Assert.True(loggedOut);

        var refreshToken = Assert.Single(repository.RefreshTokens);
        Assert.NotNull(refreshToken.RevokedAt);
        Assert.Equal("127.0.0.2", refreshToken.RevokedByIp);
        Assert.Null(refreshToken.ReplacedByTokenHash);
    }

    private static AuthService CreateService(FakeAuthRepository repository)
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "Tests",
            Audience = "Tests",
            Secret = "TEST_SECRET_VALUE_WITH_MORE_THAN_32_CHARS",
            AccessTokenMinutes = 30,
            RefreshTokenDays = 30
        });

        return new AuthService(
            repository,
            new PasswordHashService(),
            new JwtTokenService(options),
            options);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        private readonly List<AccountRecord> _accounts = new();

        public List<RefreshTokenRecord> RefreshTokens { get; } = new();

        public Task<AccountRecord?> FindAccountByIdAsync(Guid accountId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_accounts.FirstOrDefault(account => account.Id == accountId));
        }

        public Task<AccountRecord?> FindAccountByEmailOrUsernameAsync(string emailOrUsername, CancellationToken cancellationToken)
        {
            return Task.FromResult(_accounts.FirstOrDefault(account =>
                string.Equals(account.Email, emailOrUsername, StringComparison.OrdinalIgnoreCase)
                || string.Equals(account.Username, emailOrUsername, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<AccountRecord> CreateAccountAsync(
            string email,
            string username,
            string passwordHash,
            string? displayName,
            string role,
            CancellationToken cancellationToken)
        {
            var account = new AccountRecord(Guid.NewGuid(), email, username, passwordHash, displayName, role, true);
            _accounts.Add(account);
            return Task.FromResult(account);
        }

        public Task StoreRefreshTokenAsync(
            Guid accountId,
            string tokenHash,
            DateTimeOffset expiresAt,
            string? createdByIp,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            var account = _accounts.Single(account => account.Id == accountId);

            RefreshTokens.Add(new RefreshTokenRecord(
                Guid.NewGuid(),
                accountId,
                tokenHash,
                expiresAt,
                null,
                createdByIp,
                null,
                userAgent,
                null,
                account));

            return Task.CompletedTask;
        }

        public Task<RefreshTokenRecord?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken)
        {
            return Task.FromResult(RefreshTokens.FirstOrDefault(token => token.TokenHash == tokenHash));
        }

        public Task<bool> RevokeRefreshTokenAsync(
            string tokenHash,
            string? revokedByIp,
            string? replacedByTokenHash,
            CancellationToken cancellationToken)
        {
            var index = RefreshTokens.FindIndex(token => token.TokenHash == tokenHash && token.RevokedAt is null);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            var token = RefreshTokens[index];
            RefreshTokens[index] = token with
            {
                RevokedAt = DateTimeOffset.UtcNow,
                RevokedByIp = revokedByIp,
                ReplacedByTokenHash = replacedByTokenHash
            };

            return Task.FromResult(true);
        }
    }
}