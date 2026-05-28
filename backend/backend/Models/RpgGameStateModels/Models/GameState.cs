using System.Text.Json.Serialization;

namespace backend.Models;

public class GameState
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("версиясхемы")]
    public string SchemaVersion { get; set; } = "1.0";

    [JsonPropertyName("номерхода")]
    public int TurnNumber { get; set; }

    [JsonPropertyName("режим")]
    public string Mode { get; set; } = "exploration"; // exploration, combat, dialogue, travel, rest

    [JsonPropertyName("игрок")]
    public Player Player { get; set; } = new();

    [JsonPropertyName("мир")]
    public WorldState World { get; set; } = new();

    [JsonPropertyName("квесты")]
    public List<Quest> Quests { get; set; } = new();

    [JsonPropertyName("журнал")]
    public List<GameLogEntry> History { get; set; } = new();
}

public class GameLogEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = $"log_{Guid.NewGuid():N}";

    [JsonPropertyName("номерхода")]
    public int TurnNumber { get; set; }

    [JsonPropertyName("тип")]
    public string Type { get; set; } = "событие"; // ход_игрока, ответ_мастера, бой, награда, системное

    [JsonPropertyName("текст")]
    public string Text { get; set; } = "";

    [JsonPropertyName("важное")]
    public bool Important { get; set; }
}
