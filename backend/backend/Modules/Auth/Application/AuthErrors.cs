namespace backend.Modules.Auth.Application;

public sealed class AuthServiceException : Exception
{
    public AuthServiceException(string message)
        : base(message)
    {
    }
}
