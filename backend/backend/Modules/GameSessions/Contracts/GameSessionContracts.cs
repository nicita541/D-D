using System.Text.Json.Serialization;

namespace backend.Modules.GameSessions.Contracts;

public sealed class StartGameSessionRequest
{
    [JsonPropertyName("accountCharacterId")]
    public Guid AccountCharacterId { get; set; }

    [JsonPropertyName("campaignTemplateId")]
    public Guid CampaignTemplateId { get; set; }

    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonIgnore]
    public string ResolvedMode => string.IsNullOrWhiteSpace(Mode) ? "solo" : Mode.Trim().ToLowerInvariant();
}

public sealed record StartGameSessionResponse(
    Guid GameStateId,
    Guid CharacterId,
    string PlayUrl);
