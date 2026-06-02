using System.Text.Json.Serialization;

namespace backend.Shared.Contracts;

public sealed class OperationResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}

public sealed class MessageResponse
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}
