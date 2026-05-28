using System.Text.Json.Serialization;

namespace backend.Contracts.GameStates;

public sealed class CreateGameStateResponse
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("accountId")]
    public required Guid AccountId { get; init; }

    [JsonPropertyName("saveName")]
    public required string SaveName { get; init; }
}
