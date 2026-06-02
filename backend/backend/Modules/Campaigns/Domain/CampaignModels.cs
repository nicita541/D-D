using System.Text.Json.Serialization;

namespace backend.Modules.Campaigns.Domain;

public sealed class CampaignTemplate
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("название")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("жанр")]
    public string? Genre { get; set; }

    [JsonPropertyName("тон")]
    public string? Tone { get; set; }

    [JsonPropertyName("краткоеописание")]
    public string? Summary { get; set; }

    [JsonPropertyName("вступление")]
    public string? OpeningScene { get; set; }

    [JsonPropertyName("главнаяцель")]
    public string? MainGoal { get; set; }

    [JsonPropertyName("секретымастера")]
    public List<string> MasterSecrets { get; set; } = new();

    [JsonPropertyName("начальныефлаги")]
    public Dictionary<string, object?> InitialFlags { get; set; } = new();
}

public sealed class StoryState
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("gameStateId")]
    public Guid GameStateId { get; set; }

    [JsonPropertyName("campaignTemplateId")]
    public Guid? CampaignTemplateId { get; set; }

    [JsonPropertyName("названиеКампании")]
    public string? CampaignTitle { get; set; }

    [JsonPropertyName("текущаяглава")]
    public string? CurrentAct { get; set; }

    [JsonPropertyName("текущаясцена")]
    public string? CurrentScene { get; set; }

    [JsonPropertyName("текущаяцель")]
    public string? CurrentGoal { get; set; }

    [JsonPropertyName("напряжение")]
    public int TensionLevel { get; set; }

    [JsonPropertyName("сюжетныефлаги")]
    public Dictionary<string, object?> PlotFlags { get; set; } = new();

    [JsonPropertyName("открытыеФакты")]
    public List<string> KnownFacts { get; set; } = new();

    [JsonPropertyName("скрытыеФакты")]
    public List<string> HiddenFacts { get; set; } = new();

    [JsonPropertyName("краткаяПамять")]
    public List<string> ShortMemory { get; set; } = new();
}
