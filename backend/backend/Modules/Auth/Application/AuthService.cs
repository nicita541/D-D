using backend.Modules.Auth.Contracts;
using backend.Infrastructure.Auth;
using backend.Modules.Auth.Infrastructure;
using Microsoft.Extensions.Options;

namespace backend.Modules.Auth.Application;

public sealed class AuthService : IAuthService
{
    private readonly IAuthRepository _repository;
    private readonly IPasswordHashService _passwords;
    private readonly IJwtTokenService _tokens;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        IAuthRepository repository,
        IPasswordHashService passwords,
        IJwtTokenService tokens,
        IOptions<JwtOptions> jwtOptions)
    {
        _repository = repository;
        _passwords = passwords;
        _tokens = tokens;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        ValidateRegisterRequest(request);

        var existingByEmail = await _repository.FindAccountByEmailOrUsernameAsync(request.Email, cancellationToken);
        if (existingByEmail is not null)
        {
            throw new AuthServiceException("Account with this email already exists.");
        }

        var existingByUsername = await _repository.FindAccountByEmailOrUsernameAsync(request.Username, cancellationToken);
        if (existingByUsername is not null)
        {
            throw new AuthServiceException("Account with this username already exists.");
        }

        try
        {
            var account = await _repository.CreateAccountAsync(
                request.Email,
                request.Username,
                _passwords.HashPassword(request.Password),
                request.DisplayName,
                "user",
                cancellationToken);

            return await IssueTokensAsync(account, ipAddress, userAgent, null, cancellationToken);
        }
        catch (DuplicateAccountException ex)
        {
            throw new AuthServiceException(ex.Message);
        }
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EmailOrUsername) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var account = await _repository.FindAccountByEmailOrUsernameAsync(request.EmailOrUsername, cancellationToken);
        if (account is null || !account.IsActive || !_passwords.VerifyPassword(account.PasswordHash, request.Password))
        {
            return null;
        }

        return await IssueTokensAsync(account, ipAddress, userAgent, null, cancellationToken);
    }

    public async Task<AuthResponse?> RefreshAsync(RefreshRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return null;
        }

        var tokenHash = _tokens.HashRefreshToken(request.RefreshToken);
        var storedToken = await _repository.FindRefreshTokenAsync(tokenHash, cancellationToken);
        if (storedToken is null
            || storedToken.RevokedAt.HasValue
            || storedToken.ExpiresAt <= DateTimeOffset.UtcNow
            || !storedToken.Account.IsActive)
        {
            return null;
        }

        return await IssueTokensAsync(storedToken.Account, ipAddress, userAgent, tokenHash, cancellationToken);
    }

    public async Task<bool> LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return false;
        }

        return await _repository.RevokeRefreshTokenAsync(
    _tokens.HashRefreshToken(request.RefreshToken),
    ipAddress,
    null,
    cancellationToken);
    }

    public async Task<AccountDto?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await _repository.FindAccountByIdAsync(accountId, cancellationToken);
        return account is null || !account.IsActive ? null : ToDto(account);
    }

    private async Task<AuthResponse> IssueTokensAsync(
        AccountRecord account,
        string? ipAddress,
        string? userAgent,
        string? replacesRefreshTokenHash,
        CancellationToken cancellationToken)
    {
        var (accessToken, expiresAt) = _tokens.CreateAccessToken(account);
        var refreshToken = _tokens.CreateRefreshToken();
        var refreshTokenHash = _tokens.HashRefreshToken(refreshToken);
        var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays <= 0 ? 30 : _jwtOptions.RefreshTokenDays);

        await _repository.StoreRefreshTokenAsync(
            account.Id,
            refreshTokenHash,
            refreshExpiresAt,
            ipAddress,
            userAgent,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(replacesRefreshTokenHash))
        {
            await _repository.RevokeRefreshTokenAsync(
                replacesRefreshTokenHash,
                ipAddress,
                refreshTokenHash,
                cancellationToken);
        }

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = expiresAt,
            Account = ToDto(account)
        };
    }

    private static AccountDto ToDto(AccountRecord account)
    {
        return new AccountDto
        {
            Id = account.Id,
            Email = account.Email,
            Username = account.Username,
            DisplayName = account.DisplayName,
            Role = account.Role
        };
    }

    private static void ValidateRegisterRequest(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new AuthServiceException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new AuthServiceException("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            throw new AuthServiceException("Password must contain at least 8 characters.");
        }
    }
}
