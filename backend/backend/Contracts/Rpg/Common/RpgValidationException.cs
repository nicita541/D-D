namespace backend.Contracts.Rpg.Common;

public sealed class RpgValidationException : Exception
{
    public RpgValidationException(string message)
        : base(message)
    {
    }
}
