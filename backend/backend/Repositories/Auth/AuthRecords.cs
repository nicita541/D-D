namespace backend.Repositories.Auth;

public sealed record AccountRecord(
    Guid Id,
    string Email,
    string Username,
    string PasswordHash,
    string? DisplayName,
    string Role,
    bool IsActive);

public sealed record RefreshTokenRecord(
    Guid Id,
    Guid AccountId,
    string TokenHash,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    AccountRecord Account);
