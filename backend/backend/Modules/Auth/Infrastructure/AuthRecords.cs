namespace backend.Modules.Auth.Infrastructure;

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
    string? CreatedByIp,
    string? RevokedByIp,
    string? UserAgent,
    string? ReplacedByTokenHash,
    AccountRecord Account);