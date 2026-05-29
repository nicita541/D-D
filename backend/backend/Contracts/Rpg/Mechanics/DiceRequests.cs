using System.Text.Json.Serialization;

namespace backend.Contracts.Rpg.Mechanics;

public sealed class RollDiceRequest
{
    [JsonPropertyName("формула")]
    public string? FormulaRu { get; set; }

    [JsonPropertyName("formula")]
    public string? Formula { get; set; }

    [JsonPropertyName("причина")]
    public string? ReasonRu { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("персонажId")]
    public Guid? CharacterIdRu { get; set; }

    [JsonPropertyName("characterId")]
    public Guid? CharacterId { get; set; }

    [JsonIgnore]
    public string ResolvedFormula => string.IsNullOrWhiteSpace(FormulaRu)
        ? Formula?.Trim() ?? string.Empty
        : FormulaRu.Trim();

    [JsonIgnore]
    public string ResolvedReason => string.IsNullOrWhiteSpace(ReasonRu)
        ? Reason?.Trim() ?? string.Empty
        : ReasonRu.Trim();

    [JsonIgnore]
    public Guid? ResolvedCharacterId => CharacterIdRu ?? CharacterId;
}

public sealed class AbilityCheckRequest
{
    [JsonPropertyName("персонажId")]
    public Guid? CharacterIdRu { get; set; }

    [JsonPropertyName("characterId")]
    public Guid? CharacterId { get; set; }

    [JsonPropertyName("характеристика")]
    public string? AbilityRu { get; set; }

    [JsonPropertyName("ability")]
    public string? Ability { get; set; }

    [JsonPropertyName("сложность")]
    public int? DifficultyClassRu { get; set; }

    [JsonPropertyName("difficultyClass")]
    public int? DifficultyClass { get; set; }

    [JsonPropertyName("причина")]
    public string? ReasonRu { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonIgnore]
    public Guid? ResolvedCharacterId => CharacterIdRu ?? CharacterId;

    [JsonIgnore]
    public string ResolvedAbility => string.IsNullOrWhiteSpace(AbilityRu)
        ? Ability?.Trim() ?? string.Empty
        : AbilityRu.Trim();

    [JsonIgnore]
    public int? ResolvedDifficultyClass => DifficultyClassRu ?? DifficultyClass;

    [JsonIgnore]
    public string ResolvedReason => string.IsNullOrWhiteSpace(ReasonRu)
        ? Reason?.Trim() ?? string.Empty
        : ReasonRu.Trim();
}
