using Microsoft.AspNetCore.SignalR;

namespace backend.Modules.Realtime;

public interface IGameRealtimeNotifier
{
    Task NotifyAsync(Guid gameStateId, string eventName, string reason, CancellationToken cancellationToken);
}

public sealed class GameRealtimeNotifier : IGameRealtimeNotifier
{
    private readonly IHubContext<GameHub> _hub;

    public GameRealtimeNotifier(IHubContext<GameHub> hub)
    {
        _hub = hub;
    }

    public Task NotifyAsync(Guid gameStateId, string eventName, string reason, CancellationToken cancellationToken)
        => _hub.Clients
            .Group(GameRealtimeGroups.Game(gameStateId))
            .SendAsync(eventName, new GameRealtimeEvent(gameStateId, reason, DateTimeOffset.UtcNow), cancellationToken);
}

public sealed record GameRealtimeEvent(
    Guid GameStateId,
    string Reason,
    DateTimeOffset OccurredAt);

public static class GameRealtimeEvents
{
    public const string GameUpdated = "GameUpdated";
    public const string TurnAdded = "TurnAdded";
    public const string CombatUpdated = "CombatUpdated";
    public const string PartyUpdated = "PartyUpdated";
    public const string InventoryUpdated = "InventoryUpdated";
    public const string TravelUpdated = "TravelUpdated";
    public const string RestCompleted = "RestCompleted";
    public const string InviteAccepted = "InviteAccepted";
    public const string Error = "Error";
}

public static class GameRealtimeGroups
{
    public static string Game(Guid gameStateId) => $"game:{gameStateId:N}";
}
