using System.Text.Json.Serialization;

namespace backend.Models;

public class Player
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("персонаж")]
    public CharacterProfile Character { get; set; } = new();

    [JsonPropertyName("прогресс")]
    public Progression Progression { get; set; } = new();

    [JsonPropertyName("ресурсы")]
    public Resources Resources { get; set; } = new();

    [JsonPropertyName("характеристики")]
    public CharacterAttributes Attributes { get; set; } = new();

    [JsonPropertyName("владения")]
    public Proficiencies Proficiencies { get; set; } = new();

    [JsonPropertyName("способности")]
    public AbilityBook Abilities { get; set; } = new();

    [JsonPropertyName("потребности")]
    public Needs Needs { get; set; } = new();

    [JsonPropertyName("богатство")]
    public Wealth Wealth { get; set; } = new();

    [JsonPropertyName("инвентарь")]
    public List<InventoryItem> Inventory { get; set; } = new();

    [JsonPropertyName("экипировка")]
    public EquippedGear Equipment { get; set; } = new();

    [JsonPropertyName("бой")]
    public CombatStats Combat { get; set; } = new();
}

public class CharacterProfile
{
    [JsonPropertyName("имя")]
    public string Name { get; set; } = "";

    [JsonPropertyName("предыстория")]
    public string Background { get; set; } = "";

    [JsonPropertyName("вид")]
    public string Species { get; set; } = "";

    [JsonPropertyName("класс")]
    public string Class { get; set; } = "";

    [JsonPropertyName("подкласс")]
    public string? Subclass { get; set; }

    [JsonPropertyName("описание")]
    public string? Description { get; set; }

    [JsonPropertyName("мировоззрение")]
    public string? Alignment { get; set; }
}

public class Progression
{
    [JsonPropertyName("уровень")]
    public int Level { get; set; } = 1;

    [JsonPropertyName("опыт")]
    public int Experience { get; set; }

    [JsonPropertyName("опытдоследующегоуровня")]
    public int ExperienceToNextLevel { get; set; } = 300;
}
