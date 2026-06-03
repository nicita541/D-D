namespace backend.Modules.Characters.Domain;

public static class ProgressionRules
{
    public const int MaxMvpLevel = 5;
    public const int DefaultHpIncrease = 5;

    public static int GetLevelThreshold(int level)
        => level switch
        {
            1 => 0,
            2 => 300,
            3 => 900,
            4 => 2700,
            5 => 6500,
            _ => throw new ArgumentOutOfRangeException(nameof(level), "Supported MVP levels are 1 through 5.")
        };

    public static int? GetNextLevelThreshold(int currentLevel)
        => currentLevel switch
        {
            <= 0 => 300,
            1 => 300,
            2 => 900,
            3 => 2700,
            4 => 6500,
            _ => null
        };

    public static bool CanLevelUp(int currentLevel, int experience)
    {
        var threshold = GetNextLevelThreshold(currentLevel);
        return threshold.HasValue && experience >= threshold.Value;
    }

    public static int GetProficiencyBonus(int level)
        => level >= 5 ? 3 : 2;

    public static int GetHpIncrease(int? requestedHpIncrease)
        => requestedHpIncrease ?? DefaultHpIncrease;
}
