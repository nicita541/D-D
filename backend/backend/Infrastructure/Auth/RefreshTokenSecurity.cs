using System.Security.Cryptography;
using System.Text;

namespace backend.Infrastructure.Auth;

public static class RefreshTokenSecurity
{
    public static string GenerateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
