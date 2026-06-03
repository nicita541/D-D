using System.Text.Json.Serialization;

namespace backend.Modules.Conditions.Contracts;

public sealed class TickConditionsRequest
{
    [JsonPropertyName("turns")]
    public int? Turns { get; set; }

    [JsonPropertyName("ходы")]
    public int? TurnsRu { get; set; }

    [JsonIgnore]
    public int ResolvedTurns => Math.Max(0, TurnsRu ?? Turns ?? 1);
}

public sealed class KnockoutRequest
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("причина")]
    public string? ReasonRu { get; set; }

    [JsonIgnore]
    public string ResolvedReason => string.IsNullOrWhiteSpace(ReasonRu)
        ? Reason?.Trim() ?? string.Empty
        : ReasonRu.Trim();
}

public sealed class ReviveRequest
{
    [JsonPropertyName("hp")]
    public int? Hp { get; set; }

    [JsonPropertyName("хп")]
    public int? HpRu { get; set; }

    [JsonPropertyName("clearDead")]
    public bool? ClearDead { get; set; }

    [JsonPropertyName("очиститьСмерть")]
    public bool? ClearDeadRu { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("причина")]
    public string? ReasonRu { get; set; }

    [JsonIgnore]
    public int ResolvedHp => Math.Max(1, HpRu ?? Hp ?? 1);

    [JsonIgnore]
    public bool ResolvedClearDead => ClearDeadRu ?? ClearDead ?? true;

    [JsonIgnore]
    public string ResolvedReason => string.IsNullOrWhiteSpace(ReasonRu)
        ? Reason?.Trim() ?? string.Empty
        : ReasonRu.Trim();
}
