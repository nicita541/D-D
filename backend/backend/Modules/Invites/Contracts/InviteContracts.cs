using System.Text.Json.Serialization;

namespace backend.Modules.Invites.Contracts;

public sealed class CreateInviteRequest
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "player";

    [JsonPropertyName("роль")]
    public string? RoleRu { get; set; }

    [JsonPropertyName("expiresInHours")]
    public int? ExpiresInHours { get; set; }

    [JsonPropertyName("maxUses")]
    public int? MaxUses { get; set; }

    [JsonIgnore]
    public string ResolvedRole => string.IsNullOrWhiteSpace(RoleRu) ? Role.Trim() : RoleRu.Trim();

    [JsonIgnore]
    public int ResolvedExpiresInHours => Math.Clamp(ExpiresInHours ?? 168, 1, 24 * 30);

    [JsonIgnore]
    public int ResolvedMaxUses => Math.Clamp(MaxUses ?? 1, 1, 50);
}

public sealed record InviteCreatedResponse(
    Guid Id,
    Guid GameStateId,
    string Token,
    string Role,
    int MaxUses,
    DateTimeOffset ExpiresAt);

public sealed record InvitePreviewResponse(
    Guid Id,
    Guid GameStateId,
    string GameName,
    string Role,
    int UsesRemaining,
    DateTimeOffset ExpiresAt);

public sealed record InviteAcceptedResponse(
    Guid GameStateId,
    Guid PartyMemberId,
    string Role,
    string Message);
