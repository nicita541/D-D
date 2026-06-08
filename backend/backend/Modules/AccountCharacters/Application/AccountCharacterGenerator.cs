using System.Security.Cryptography;
using System.Text.Json;
using backend.Modules.AccountCharacters.Contracts;

namespace backend.Modules.AccountCharacters.Application;

internal static class AccountCharacterGenerator
{
    private static readonly string[] Species = ["человек", "эльф", "дварф", "полурослик", "полуэльф"];
    private static readonly string[] Classes = ["воин", "плут", "следопыт", "жрец", "волшебник"];
    private static readonly string[] Backgrounds = ["странник", "ремесленник", "бывший солдат", "ученик мага", "искатель руин"];

    public static AccountCharacterDraft Generate(GenerateAccountCharacterRequest request)
    {
        var species = Pick(request.Species, Species);
        var className = Pick(request.ClassName, Classes);
        var background = Pick(request.Background, Backgrounds);
        var name = string.IsNullOrWhiteSpace(request.Name) ? GenerateName(species, className) : request.Name.Trim();

        var strength = RollStat();
        var dexterity = RollStat();
        var constitution = RollStat();
        var intelligence = RollStat();
        var wisdom = RollStat();
        var charisma = RollStat();
        var hpMax = Math.Max(6, ClassHitDie(className) + AbilityModifier(constitution));
        var manaMax = className is "волшебник" or "жрец" ? 2 : 0;
        var armorClass = 10 + AbilityModifier(dexterity);

        return new AccountCharacterDraft(
            name,
            species,
            className,
            background,
            string.IsNullOrWhiteSpace(request.Description)
                ? $"Начинающий герой: {species}, {className}. Пока без богатого снаряжения и громкой славы."
                : request.Description.Trim(),
            "нейтральное",
            JsonSerializer.SerializeToElement(new
            {
                strength,
                dexterity,
                constitution,
                intelligence,
                wisdom,
                charisma,
                initiative = AbilityModifier(dexterity),
                speed = 9,
                perception = 10 + AbilityModifier(wisdom)
            }),
            JsonSerializer.SerializeToElement(new
            {
                hpMax,
                hpCurrent = hpMax,
                manaMax,
                manaCurrent = manaMax,
                actionPointsMax = 1,
                actionPointsCurrent = 1
            }),
            JsonSerializer.SerializeToElement(new
            {
                level = 1,
                experience = 0,
                experienceToNextLevel = 300
            }),
            JsonSerializer.SerializeToElement(new
            {
                copper = 0,
                silver = 0,
                gold = 0,
                platinum = 0
            }),
            JsonSerializer.SerializeToElement(new
            {
                armorClass,
                proficiencyBonus = 2,
                inCombat = false,
                initiativeRoll = 0
            }),
            JsonSerializer.SerializeToElement(Array.Empty<object>()),
            JsonSerializer.SerializeToElement(new { }),
            JsonSerializer.SerializeToElement(new[]
            {
                new
                {
                    name = "Удар",
                    roll = "1d20",
                    damage = "1",
                    damageType = "bludgeoning"
                }
            }),
            JsonSerializer.SerializeToElement(new
            {
                generated = true,
                generator = "basic-v1"
            }));
    }

    private static string Pick(string? value, IReadOnlyList<string> options)
        => string.IsNullOrWhiteSpace(value) ? options[RandomNumberGenerator.GetInt32(options.Count)] : value.Trim();

    private static int RollStat()
        => RandomNumberGenerator.GetInt32(8, 17);

    private static int AbilityModifier(int score)
        => (int)Math.Floor((score - 10) / 2.0);

    private static int ClassHitDie(string? className)
        => className switch
        {
            "воин" => 10,
            "следопыт" => 10,
            "жрец" => 8,
            "плут" => 8,
            "волшебник" => 6,
            _ => 8
        };

    private static string GenerateName(string species, string className)
        => $"{char.ToUpperInvariant(species[0])}{species[1..]}-{className}-{RandomNumberGenerator.GetInt32(100, 999)}";
}
