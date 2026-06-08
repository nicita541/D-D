using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Modules.Turns.Contracts;

public sealed class CreateTurnRequest
{
    [JsonPropertyName("сообщение")]
    public string? PlayerMessage { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonIgnore]
    public string ResolvedPlayerMessage => string.IsNullOrWhiteSpace(PlayerMessage)
        ? Message?.Trim() ?? string.Empty
        : PlayerMessage.Trim();

    [JsonIgnore]
    public string? VisiblePlayerMessage { get; init; }

    [JsonIgnore]
    public string TurnSource { get; init; } = TurnSources.Player;

    [JsonIgnore]
    public string? ResolvedVisiblePlayerMessage => VisiblePlayerMessage is null
        ? string.Equals(TurnSource, TurnSources.Player, StringComparison.OrdinalIgnoreCase)
            ? ResolvedPlayerMessage
            : null
        : string.IsNullOrWhiteSpace(VisiblePlayerMessage)
            ? null
            : VisiblePlayerMessage.Trim();
}

public static class TurnSources
{
    public const string Player = "player";
    public const string System = "system";
}

public sealed record GameChangeProposal(
    string Operation,
    JsonElement Payload);

public sealed record PendingTurn(
    Guid Id,
    Guid GameStateId,
    Guid AccountId,
    int TurnNumber,
    string PlayerMessage,
    string? VisiblePlayerMessage,
    string TurnSource);

public enum PendingTurnCreationStatus
{
    Created,
    NotFound,
    Conflict
}

public sealed record PendingTurnCreationResult(PendingTurnCreationStatus Status, PendingTurn? Turn)
{
    public static PendingTurnCreationResult Created(PendingTurn turn) => new(PendingTurnCreationStatus.Created, turn);

    public static PendingTurnCreationResult NotFound() => new(PendingTurnCreationStatus.NotFound, null);

    public static PendingTurnCreationResult Conflict() => new(PendingTurnCreationStatus.Conflict, null);
}
