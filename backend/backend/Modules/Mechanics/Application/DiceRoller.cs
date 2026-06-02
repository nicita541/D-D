using System.Security.Cryptography;
using System.Text.RegularExpressions;
using backend.Shared.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.Mechanics.Application;

public interface IDiceRoller
{
    DiceFormula Parse(string formula);

    DiceRollResult Roll(string formula);

    DiceRollResult Roll(DiceFormula formula);
}

public sealed partial class DiceRoller : IDiceRoller
{
    public const int MaxDiceCount = 100;
    public const int MaxDiceSides = 1000;
    public const int MinModifier = -10000;
    public const int MaxModifier = 10000;

    public DiceFormula Parse(string formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
        {
            throw new RpgValidationException("Формула броска обязательна.");
        }

        var match = DiceFormulaRegex().Match(formula.Trim());
        if (!match.Success)
        {
            throw new RpgValidationException("Формула броска должна быть в формате d20, 1d20, 1d20+4 или 2d6.");
        }

        var countText = match.Groups["count"].Value;
        var count = string.IsNullOrWhiteSpace(countText) ? 1 : int.Parse(countText);
        var sides = int.Parse(match.Groups["sides"].Value);
        var modifierText = match.Groups["modifier"].Value.Replace(" ", string.Empty);
        var modifier = string.IsNullOrWhiteSpace(modifierText) ? 0 : int.Parse(modifierText);

        if (count is < 1 or > MaxDiceCount)
        {
            throw new RpgValidationException($"Количество кубов должно быть от 1 до {MaxDiceCount}.");
        }

        if (sides is < 1 or > MaxDiceSides)
        {
            throw new RpgValidationException($"Количество граней должно быть от 1 до {MaxDiceSides}.");
        }

        if (modifier is < MinModifier or > MaxModifier)
        {
            throw new RpgValidationException($"Модификатор должен быть от {MinModifier} до {MaxModifier}.");
        }

        return new DiceFormula(count, sides, modifier);
    }

    public DiceRollResult Roll(string formula)
    {
        return Roll(Parse(formula));
    }

    public DiceRollResult Roll(DiceFormula formula)
    {
        var rolls = new int[formula.DiceCount];
        var total = formula.Modifier;
        for (var index = 0; index < rolls.Length; index++)
        {
            rolls[index] = RandomNumberGenerator.GetInt32(1, formula.DiceSides + 1);
            total += rolls[index];
        }

        return new DiceRollResult(formula, rolls, total);
    }

    [GeneratedRegex(@"^(?<count>\d*)d(?<sides>\d+)(?<modifier>\s*[+-]\s*\d+)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DiceFormulaRegex();
}

public sealed record DiceFormula(int DiceCount, int DiceSides, int Modifier)
{
    public string Normalized
    {
        get
        {
            var modifier = Modifier switch
            {
                > 0 => $"+{Modifier}",
                < 0 => Modifier.ToString(),
                _ => string.Empty
            };

            return $"{DiceCount}d{DiceSides}{modifier}";
        }
    }
}

public sealed record DiceRollResult(DiceFormula Formula, IReadOnlyList<int> Rolls, int Total);
