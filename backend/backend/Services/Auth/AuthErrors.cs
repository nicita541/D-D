namespace backend.Services.Auth;

public sealed class AuthServiceException : Exception
{
    public AuthServiceException(string message)
        : base(message)
    {
    }
}
