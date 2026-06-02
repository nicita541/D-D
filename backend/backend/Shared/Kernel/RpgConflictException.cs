namespace backend.Shared.Kernel;

public sealed class RpgConflictException : Exception
{
    public RpgConflictException(string message)
        : base(message)
    {
    }
}
