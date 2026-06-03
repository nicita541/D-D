using System.Text.Json.Serialization;

namespace backend.Modules.Story.Contracts;

public sealed class CreateOrUpdateStoryStateRequest
{
    [JsonPropertyName("campaignTemplateId")]
    public Guid? CampaignTemplateId { get; set; }

    [JsonPropertyName("текущаяглава")]
    public string? CurrentAct { get; set; }

    [JsonPropertyName("текущаясцена")]
    public string? CurrentScene { get; set; }

    [JsonPropertyName("текущаяцель")]
    public string? CurrentGoal { get; set; }

    [JsonPropertyName("напряжение")]
    public int TensionLevel { get; set; } = 1;

    [JsonPropertyName("сюжетныефлаги")]
    public Dictionary<string, object?> PlotFlags { get; set; } = new();

    [JsonPropertyName("открытыеФакты")]
    public List<string> KnownFacts { get; set; } = new();

    [JsonPropertyName("скрытыеФакты")]
    public List<string> HiddenFacts { get; set; } = new();

    [JsonPropertyName("краткаяПамять")]
    public List<string> ShortMemory { get; set; } = new();
}
