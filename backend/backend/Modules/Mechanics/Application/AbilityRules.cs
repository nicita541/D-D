using backend.Shared.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.Mechanics.Application;

public static class AbilityRules
{
    public static string NormalizeAbility(string ability)
    {
        var normalized = ability.Trim().ToLowerInvariant();
        return normalized switch
        {
            "сила" or "strength" => "сила",
            "ловкость" or "dexterity" => "ловкость",
            "телосложение" or "constitution" => "телосложение",
            "интеллект" or "intelligence" => "интеллект",
            "мудрость" or "wisdom" => "мудрость",
            "харизма" or "charisma" => "харизма",
            _ => throw new RpgValidationException("Характеристика должна быть одной из: сила, ловкость, телосложение, интеллект, мудрость, харизма.")
        };
    }

    public static string ToColumnName(string normalizedAbility)
    {
        return normalizedAbility switch
        {
            "сила" => "strength",
            "ловкость" => "dexterity",
            "телосложение" => "constitution",
            "интеллект" => "intelligence",
            "мудрость" => "wisdom",
            "харизма" => "charisma",
            _ => throw new RpgValidationException("Характеристика не поддерживается.")
        };
    }

    public static int CalculateModifier(int score)
    {
        return (int)Math.Floor((score - 10) / 2.0);
    }
}
