using System.Text.Json.Serialization;

namespace backend.Models;

public class InventoryItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = $"item_{Guid.NewGuid():N}";

    [JsonPropertyName("templateId")]
    public string? TemplateId { get; set; }

    [JsonPropertyName("название")]
    public string Name { get; set; } = "";

    [JsonPropertyName("тип")]
    public string Type { get; set; } = "";

    [JsonPropertyName("подтип")]
    public string? Subtype { get; set; }

    [JsonPropertyName("описание")]
    public string? Description { get; set; }

    [JsonPropertyName("количество")]
    public int Quantity { get; set; } = 1;

    [JsonPropertyName("суммируется")]
    public bool Stackable { get; set; }

    [JsonPropertyName("весзаштуку")]
    public decimal WeightEach { get; set; }

    [JsonPropertyName("состояние")]
    public string Condition { get; set; } = "нормальное";

    [JsonPropertyName("редкость")]
    public string Rarity { get; set; } = "обычный";

    [JsonPropertyName("цена")]
    public CoinValue? Price { get; set; }

    [JsonPropertyName("оружие")]
    public WeaponStats? Weapon { get; set; }

    [JsonPropertyName("доспех")]
    public ArmorStats? Armor { get; set; }

    [JsonPropertyName("расходник")]
    public ConsumableStats? Consumable { get; set; }

    [JsonPropertyName("магический")]
    public bool IsMagical { get; set; }

    [JsonPropertyName("теги")]
    public List<string> Tags { get; set; } = new();
}

public class WeaponStats
{
    [JsonPropertyName("кубикиурона")]
    public string DamageDice { get; set; } = "";

    [JsonPropertyName("типурона")]
    public string DamageType { get; set; } = "";

    [JsonPropertyName("бонусатаки")]
    public int AttackBonus { get; set; }

    [JsonPropertyName("бонусурона")]
    public int DamageBonus { get; set; }

    [JsonPropertyName("дальность")]
    public string? Range { get; set; }

    [JsonPropertyName("свойства")]
    public List<string> Properties { get; set; } = new();
}

public class ArmorStats
{
    [JsonPropertyName("кд")]
    public int ArmorClass { get; set; }

    [JsonPropertyName("бонускд")]
    public int ArmorClassBonus { get; set; }

    [JsonPropertyName("типдоспеха")]
    public string ArmorType { get; set; } = "";

    [JsonPropertyName("помехаскрытности")]
    public bool StealthDisadvantage { get; set; }

    [JsonPropertyName("требованиесилы")]
    public int? StrengthRequirement { get; set; }
}

public class ConsumableStats
{
    [JsonPropertyName("использований")]
    public int Uses { get; set; } = 1;

    [JsonPropertyName("эффекты")]
    public List<EffectModifier> Effects { get; set; } = new();
}

public class EquippedGear
{
    [JsonPropertyName("головаId")]
    public string? HeadItemId { get; set; }

    [JsonPropertyName("телоId")]
    public string? BodyItemId { get; set; }

    [JsonPropertyName("рукиId")]
    public string? HandsItemId { get; set; }

    [JsonPropertyName("ногиId")]
    public string? LegsItemId { get; set; }

    [JsonPropertyName("обувьId")]
    public string? FeetItemId { get; set; }

    [JsonPropertyName("основнаярукаId")]
    public string? MainHandItemId { get; set; }

    [JsonPropertyName("втораярукаId")]
    public string? OffHandItemId { get; set; }

    [JsonPropertyName("амулетId")]
    public string? AmuletItemId { get; set; }

    [JsonPropertyName("кольцо1Id")]
    public string? Ring1ItemId { get; set; }

    [JsonPropertyName("кольцо2Id")]
    public string? Ring2ItemId { get; set; }
}

public class Wealth
{
    [JsonPropertyName("монеты")]
    public Coins Coins { get; set; } = new();
}

public class Coins
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

public class CoinValue
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
