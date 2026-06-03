using System.Text.Json.Serialization;

namespace backend.Modules.Rest.Contracts;

public sealed class RestRequest
{
    [JsonPropertyName("characterId")]
    public Guid? CharacterId { get; set; }

    [JsonPropertyName("персонажId")]
    public Guid? CharacterIdRu { get; set; }

    [JsonPropertyName("minutes")]
    public int? Minutes { get; set; }

    [JsonPropertyName("минуты")]
    public int? MinutesRu { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("причина")]
    public string? ReasonRu { get; set; }

    [JsonIgnore]
    public Guid? ResolvedCharacterId => CharacterIdRu ?? CharacterId;

    [JsonIgnore]
    public int? ResolvedMinutes => MinutesRu ?? Minutes;

    [JsonIgnore]
    public string ResolvedReason => string.IsNullOrWhiteSpace(ReasonRu)
        ? Reason?.Trim() ?? string.Empty
        : ReasonRu.Trim();
}
