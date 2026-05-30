namespace backend.Services.Rpg;

public static class CombatRules
{
    public const string DefeatedCondition = "повержен";

    public static int ApplyDamage(int hpCurrent, int damage)
        => Math.Max(0, hpCurrent - Math.Max(0, damage));

    public static int ApplyHealing(int hpCurrent, int hpMax, int healing)
        => Math.Min(Math.Max(0, hpMax), hpCurrent + Math.Max(0, healing));

    public static IReadOnlyList<string> AddConditionOnce(IEnumerable<string> conditions, string condition)
    {
        var result = new List<string>();
        var exists = false;
        foreach (var current in conditions)
        {
            if (string.Equals(current, condition, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
            }

            if (!string.IsNullOrWhiteSpace(current))
            {
                result.Add(current.Trim());
            }
        }

        if (!exists)
        {
            result.Add(condition);
        }

        return result;
    }
}
