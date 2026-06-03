using Microsoft.AspNetCore.Identity;

namespace backend.Infrastructure.Auth;

public sealed class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<object> _passwordHasher = new();
    private static readonly object PasswordUser = new();

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(PasswordUser, password);
    }

    public bool VerifyPassword(string passwordHash, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(PasswordUser, passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
