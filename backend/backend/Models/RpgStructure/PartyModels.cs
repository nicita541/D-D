using System.Text.Json.Serialization;

namespace backend.Models.RpgStructure;

public sealed class Party
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("gameStateId")]
    public Guid GameStateId { get; set; }

    [JsonPropertyName("название")]
    public string Name { get; set; } = "Партия";

    [JsonPropertyName("участники")]
    public List<PartyMember> Members { get; set; } = new();
}

public sealed class PartyMember
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

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
