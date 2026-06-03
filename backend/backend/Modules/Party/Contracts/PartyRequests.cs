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
