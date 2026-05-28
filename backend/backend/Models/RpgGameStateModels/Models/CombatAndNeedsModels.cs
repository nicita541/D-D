using System.Text.Json.Serialization;

namespace backend.Models;

public class CombatStats
{
    [JsonPropertyName("классдоспеха")]
    public int ArmorClass { get; set; }

    [JsonPropertyName("бонусмастерства")]
    public int ProficiencyBonus { get; set; }

    [JsonPropertyName("атаки")]
    public List<Attack> Attacks { get; set; } = new();

    [JsonPropertyName("активныйбой")]
    public bool InCombat { get; set; }

    [JsonPropertyName("инициатива")]
    public int InitiativeRoll { get; set; }
}

public class Attack
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = $"attack_{Guid.NewGuid():N}";

    [JsonPropertyName("предметId")]
    public string? ItemId { get; set; }

    [JsonPropertyName("название")]
    public string Name { get; set; } = "";

    [JsonPropertyName("бросок")]
    public string Roll { get; set; } = "";

    [JsonPropertyName("урон")]
    public string Damage { get; set; } = "";

    [JsonPropertyName("типурона")]
    public string DamageType { get; set; } = "";
}

public class Needs
{
    [JsonPropertyName("еда")]
    public DailyNeed Food { get; set; } = new();

    [JsonPropertyName("вода")]
    public DailyNeed Water { get; set; } = new();

    [JsonPropertyName("грузоподъемность")]
    public CarryingCapacity CarryingCapacity { get; set; } = new();

    [JsonPropertyName("передвижение")]
    public Movement Movement { get; set; } = new();
}

public class DailyNeed
{
    [JsonPropertyName("размер")]
    public string Size { get; set; } = "средний";

    [JsonPropertyName("вдень")]
    public string PerDay { get; set; } = "";

    [JsonPropertyName("осталось")]
    public decimal Remaining { get; set; }
}

public class CarryingCapacity
{
    [JsonPropertyName("максимум")]
    public decimal Max { get; set; }

    [JsonPropertyName("текущийвес")]
    public decimal CurrentWeight { get; set; }

    [JsonPropertyName("единица")]
    public string Unit { get; set; } = "кг";
}

public class Movement
{
    [JsonPropertyName("метровзаход")]
    public int MetersPerTurn { get; set; }

    [JsonPropertyName("кмзадень")]
    public int KmPerDay { get; set; }
}
