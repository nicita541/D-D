namespace backend.Infrastructure.Auth;

public sealed record GameAccess(
    Guid RequestAccountId,
    Guid GameStateId,
    Guid OwnerAccountId,
    Guid? PartyMemberId,
    Guid? CharacterId,
    string Role)
{
    public bool IsOwner => RequestAccountId == OwnerAccountId;

    public bool IsHost => IsOwner || string.Equals(Role, GameAccessRoles.Host, StringComparison.OrdinalIgnoreCase);

    public bool IsGameMaster => string.Equals(Role, GameAccessRoles.GameMaster, StringComparison.OrdinalIgnoreCase);

    public bool IsPlayer => string.Equals(Role, GameAccessRoles.Player, StringComparison.OrdinalIgnoreCase);

    public bool IsObserver => string.Equals(Role, GameAccessRoles.Observer, StringComparison.OrdinalIgnoreCase);

    public bool HasActiveMembership => PartyMemberId.HasValue;

    public bool CanReadGame => IsOwner || HasActiveMembership;

    public bool CanPlayGame => IsHost || IsGameMaster || IsPlayer;

    public bool CanManageGame => IsHost;

    public bool CanViewSecrets => IsHost || IsGameMaster;

    public bool CanControlCharacter(Guid? characterId)
    {
        if (!characterId.HasValue)
        {
            return CanPlayGame;
        }

        return CanManageGame || (CanPlayGame && CharacterId == characterId.Value);
    }
}

public static class GameAccessRoles
{
    public const string None = "none";
    public const string Host = "host";
    public const string Player = "player";
    public const string Observer = "observer";
    public const string GameMaster = "gm";
}
