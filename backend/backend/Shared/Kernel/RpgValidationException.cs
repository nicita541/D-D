namespace backend.Shared.Kernel;

public sealed class RpgValidationException : Exception
{
    public RpgValidationException(string message)
        : base(message)
    {
    }
}
