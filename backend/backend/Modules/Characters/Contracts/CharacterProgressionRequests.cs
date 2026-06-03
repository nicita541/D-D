using System.Text.Json.Serialization;

namespace backend.Modules.Characters.Contracts;

public sealed class AddExperienceRequest
{
    [JsonPropertyName("\u043e\u043f\u044b\u0442")]
    public int? ExperienceRuUtf8 { get; set; }

    [JsonPropertyName("\u0420\u0455\u0420\u0457\u0421\u2039\u0421\u201a")]
    public int? ExperienceRu { get; set; }

    [JsonPropertyName("experience")]
    public int? Experience { get; set; }

    [JsonPropertyName("amount")]
    public int? Amount { get; set; }

    [JsonPropertyName("\u043f\u0440\u0438\u0447\u0438\u043d\u0430")]
    public string? ReasonRuUtf8 { get; set; }

    [JsonPropertyName("\u0420\u0457\u0421\u0402\u0420\u0451\u0421\u2021\u0420\u0451\u0420\u0405\u0420\u00b0")]
    public string? ReasonRu { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonIgnore]
    public int? ResolvedExperience => ExperienceRuUtf8 ?? ExperienceRu ?? Experience ?? Amount;

    [JsonIgnore]
    public string ResolvedReason
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ReasonRuUtf8))
            {
                return ReasonRuUtf8.Trim();
            }

            if (!string.IsNullOrWhiteSpace(ReasonRu))
            {
                return ReasonRu.Trim();
            }

            return Reason?.Trim() ?? string.Empty;
        }
    }
}

public sealed class LevelUpRequest
{
    [JsonPropertyName("\u043d\u043e\u0432\u044b\u0439\u0423\u0440\u043e\u0432\u0435\u043d\u044c")]
    public int? NewLevelRuUtf8 { get; set; }

    [JsonPropertyName("\u0420\u0405\u0420\u0455\u0420\u0406\u0421\u2039\u0420\u2116\u0420\u0408\u0421\u0402\u0420\u0455\u0420\u0406\u0420\u00b5\u0420\u0405\u0421\u040a")]
    public int? NewLevelRu { get; set; }

    [JsonPropertyName("newLevel")]
    public int? NewLevel { get; set; }

    [JsonPropertyName("\u0445\u043f\u041c\u0430\u043a\u0441\u0438\u043c\u0443\u043c\u0414\u043e\u0431\u0430\u0432\u0438\u0442\u044c")]
    public int? HpMaxAddRuUtf8 { get; set; }

    [JsonPropertyName("\u0421\u2026\u0420\u0457\u0420\u045a\u0420\u00b0\u0420\u0454\u0421\u0403\u0420\u0451\u0420\u0458\u0421\u0453\u0420\u0458\u0420\u201d\u0420\u0455\u0420\u00b1\u0420\u00b0\u0420\u0406\u0420\u0451\u0421\u201a\u0421\u040a")]
    public int? HpMaxAddRu { get; set; }

    [JsonPropertyName("hpMaxAdd")]
    public int? HpMaxAdd { get; set; }

    [JsonPropertyName("\u0437\u0430\u043c\u0435\u0442\u043a\u0430")]
    public string? NoteRu { get; set; }

    [JsonPropertyName("\u0420\u00b7\u0420\u00b0\u0420\u0458\u0420\u00b5\u0421\u201a\u0420\u0454\u0420\u00b0")]
    public string? NoteLegacyRu { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonIgnore]
    public int? ResolvedNewLevel => NewLevelRuUtf8 ?? NewLevelRu ?? NewLevel;

    [JsonIgnore]
    public int? ResolvedHpMaxAdd => HpMaxAddRuUtf8 ?? HpMaxAddRu ?? HpMaxAdd;

    [JsonIgnore]
    public string ResolvedNote
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(NoteRu))
            {
                return NoteRu.Trim();
            }

            if (!string.IsNullOrWhiteSpace(NoteLegacyRu))
            {
                return NoteLegacyRu.Trim();
            }

            return Note?.Trim() ?? string.Empty;
        }
    }
}
