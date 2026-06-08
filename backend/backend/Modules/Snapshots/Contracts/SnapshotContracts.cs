using System.Text.Json.Serialization;

namespace backend.Modules.Snapshots.Contracts;

public sealed class CreateSnapshotRequest
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

public sealed record SnapshotDto(
    Guid Id,
    Guid GameStateId,
    string Reason,
    Guid? CreatedByAccountId,
    DateTimeOffset CreatedAt,
    Guid? RestoredByAccountId,
    DateTimeOffset? RestoredAt);

public sealed record SnapshotRestoreResponse(
    Guid Id,
    Guid GameStateId,
    string Message);
