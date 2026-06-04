using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Modules.Characters.Contracts;

public sealed class ConditionRequest
{
    [JsonPropertyName("название")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("тип")]
    public string Type { get; set; } = "effect";

    [JsonPropertyName("описание")]
    public string? Description { get; set; }

    [JsonPropertyName("источник")]
    public string? Source { get; set; }

    [JsonPropertyName("осталосьХодов")]
    public int? RemainingTurns { get; set; }

    [JsonPropertyName("постоянное")]
    public bool IsPermanent { get; set; }

    [JsonPropertyName("стаки")]
    public int Stacks { get; set; } = 1;

    [JsonPropertyName("максимумСтаков")]
    public int? MaxStacks { get; set; }

    [JsonPropertyName("эффекты")]
    public JsonElement? Effects { get; set; }

    [JsonPropertyName("теги")]
    public JsonElement? Tags { get; set; }
}

public sealed class LimitedResourceRequest
{
    [JsonPropertyName("название")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("максимум")]
    public int MaxValue { get; set; }

    [JsonPropertyName("текущее")]
    public int CurrentValue { get; set; }

    [JsonPropertyName("восстановление")]
    public string Recovery { get; set; } = "rest";
}

public sealed class ProficiencyRequest
{
    [JsonPropertyName("тип")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("значение")]
    public string Value { get; set; } = string.Empty;
}

public sealed class AbilityRequest
{
    [JsonPropertyName("категория")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("название")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("описание")]
    public string? Description { get; set; }

    [JsonPropertyName("тип")]
    public string AbilityType { get; set; } = "active";

    [JsonPropertyName("ресурсId")]
    public Guid? CostResourceId { get; set; }

    [JsonPropertyName("стоимость")]
    public int? CostAmount { get; set; }

    [JsonPropertyName("эффекты")]
    public JsonElement? Effects { get; set; }
}

public sealed class InventoryItemRequest
{
    [JsonPropertyName("templateId")]
    public string? TemplateId { get; set; }

    [JsonPropertyName("template_id")]
    public string? TemplateIdSnake { get; set; }

    [JsonPropertyName("название")]
    public string? Name { get; set; }

    [JsonPropertyName("name")]
    public string? NameAlias { get; set; }

    [JsonPropertyName("тип")]
    public string? ItemType { get; set; }

    [JsonPropertyName("item_type")]
    public string? ItemTypeSnake { get; set; }

    [JsonPropertyName("itemType")]
    public string? ItemTypeAlias { get; set; }

    [JsonPropertyName("подтип")]
    public string? Subtype { get; set; }

    [JsonPropertyName("subtype")]
    public string? SubtypeAlias { get; set; }

    [JsonPropertyName("описание")]
    public string? Description { get; set; }

    [JsonPropertyName("description")]
    public string? DescriptionAlias { get; set; }

    [JsonPropertyName("количество")]
    public int Quantity { get; set; } = 1;

    [JsonPropertyName("quantity")]
    public int? QuantityAlias { get; set; }

    [JsonPropertyName("stackable")]
    public bool StackableAlias { get; set; }

    [JsonPropertyName("стакуемый")]
    public bool Stackable { get; set; }

    [JsonPropertyName("вес")]
    public decimal WeightEach { get; set; }

    [JsonPropertyName("weight_each")]
    public decimal WeightEachSnake { get; set; }

    [JsonPropertyName("weight")]
    public decimal? WeightAlias { get; set; }

    [JsonPropertyName("состояние")]
    public string? Condition { get; set; }

    [JsonPropertyName("condition")]
    public string? ConditionAlias { get; set; }

    [JsonPropertyName("редкость")]
    public string? Rarity { get; set; }

    [JsonPropertyName("rarity")]
    public string? RarityAlias { get; set; }

    [JsonPropertyName("магический")]
    public bool IsMagical { get; set; }

    [JsonPropertyName("is_magical")]
    public bool IsMagicalSnake { get; set; }

    [JsonPropertyName("медные")]
    public int PriceCopper { get; set; }

    [JsonPropertyName("price_copper")]
    public int PriceCopperSnake { get; set; }

    [JsonPropertyName("серебряные")]
    public int PriceSilver { get; set; }

    [JsonPropertyName("price_silver")]
    public int PriceSilverSnake { get; set; }

    [JsonPropertyName("золотые")]
    public int PriceGold { get; set; }

    [JsonPropertyName("price_gold")]
    public int PriceGoldSnake { get; set; }

    [JsonPropertyName("платиновые")]
    public int PricePlatinum { get; set; }

