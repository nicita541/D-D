namespace backend.Modules.Auth.Infrastructure;

public sealed class DuplicateAccountException : Exception
{
    public DuplicateAccountException()
        : base("Account with this email or username already exists.")
    {
    }
}
