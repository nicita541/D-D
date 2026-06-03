using backend.Modules.Auth.Contracts;

namespace backend.Modules.Auth.Application;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken);

    Task<AuthResponse?> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken);

    Task<AuthResponse?> RefreshAsync(RefreshRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken);

    Task<bool> LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<AccountDto?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken);
}