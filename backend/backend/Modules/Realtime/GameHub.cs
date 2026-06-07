using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using backend.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace backend.Modules.Realtime;

[Authorize]
public sealed class GameHub : Hub
{
    private readonly IGameAccessService _access;

    public GameHub(IGameAccessService access)
    {
        _access = access;
    }

    public async Task JoinGame(Guid gameStateId)
    {
        var accountId = GetAccountId();
        var access = await _access.GetAccessAsync(accountId, gameStateId, Context.ConnectionAborted);
        if (access is null || !access.CanReadGame)
        {
            throw new HubException("Нет доступа к игре.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GameRealtimeGroups.Game(gameStateId), Context.ConnectionAborted);
        await Clients.Caller.SendAsync(
            GameRealtimeEvents.GameUpdated,
            new GameRealtimeEvent(gameStateId, "Joined", DateTimeOffset.UtcNow),
            Context.ConnectionAborted);
    }

    public Task LeaveGame(Guid gameStateId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GameRealtimeGroups.Game(gameStateId), Context.ConnectionAborted);

    private Guid GetAccountId()
    {
        var accountIdValue =
            Context.User?.FindFirstValue("accountId")
            ?? Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(accountIdValue, out var accountId)
            ? accountId
            : throw new HubException("Некорректный пользователь.");
    }
}
