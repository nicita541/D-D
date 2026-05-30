namespace backend.Contracts.Rpg.Common;

public sealed class RpgConflictException : Exception
{
    public RpgConflictException(string message)
        : base(message)
    {
    }
}
