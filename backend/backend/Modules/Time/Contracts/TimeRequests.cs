using System.Text.Json.Serialization;

namespace backend.Modules.Time.Contracts;

public sealed class AdvanceTimeRequest
{
    [JsonPropertyName("minutes")]
    public int? Minutes { get; set; }

    [JsonPropertyName("минуты")]
    public int? MinutesRu { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("причина")]
    public string? ReasonRu { get; set; }

    [JsonPropertyName("tickConditions")]
    public bool? TickConditions { get; set; }

    [JsonPropertyName("обновитьСостояния")]
    public bool? TickConditionsRu { get; set; }

    [JsonIgnore]
    public int? ResolvedMinutes => MinutesRu ?? Minutes;

    [JsonIgnore]
    public bool ResolvedTickConditions => TickConditionsRu ?? TickConditions ?? true;

    [JsonIgnore]
    public string ResolvedReason => string.IsNullOrWhiteSpace(ReasonRu)
        ? Reason?.Trim() ?? string.Empty
        : ReasonRu.Trim();
}
