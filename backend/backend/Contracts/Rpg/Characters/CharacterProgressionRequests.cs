using System.Text.Json.Serialization;

namespace backend.Contracts.Rpg.Characters;

public sealed class AddExperienceRequest
{
    [JsonPropertyName("опыт")]
    public int? ExperienceRu { get; set; }

    [JsonPropertyName("experience")]
    public int? Experience { get; set; }

    [JsonPropertyName("причина")]
    public string? ReasonRu { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonIgnore]
    public int? ResolvedExperience => ExperienceRu ?? Experience;

    [JsonIgnore]
    public string ResolvedReason => string.IsNullOrWhiteSpace(ReasonRu)
        ? Reason?.Trim() ?? string.Empty
        : ReasonRu.Trim();
}

public sealed class LevelUpRequest
{
    [JsonPropertyName("новыйУровень")]
    public int? NewLevelRu { get; set; }

    [JsonPropertyName("newLevel")]
    public int? NewLevel { get; set; }

    [JsonPropertyName("хпМаксимумДобавить")]
    public int? HpMaxAddRu { get; set; }

    [JsonPropertyName("hpMaxAdd")]
    public int? HpMaxAdd { get; set; }

    [JsonIgnore]
    public int? ResolvedNewLevel => NewLevelRu ?? NewLevel;

    [JsonIgnore]
    public int ResolvedHpMaxAdd => HpMaxAddRu ?? HpMaxAdd ?? 0;
}
