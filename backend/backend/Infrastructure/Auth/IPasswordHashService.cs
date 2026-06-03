namespace backend.Infrastructure.Auth;

public interface IPasswordHashService
{
    string HashPassword(string password);

    bool VerifyPassword(string passwordHash, string password);
}
