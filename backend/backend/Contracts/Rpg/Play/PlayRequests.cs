using System.Text.Json.Serialization;

namespace backend.Contracts.Rpg.Play;

public sealed class PlayContinueRequest
{
    [JsonPropertyName("сообщение")]
    public string? PlayerMessage { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("заметка")]
    public string? NoteRu { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonIgnore]
    public string ResolvedPlayerMessage => string.IsNullOrWhiteSpace(PlayerMessage)
        ? Message?.Trim() ?? string.Empty
        : PlayerMessage.Trim();

    [JsonIgnore]
    public string ResolvedNote => string.IsNullOrWhiteSpace(NoteRu)
        ? Note?.Trim() ?? string.Empty
        : NoteRu.Trim();
}
