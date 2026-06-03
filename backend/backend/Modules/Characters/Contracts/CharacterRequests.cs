using System.Text.Json.Serialization;

namespace backend.Modules.Characters.Contracts;

public sealed class CreateCharacterRequest
{
    [JsonPropertyName("имя")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("предыстория")]
    public string? Background { get; set; }

    [JsonPropertyName("вид")]
    public string? Species { get; set; }

    [JsonPropertyName("класс")]
    public string? ClassName { get; set; }

    [JsonPropertyName("подкласс")]
    public string? Subclass { get; set; }

    [JsonPropertyName("описание")]
    public string? Description { get; set; }

    [JsonPropertyName("мировоззрение")]
    public string? Alignment { get; set; }

    [JsonPropertyName("прогресс")]
    public CharacterProgressionRequest Progression { get; set; } = new();

    [JsonPropertyName("характеристики")]
    public CharacterAttributesRequest Attributes { get; set; } = new();

    [JsonPropertyName("ресурсы")]
    public CharacterResourcesRequest Resources { get; set; } = new();

    [JsonPropertyName("богатство")]
    public CharacterWealthRequest Wealth { get; set; } = new();

    [JsonPropertyName("бой")]
    public CharacterCombatStatsRequest Combat { get; set; } = new();
}

public sealed class UpdateCharacterRequest
{
    [JsonPropertyName("имя")]
    public string? Name { get; set; }

    [JsonPropertyName("предыстория")]
    public string? Background { get; set; }

    [JsonPropertyName("вид")]
    public string? Species { get; set; }

    [JsonPropertyName("класс")]
    public string? ClassName { get; set; }

    [JsonPropertyName("подкласс")]
    public string? Subclass { get; set; }

    [JsonPropertyName("описание")]
    public string? Description { get; set; }

    [JsonPropertyName("мировоззрение")]
    public string? Alignment { get; set; }

    [JsonPropertyName("прогресс")]
    public CharacterProgressionRequest? Progression { get; set; }

    [JsonPropertyName("характеристики")]
    public CharacterAttributesRequest? Attributes { get; set; }

    [JsonPropertyName("ресурсы")]
    public CharacterResourcesRequest? Resources { get; set; }

    [JsonPropertyName("богатство")]
    public CharacterWealthRequest? Wealth { get; set; }

    [JsonPropertyName("бой")]
    public CharacterCombatStatsRequest? Combat { get; set; }
}

public sealed class CharacterProgressionRequest
{
    [JsonPropertyName("уровень")]
    public int Level { get; set; } = 1;

    [JsonPropertyName("опыт")]
    public int Experience { get; set; }

    [JsonPropertyName("����������������������")]
    public int ExperienceToNextLevel { get; set; } = 300;
}

public sealed class CharacterAttributesRequest
{
    [JsonPropertyName("сила")]
    public int Strength { get; set; } = 10;

    [JsonPropertyName("ловкость")]
    public int Dexterity { get; set; } = 10;

    [JsonPropertyName("телосложение")]
    public int Constitution { get; set; } = 10;

    [JsonPropertyName("интеллект")]
    public int Intelligence { get; set; } = 10;

    [JsonPropertyName("мудрость")]
    public int Wisdom { get; set; } = 10;

    [JsonPropertyName("харизма")]
    public int Charisma { get; set; } = 10;

    [JsonPropertyName("инициатива")]
    public int Initiative { get; set; }

    [JsonPropertyName("скорость")]
    public int Speed { get; set; } = 9;

    [JsonPropertyName("восприятие")]
    public int Perception { get; set; } = 10;
}

public sealed class CharacterResourcesRequest
{
    [JsonPropertyName("хпМаксимум")]
    public int HpMax { get; set; } = 1;

    [JsonPropertyName("хпТекущее")]
    public int HpCurrent { get; set; } = 1;

    [JsonPropertyName("манаМаксимум")]
    public int ManaMax { get; set; }

    [JsonPropertyName("манаТекущая")]
    public int ManaCurrent { get; set; }

    [JsonPropertyName("очкиДействийМаксимум")]
    public int ActionPointsMax { get; set; } = 1;

    [JsonPropertyName("очкиДействийТекущие")]
    public int ActionPointsCurrent { get; set; } = 1;
}

public sealed class CharacterWealthRequest
{
    [JsonPropertyName("медные")]
    public int Copper { get; set; }

    [JsonPropertyName("серебряные")]
    public int Silver { get; set; }

    [JsonPropertyName("золотые")]
    public int Gold { get; set; }

    [JsonPropertyName("платиновые")]
    public int Platinum { get; set; }
}

public sealed class CharacterCombatStatsRequest
{
    [JsonPropertyName("классДоспеха")]
    public int ArmorClass { get; set; } = 10;

    [JsonPropertyName("бонусМастерства")]
    public int ProficiencyBonus { get; set; } = 2;

    [JsonPropertyName("вБою")]
    public bool InCombat { get; set; }

    [JsonPropertyName("бросокИнициативы")]
    public int InitiativeRoll { get; set; }
}
