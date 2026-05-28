using System.Text.Json.Serialization;

namespace backend.Contracts.Rpg.GameStates;

public sealed class CreateGameStateRequest
{
    [JsonPropertyName("название")]
    public string? Name { get; set; }
}
