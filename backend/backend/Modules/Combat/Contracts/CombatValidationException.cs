namespace backend.Modules.Combat.Contracts;

public sealed class CombatValidationException : Exception
{
    public CombatValidationException(string message)
        : base(message)
    {
    }
}