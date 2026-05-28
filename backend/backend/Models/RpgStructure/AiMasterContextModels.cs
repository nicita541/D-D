using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Models.RpgStructure;

public sealed class AiMasterContext
{
    [JsonPropertyName("gameStateId")]
    public Guid GameStateId { get; set; }

    [JsonPropertyName("номерхода")]
    public int TurnNumber { get; set; }

    [JsonPropertyName("режим")]
    public string Mode { get; set; } = "exploration";

    [JsonPropertyName("сюжет")]
    public JsonElement? Story { get; set; }

    [JsonPropertyName("партия")]
    public JsonElement? Party { get; set; }

    [JsonPropertyName("игрок")]
    public JsonElement? Player { get; set; }

    [JsonPropertyName("мир")]
    public JsonElement? World { get; set; }

    [JsonPropertyName("квесты")]
    public JsonElement? Quests { get; set; }

    [JsonPropertyName("последниеСобытия")]
    public JsonElement? RecentHistory { get; set; }

    [JsonPropertyName("бой")]
    public JsonElement? Combat { get; set; }
}
