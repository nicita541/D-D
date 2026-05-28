using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace backend.Infrastructure.Auth;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUser GetRequiredUser()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Authenticated user is required.");
        }

        var accountIdValue =
            user.FindFirstValue("accountId")
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(accountIdValue, out var accountId))
        {
            throw new UnauthorizedAccessException("Authenticated user has no valid account id claim.");
        }

        return new CurrentUser(
            accountId,
            user.FindFirstValue(JwtRegisteredClaimNames.Email) ?? user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            user.FindFirstValue("username") ?? user.Identity?.Name ?? string.Empty,
            user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role") ?? "user");
    }
}
