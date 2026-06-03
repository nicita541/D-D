namespace backend.Infrastructure.Auth;

public sealed record CurrentUser(
    Guid AccountId,
    string Email,
    string Username,
    string Role);
