using System.Text.Json.Serialization;

namespace backend.Models;

public class Quest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = $"quest_{Guid.NewGuid():N}";

    [JsonPropertyName("название")]
    public string Title { get; set; } = "";

    [JsonPropertyName("описание")]
    public string Description { get; set; } = "";

    [JsonPropertyName("статус")]
    public string Status { get; set; } = "активен"; // активен, выполнен, провален, скрыт

    [JsonPropertyName("этапы")]
    public List<QuestStep> Steps { get; set; } = new();

    [JsonPropertyName("награды")]
    public QuestReward Reward { get; set; } = new();
}

public class QuestStep
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = $"step_{Guid.NewGuid():N}";

    [JsonPropertyName("описание")]
    public string Description { get; set; } = "";

    [JsonPropertyName("выполнен")]
    public bool IsCompleted { get; set; }
}

public class QuestReward
{
    [JsonPropertyName("опыт")]
    public int Experience { get; set; }

    [JsonPropertyName("монеты")]
    public Coins Coins { get; set; } = new();

    [JsonPropertyName("предметы")]
    public List<InventoryItem> Items { get; set; } = new();
}
