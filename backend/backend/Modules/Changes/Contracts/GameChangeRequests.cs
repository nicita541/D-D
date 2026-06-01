using System.Text.Json.Serialization;

namespace backend.Contracts.Rpg.Changes;

public sealed class RejectGameChangeRequest
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("причина")]
    public string? ReasonRu { get; set; }

    [JsonIgnore]
    public string ResolvedReason => string.IsNullOrWhiteSpace(ReasonRu)
        ? Reason?.Trim() ?? "Rejected by user."
        : ReasonRu.Trim();
}
