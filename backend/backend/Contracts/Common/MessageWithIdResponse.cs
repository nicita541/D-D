using System.Text.Json.Serialization;

namespace backend.Contracts.Common;

public sealed class MessageWithIdResponse
{
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("id")]
    public required Guid Id { get; init; }
}
