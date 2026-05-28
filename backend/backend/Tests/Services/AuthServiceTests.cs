using backend.Contracts.Auth;
using backend.Infrastructure.Auth;
using backend.Repositories.Auth;
using backend.Services.Auth;
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
            CancellationToken.None);

        Assert.NotEmpty(response.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        Assert.Equal("hero@example.com", response.Account.Email);
        Assert.Equal("user", response.Account.Role);
        Assert.Single(repository.RefreshTokens);
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
            CancellationToken.None);

        var response = await service.LoginAsync(
            new LoginRequest { EmailOrUsername = "hero", Password = "wrong-password" },
            null,
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
            null,
            CancellationToken.None);

        var refreshed = await service.RefreshAsync(
            new RefreshRequest { RefreshToken = registered.RefreshToken },
            null,
            CancellationToken.None);

        Assert.NotNull(refreshed);
        Assert.NotEqual(registered.RefreshToken, refreshed!.RefreshToken);
        Assert.Contains(repository.RefreshTokens, token => token.RevokedAt.HasValue);
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
            null,
            CancellationToken.None);

        var loggedOut = await service.LogoutAsync(
            new LogoutRequest { RefreshToken = registered.RefreshToken },
            null,
            CancellationToken.None);

        Assert.True(loggedOut);
        Assert.All(repository.RefreshTokens, token => Assert.NotNull(token.RevokedAt));
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

        public Task StoreRefreshTokenAsync(Guid accountId, string tokenHash, DateTimeOffset expiresAt, string? createdByIp, CancellationToken cancellationToken)
        {
            var account = _accounts.Single(account => account.Id == accountId);
            RefreshTokens.Add(new RefreshTokenRecord(Guid.NewGuid(), accountId, tokenHash, expiresAt, null, account));
            return Task.CompletedTask;
        }

        public Task<RefreshTokenRecord?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken)
        {
            return Task.FromResult(RefreshTokens.FirstOrDefault(token => token.TokenHash == tokenHash));
        }

        public Task<bool> RevokeRefreshTokenAsync(string tokenHash, string? revokedByIp, CancellationToken cancellationToken)
        {
            var index = RefreshTokens.FindIndex(token => token.TokenHash == tokenHash && token.RevokedAt is null);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            var token = RefreshTokens[index];
            RefreshTokens[index] = token with { RevokedAt = DateTimeOffset.UtcNow };
            return Task.FromResult(true);
        }
    }
}
