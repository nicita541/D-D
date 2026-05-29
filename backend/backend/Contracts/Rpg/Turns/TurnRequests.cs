using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Contracts.Rpg.Turns;

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
}

public sealed record GameChangeProposal(
    string Operation,
    JsonElement Payload);

public sealed record PendingTurn(
    Guid Id,
    Guid GameStateId,
    Guid AccountId,
    int TurnNumber,
    string PlayerMessage);
