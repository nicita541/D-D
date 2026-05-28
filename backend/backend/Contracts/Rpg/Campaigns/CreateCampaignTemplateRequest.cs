using System.Text.Json.Serialization;

namespace backend.Contracts.Rpg.Campaigns;

public sealed class CreateCampaignTemplateRequest
{
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
