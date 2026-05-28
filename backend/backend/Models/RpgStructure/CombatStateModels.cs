using System.Text.Json.Serialization;

namespace backend.Models.RpgStructure;

public sealed class CombatState
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("gameStateId")]
    public Guid GameStateId { get; set; }

    [JsonPropertyName("активен")]
    public bool IsActive { get; set; }

    [JsonPropertyName("раунд")]
    public int RoundNumber { get; set; } = 1;

    [JsonPropertyName("текущийУчастникId")]
    public Guid? CurrentTurnParticipantId { get; set; }

    [JsonPropertyName("участники")]
    public List<CombatParticipant> Participants { get; set; } = new();
}

public sealed class CombatParticipant
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

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

    [JsonPropertyName("ужеДействовал")]
    public bool HasActed { get; set; }

    [JsonPropertyName("состояния")]
    public List<string> Conditions { get; set; } = new();
}
