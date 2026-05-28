using System.Text.Json.Serialization;

namespace backend.Contracts.Common;

public sealed class MessageResponse
{
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
