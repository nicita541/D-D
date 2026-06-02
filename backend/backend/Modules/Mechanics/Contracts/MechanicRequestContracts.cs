using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Modules.Mechanics.Contracts;

public sealed class MechanicRequestResolveAbilityCheckRequest
{
    [JsonPropertyName("персонажId")]
    public Guid? CharacterIdRu { get; set; }

    [JsonPropertyName("characterId")]
    public Guid? CharacterId { get; set; }

    [JsonIgnore]
    public Guid? ResolvedCharacterId => CharacterIdRu ?? CharacterId;
}

public sealed record MechanicRequestRecord(
    Guid Id,
    Guid GameStateId,
    Guid? TurnId,
    Guid? ChangeId,
    string RequestType,
    JsonElement Payload,
    string Status,
    JsonElement Result,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);
