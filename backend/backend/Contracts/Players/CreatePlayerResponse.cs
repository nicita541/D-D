using System.Text.Json.Serialization;

namespace backend.Contracts.Players;

public sealed class CreatePlayerResponse
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
