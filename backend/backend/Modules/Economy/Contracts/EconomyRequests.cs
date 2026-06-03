using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Modules.Economy.Contracts;

public sealed class CreateLootContainerRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("название")]
    public string? NameRu { get; set; }

    [JsonPropertyName("sourceType")]
    public string? SourceType { get; set; }

    [JsonPropertyName("типИсточника")]
    public string? SourceTypeRu { get; set; }

    [JsonPropertyName("sourceId")]
    public Guid? SourceId { get; set; }

    [JsonPropertyName("источникId")]
    public Guid? SourceIdRu { get; set; }

    [JsonPropertyName("currencyAmount")]
    public int? CurrencyAmount { get; set; }

    [JsonPropertyName("золото")]
    public int? CurrencyAmountRu { get; set; }

    [JsonPropertyName("items")]
    public List<CreateLootItemRequest>? Items { get; set; }

    [JsonPropertyName("предметы")]
    public List<CreateLootItemRequest>? ItemsRu { get; set; }

    [JsonPropertyName("metadata")]
    public JsonElement? Metadata { get; set; }

    [JsonPropertyName("данные")]
    public JsonElement? MetadataRu { get; set; }

    [JsonIgnore]
    public string ResolvedName => string.IsNullOrWhiteSpace(NameRu) ? Name?.Trim() ?? string.Empty : NameRu.Trim();

    [JsonIgnore]
    public string? ResolvedSourceType => string.IsNullOrWhiteSpace(SourceTypeRu) ? SourceType?.Trim() : SourceTypeRu.Trim();

    [JsonIgnore]
    public Guid? ResolvedSourceId => SourceIdRu ?? SourceId;

    [JsonIgnore]
    public int ResolvedCurrencyAmount => Math.Max(0, CurrencyAmountRu ?? CurrencyAmount ?? 0);

    [JsonIgnore]
    public IReadOnlyList<CreateLootItemRequest> ResolvedItems => ItemsRu ?? Items ?? new List<CreateLootItemRequest>();

    [JsonIgnore]
    public JsonElement? ResolvedMetadata => MetadataRu ?? Metadata;
}

public sealed class CreateLootItemRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("название")]
    public string? NameRu { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("описание")]
    public string? DescriptionRu { get; set; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    [JsonPropertyName("количество")]
    public int? QuantityRu { get; set; }

    [JsonPropertyName("itemType")]
    public string? ItemType { get; set; }

    [JsonPropertyName("тип")]
    public string? ItemTypeRu { get; set; }

    [JsonPropertyName("rarity")]
    public string? Rarity { get; set; }

    [JsonPropertyName("редкость")]
    public string? RarityRu { get; set; }

    [JsonPropertyName("metadata")]
    public JsonElement? Metadata { get; set; }

    [JsonPropertyName("свойства")]
    public JsonElement? MetadataRu { get; set; }

    [JsonIgnore]
    public string ResolvedName => string.IsNullOrWhiteSpace(NameRu) ? Name?.Trim() ?? string.Empty : NameRu.Trim();

    [JsonIgnore]
    public string? ResolvedDescription => string.IsNullOrWhiteSpace(DescriptionRu) ? Description?.Trim() : DescriptionRu.Trim();

    [JsonIgnore]
    public int ResolvedQuantity => Math.Max(1, QuantityRu ?? Quantity ?? 1);

    [JsonIgnore]
    public string ResolvedItemType => string.IsNullOrWhiteSpace(ItemTypeRu) ? ItemType?.Trim() ?? "misc" : ItemTypeRu.Trim();

    [JsonIgnore]
    public string ResolvedRarity => string.IsNullOrWhiteSpace(RarityRu) ? Rarity?.Trim() ?? "common" : RarityRu.Trim();

    [JsonIgnore]
    public JsonElement? ResolvedMetadata => MetadataRu ?? Metadata;
}

public sealed class ClaimLootRequest
{
    [JsonPropertyName("characterId")]
    public Guid? CharacterId { get; set; }

    [JsonPropertyName("персонажId")]
    public Guid? CharacterIdRu { get; set; }

    [JsonIgnore]
    public Guid? ResolvedCharacterId => CharacterIdRu ?? CharacterId;
}

public sealed class CurrencyChangeRequest
{
    [JsonPropertyName("amount")]
    public int? Amount { get; set; }

    [JsonPropertyName("золото")]
    public int? AmountRu { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("причина")]
    public string? ReasonRu { get; set; }

    [JsonIgnore]
    public int? ResolvedAmount => AmountRu ?? Amount;

    [JsonIgnore]
    public string ResolvedReason => string.IsNullOrWhiteSpace(ReasonRu) ? Reason?.Trim() ?? string.Empty : ReasonRu.Trim();
}

public sealed class GrantQuestRewardRequest
{
    [JsonPropertyName("characterId")]
    public Guid? CharacterId { get; set; }

    [JsonPropertyName("персонажId")]
    public Guid? CharacterIdRu { get; set; }

    [JsonPropertyName("xpAmount")]
    public int? XpAmount { get; set; }

    [JsonPropertyName("опыт")]
    public int? XpAmountRu { get; set; }

    [JsonPropertyName("currencyAmount")]
    public int? CurrencyAmount { get; set; }

    [JsonPropertyName("золото")]
    public int? CurrencyAmountRu { get; set; }

    [JsonPropertyName("items")]
    public List<CreateLootItemRequest>? Items { get; set; }

    [JsonPropertyName("предметы")]
    public List<CreateLootItemRequest>? ItemsRu { get; set; }

    [JsonIgnore]
    public Guid? ResolvedCharacterId => CharacterIdRu ?? CharacterId;

    [JsonIgnore]
    public int ResolvedXpAmount => Math.Max(0, XpAmountRu ?? XpAmount ?? 0);

    [JsonIgnore]
    public int ResolvedCurrencyAmount => Math.Max(0, CurrencyAmountRu ?? CurrencyAmount ?? 0);

    [JsonIgnore]
    public IReadOnlyList<CreateLootItemRequest> ResolvedItems => ItemsRu ?? Items ?? new List<CreateLootItemRequest>();
}
