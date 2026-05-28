using System.Text.Json.Serialization;

namespace backend.Models;

public class CharacterAttributes
{
    [JsonPropertyName("сила")]
    public int Strength { get; set; }

    [JsonPropertyName("ловкость")]
    public int Dexterity { get; set; }

    [JsonPropertyName("телосложение")]
    public int Constitution { get; set; }

    [JsonPropertyName("интеллект")]
    public int Intelligence { get; set; }

    [JsonPropertyName("мудрость")]
    public int Wisdom { get; set; }

    [JsonPropertyName("харизма")]
    public int Charisma { get; set; }

    [JsonPropertyName("инициатива")]
    public int Initiative { get; set; }

    [JsonPropertyName("скорость")]
    public int Speed { get; set; }

    [JsonPropertyName("восприятие")]
    public int Perception { get; set; }
}

public class Proficiencies
{
    [JsonPropertyName("доспехи")]
    public List<string> Armor { get; set; } = new();

    [JsonPropertyName("оружие")]
    public List<string> Weapons { get; set; } = new();

    [JsonPropertyName("навыки")]
    public List<string> Skills { get; set; } = new();

    [JsonPropertyName("спасброски")]
    public List<string> SavingThrows { get; set; } = new();

    [JsonPropertyName("инструменты")]
    public List<string> Tools { get; set; } = new();

    [JsonPropertyName("языки")]
    public List<string> Languages { get; set; } = new();
}

public class AbilityBook
{
    [JsonPropertyName("классовые")]
    public List<Ability> ClassAbilities { get; set; } = new();

    [JsonPropertyName("видовые")]
    public List<Ability> SpeciesAbilities { get; set; } = new();

    [JsonPropertyName("черты")]
    public List<Ability> Feats { get; set; } = new();

    [JsonPropertyName("заклинания")]
    public List<Ability> Spells { get; set; } = new();
}

public class Ability
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = $"ability_{Guid.NewGuid():N}";

    [JsonPropertyName("название")]
    public string Name { get; set; } = "";

    [JsonPropertyName("описание")]
    public string Description { get; set; } = "";

    [JsonPropertyName("тип")]
    public string Type { get; set; } = ""; // пассивная, действие, бонусное_действие, реакция, заклинание

    [JsonPropertyName("стоимость")]
    public ResourceCost? Cost { get; set; }

    [JsonPropertyName("эффекты")]
    public List<EffectModifier> Effects { get; set; } = new();
}

public class ResourceCost
{
    [JsonPropertyName("ресурсId")]
    public string ResourceId { get; set; } = "";

    [JsonPropertyName("количество")]
    public int Amount { get; set; }
}
