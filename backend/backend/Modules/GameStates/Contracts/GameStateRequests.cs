using System.Text.Json.Serialization;

namespace backend.Modules.GameStates.Contracts;

public sealed class CreateGameStateRequest
{
    [JsonPropertyName("название")]
    public string? Name { get; set; }
}