    [JsonPropertyName("price_platinum")]
    public int PricePlatinumSnake { get; set; }

    [JsonPropertyName("теги")]
    public JsonElement? Tags { get; set; }

    [JsonPropertyName("slot")]
    public string? Slot { get; set; }

    [JsonPropertyName("слот")]
    public string? SlotRu { get; set; }

    [JsonPropertyName("properties")]
    public JsonElement? Properties { get; set; }

    [JsonPropertyName("свойства")]
    public JsonElement? PropertiesRu { get; set; }

    [JsonIgnore]
    public string? ResolvedName => string.IsNullOrWhiteSpace(Name)
        ? NameAlias?.Trim()
        : Name.Trim();

    [JsonIgnore]
    public string? ResolvedItemType => string.IsNullOrWhiteSpace(ItemType)
        ? string.IsNullOrWhiteSpace(ItemTypeAlias)
            ? ItemTypeSnake?.Trim()
            : ItemTypeAlias.Trim()
        : ItemType.Trim();

    [JsonIgnore]
    public int ResolvedQuantity => QuantityAlias ?? Quantity;

    [JsonIgnore]
    public decimal ResolvedWeightEach => WeightAlias ?? (WeightEach != 0 ? WeightEach : WeightEachSnake);

    [JsonIgnore]
    public string? ResolvedSlot => string.IsNullOrWhiteSpace(SlotRu)
        ? Slot?.Trim()
        : SlotRu.Trim();

    [JsonIgnore]
    public JsonElement? ResolvedProperties => PropertiesRu ?? Properties;
}

public sealed class InventoryItemActionRequest
{
    [JsonPropertyName("slot")]
    public string? Slot { get; set; }

    [JsonPropertyName("слот")]
    public string? SlotRu { get; set; }

    [JsonIgnore]
    public string? ResolvedSlot => string.IsNullOrWhiteSpace(SlotRu)
        ? Slot?.Trim()
        : SlotRu.Trim();
}

public sealed class EquipmentRequest
{
    [JsonPropertyName("голова")]
    public Guid? HeadItemId { get; set; }

    [JsonPropertyName("тело")]
    public Guid? BodyItemId { get; set; }

    [JsonPropertyName("руки")]
    public Guid? HandsItemId { get; set; }

    [JsonPropertyName("ноги")]
    public Guid? LegsItemId { get; set; }

    [JsonPropertyName("обувь")]
    public Guid? FeetItemId { get; set; }

    [JsonPropertyName("основнаяРука")]
    public Guid? MainHandItemId { get; set; }

    [JsonPropertyName("втораяРука")]
    public Guid? OffHandItemId { get; set; }

    [JsonPropertyName("амулет")]
    public Guid? AmuletItemId { get; set; }

    [JsonPropertyName("кольцо1")]
    public Guid? Ring1ItemId { get; set; }

    [JsonPropertyName("кольцо2")]
    public Guid? Ring2ItemId { get; set; }
}

public sealed class AttackRequest
{
    [JsonPropertyName("предметId")]
    public Guid? ItemId { get; set; }

    [JsonPropertyName("название")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("бросок")]
    public string Roll { get; set; } = string.Empty;

    [JsonPropertyName("урон")]
    public string Damage { get; set; } = string.Empty;

    [JsonPropertyName("типУрона")]
    public string? DamageType { get; set; }
}

public sealed class NeedsRequest
{
    [JsonPropertyName("размерЕды")]
    public string FoodSize { get; set; } = "средний";

    [JsonPropertyName("едыВДень")]
    public string? FoodPerDay { get; set; }

    [JsonPropertyName("едыОсталось")]
    public decimal FoodRemaining { get; set; }

    [JsonPropertyName("размерВоды")]
    public string WaterSize { get; set; } = "средний";

    [JsonPropertyName("водыВДень")]
    public string? WaterPerDay { get; set; }

    [JsonPropertyName("водыОсталось")]
    public decimal WaterRemaining { get; set; }

    [JsonPropertyName("грузМаксимум")]
    public decimal CarryCapacityMax { get; set; }

    [JsonPropertyName("текущийВес")]
    public decimal CarryCurrentWeight { get; set; }

    [JsonPropertyName("единицаВеса")]
    public string CarryUnit { get; set; } = "кг";

    [JsonPropertyName("метровЗаХод")]
    public int MovementMetersPerTurn { get; set; } = 9;

    [JsonPropertyName("кмВДень")]
    public int MovementKmPerDay { get; set; } = 36;
}
