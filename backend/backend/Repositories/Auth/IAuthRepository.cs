namespace backend.Repositories.Auth;

public interface IAuthRepository
{
    Task<AccountRecord?> FindAccountByIdAsync(Guid accountId, CancellationToken cancellationToken);

    Task<AccountRecord?> FindAccountByEmailOrUsernameAsync(string emailOrUsername, CancellationToken cancellationToken);

    Task<AccountRecord> CreateAccountAsync(
        string email,
        string username,
        string passwordHash,
        string? displayName,
        string role,
        CancellationToken cancellationToken);

    Task StoreRefreshTokenAsync(
        Guid accountId,
        string tokenHash,
        DateTimeOffset expiresAt,
        string? createdByIp,
        CancellationToken cancellationToken);

    Task<RefreshTokenRecord?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);

    Task<bool> RevokeRefreshTokenAsync(
        string tokenHash,
        string? revokedByIp,
        CancellationToken cancellationToken);
}
