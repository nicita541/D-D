namespace backend.Modules.Combat;

public sealed class CombatValidationException : Exception
{
    public CombatValidationException(string message)
        : base(message)
    {
    }
}