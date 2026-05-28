namespace backend.Contracts.Rpg.Combat;

public sealed class CombatValidationException : Exception
{
    public CombatValidationException(string message)
        : base(message)
    {
    }
}