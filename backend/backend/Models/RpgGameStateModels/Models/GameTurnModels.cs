using System.Text.Json.Serialization;

namespace backend.Models;

public class GameTurnRequest
{
    [JsonPropertyName("playerId")]
    public Guid PlayerId { get; set; }

    [JsonPropertyName("сообщение")]
    public string Message { get; set; } = "";
}

public class GameTurnResponse
{
    [JsonPropertyName("ответмастера")]
    public string MasterAnswer { get; set; } = "";

    [JsonPropertyName("примененныеизменения")]
    public List<GameChange> AppliedChanges { get; set; } = new();

    [JsonPropertyName("отклоненныеизменения")]
    public List<RejectedChange> RejectedChanges { get; set; } = new();

    [JsonPropertyName("состояниеигры")]
    public GameState GameState { get; set; } = new();
}

public class AiMasterResponse
{
    [JsonPropertyName("ответмастера")]
    public string MasterAnswer { get; set; } = "";

    [JsonPropertyName("изменения")]
    public List<GameChange> Changes { get; set; } = new();
}

public class GameChange
{
    [JsonPropertyName("операция")]
    public string Operation { get; set; } = "";

    [JsonPropertyName("причина")]
    public string? Reason { get; set; }

    [JsonPropertyName("количество")]
    public int? Amount { get; set; }

    [JsonPropertyName("типмонет")]
    public string? CoinType { get; set; }

    [JsonPropertyName("предметId")]
    public string? ItemId { get; set; }

    [JsonPropertyName("предметНазвание")]
    public string? ItemName { get; set; }

    [JsonPropertyName("предмет")]
    public InventoryItem? Item { get; set; }

    [JsonPropertyName("из")]
    public string? From { get; set; }

    [JsonPropertyName("в")]
    public string? To { get; set; }

    [JsonPropertyName("слот")]
    public string? Slot { get; set; }

    [JsonPropertyName("значение")]
    public int? Value { get; set; }

    [JsonPropertyName("состояние")]
    public Condition? Condition { get; set; }

    [JsonPropertyName("локацияId")]
    public string? LocationId { get; set; }

    [JsonPropertyName("текст")]
    public string? Text { get; set; }

    [JsonPropertyName("квестId")]
    public string? QuestId { get; set; }

    [JsonPropertyName("статус")]
    public string? Status { get; set; }
}

public class RejectedChange
{
    [JsonPropertyName("изменение")]
    public GameChange Change { get; set; } = new();

    [JsonPropertyName("причина")]
    public string Reason { get; set; } = "";
}
