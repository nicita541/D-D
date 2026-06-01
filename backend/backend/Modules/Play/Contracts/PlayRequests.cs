using System.Text.Json.Serialization;
using backend.Modules.Combat;

namespace backend.Modules.Play;

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

public sealed class PlayActRequest
{
    [JsonPropertyName("сообщение")]
    public string? MessageRu { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("персонажId")]
    public Guid? CharacterIdRu { get; set; }

    [JsonPropertyName("characterId")]
    public Guid? CharacterId { get; set; }

    [JsonPropertyName("режим")]
    public string? ModeRu { get; set; }

    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("autoApplySafeChanges")]
    public bool? AutoApplySafeChanges { get; set; }

    [JsonPropertyName("autoContinueAfterResolvedRoll")]
    public bool? AutoContinueAfterResolvedRoll { get; set; }

    [JsonPropertyName("заметка")]
    public string? NoteRu { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonIgnore]
    public string ResolvedMessage => string.IsNullOrWhiteSpace(MessageRu)
        ? Message?.Trim() ?? string.Empty
        : MessageRu.Trim();

    [JsonIgnore]
    public Guid? ResolvedCharacterId => CharacterIdRu ?? CharacterId;

    [JsonIgnore]
    public string ResolvedMode => string.IsNullOrWhiteSpace(ModeRu)
        ? Mode?.Trim() ?? "auto"
        : ModeRu.Trim();

    [JsonIgnore]
    public bool ResolvedAutoApplySafeChanges => AutoApplySafeChanges ?? true;

    [JsonIgnore]
    public bool ResolvedAutoContinueAfterResolvedRoll => AutoContinueAfterResolvedRoll ?? true;

    [JsonIgnore]
    public string ResolvedNote => string.IsNullOrWhiteSpace(NoteRu)
        ? Note?.Trim() ?? string.Empty
        : NoteRu.Trim();
}

public sealed class PlayResolveAndContinueRequest
{
    [JsonPropertyName("персонажId")]
    public Guid? CharacterIdRu { get; set; }

    [JsonPropertyName("characterId")]
    public Guid? CharacterId { get; set; }

    [JsonPropertyName("roll")]
    public int? Roll { get; set; }

    [JsonPropertyName("modifier")]
    public int? Modifier { get; set; }

    [JsonPropertyName("заметка")]
    public string? NoteRu { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonIgnore]
    public Guid? ResolvedCharacterId => CharacterIdRu ?? CharacterId;

    [JsonIgnore]
    public string ResolvedNote => string.IsNullOrWhiteSpace(NoteRu)
        ? Note?.Trim() ?? string.Empty
        : NoteRu.Trim();
}

public sealed class PlayTravelRequest
{
    [JsonPropertyName("целеваяЛокацияId")]
    public Guid? TargetLocationIdRu { get; set; }

    [JsonPropertyName("targetLocationId")]
    public Guid? TargetLocationId { get; set; }

    [JsonPropertyName("выходId")]
    public Guid? ExitIdRu { get; set; }

    [JsonPropertyName("exitId")]
    public Guid? ExitId { get; set; }

    [JsonPropertyName("заметка")]
    public string? NoteRu { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonIgnore]
    public Guid? ResolvedTargetLocationId => TargetLocationIdRu ?? TargetLocationId;

    [JsonIgnore]
    public Guid? ResolvedExitId => ExitIdRu ?? ExitId;

    [JsonIgnore]
    public string ResolvedNote => string.IsNullOrWhiteSpace(NoteRu)
        ? Note?.Trim() ?? string.Empty
        : NoteRu.Trim();
}

public sealed class PlayCombatStartRequest
{
    [JsonPropertyName("участники")]
    public List<AddCombatParticipantRequest> ParticipantsRu { get; set; } = new();

    [JsonPropertyName("participants")]
    public List<AddCombatParticipantRequest> Participants { get; set; } = new();

    [JsonPropertyName("autoAddParty")]
    public bool? AutoAddParty { get; set; }

    [JsonPropertyName("заметка")]
    public string? NoteRu { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonIgnore]
    public IReadOnlyList<AddCombatParticipantRequest> ResolvedParticipants =>
        ParticipantsRu.Count > 0 ? ParticipantsRu : Participants;

    [JsonIgnore]
    public bool ResolvedAutoAddParty => AutoAddParty ?? true;

    [JsonIgnore]
    public string ResolvedNote => string.IsNullOrWhiteSpace(NoteRu)
        ? Note?.Trim() ?? string.Empty
        : NoteRu.Trim();
}

public sealed class PlayCombatActionRequest
{
    [JsonPropertyName("действие")]
    public string? ActionRu { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("заметка")]
    public string? NoteRu { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("attack")]
    public CombatAttackRequest? Attack { get; set; }

    [JsonIgnore]
    public string ResolvedAction => string.IsNullOrWhiteSpace(ActionRu)
        ? Action?.Trim() ?? string.Empty
        : ActionRu.Trim();

    [JsonIgnore]
    public string ResolvedNote => string.IsNullOrWhiteSpace(NoteRu)
        ? Note?.Trim() ?? string.Empty
        : NoteRu.Trim();
}
