using backend.Contracts.Auth;

namespace backend.Services.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<AuthResponse?> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<AuthResponse?> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<bool> LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<AccountDto?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken);
}
