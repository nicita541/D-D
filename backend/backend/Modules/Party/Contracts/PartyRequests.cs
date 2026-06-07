using System.Text.Json.Serialization;

namespace backend.Modules.Party.Contracts;

public sealed class CreatePartyRequest
{
    [JsonPropertyName("название")]
    public string? Name { get; set; }
}

public sealed class AddPartyMemberRequest
{
    [JsonPropertyName("accountId")]
    public Guid? AccountId { get; set; }

    [JsonPropertyName("characterId")]
    public Guid? CharacterId { get; set; }

    [JsonPropertyName("роль")]
    public string Role { get; set; } = "player";

    [JsonPropertyName("статус")]
    public string Status { get; set; } = "active";

    [JsonPropertyName("отображаемоеИмя")]
    public string? DisplayName { get; set; }
}

public sealed class UpdatePartyMemberRequest
{
    [JsonPropertyName("accountId")]
    public Guid? AccountId { get; set; }

    [JsonPropertyName("characterId")]
    public Guid? CharacterId { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("роль")]
    public string? RoleRu { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("статус")]
    public string? StatusRu { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("отображаемоеИмя")]
    public string? DisplayNameRu { get; set; }

    [JsonIgnore]
    public string? ResolvedRole => string.IsNullOrWhiteSpace(RoleRu) ? Role?.Trim() : RoleRu.Trim();

    [JsonIgnore]
    public string? ResolvedStatus => string.IsNullOrWhiteSpace(StatusRu) ? Status?.Trim() : StatusRu.Trim();

    [JsonIgnore]
    public string? ResolvedDisplayName => string.IsNullOrWhiteSpace(DisplayNameRu) ? DisplayName?.Trim() : DisplayNameRu.Trim();
}

public sealed class AssignPartyMemberCharacterRequest
{
    [JsonPropertyName("characterId")]
    public Guid? CharacterId { get; set; }

    [JsonPropertyName("персонажId")]
    public Guid? CharacterIdRu { get; set; }

    [JsonIgnore]
    public Guid? ResolvedCharacterId => CharacterIdRu ?? CharacterId;
}
