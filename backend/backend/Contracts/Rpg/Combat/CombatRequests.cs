using System.Text.Json.Serialization;

namespace backend.Contracts.Rpg.Combat;

public sealed class StartCombatRequest
{
    [JsonPropertyName("участники")]
    public List<AddCombatParticipantRequest> Participants { get; set; } = new();
}

public sealed class AddCombatParticipantRequest
{
    [JsonPropertyName("типАктера")]
    public string ActorType { get; set; } = "character";

    [JsonPropertyName("actorId")]
    public Guid ActorId { get; set; }

    [JsonPropertyName("имя")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("инициатива")]
    public int Initiative { get; set; }

    [JsonPropertyName("хпТекущее")]
    public int HpCurrent { get; set; }

    [JsonPropertyName("хпМаксимум")]
    public int HpMax { get; set; }

    [JsonPropertyName("состояния")]
    public List<string> Conditions { get; set; } = new();
}
