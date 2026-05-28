namespace backend.Repositories.Auth;

public sealed class DuplicateAccountException : Exception
{
    public DuplicateAccountException()
        : base("Account with this email or username already exists.")
    {
    }
}
