using backend.Infrastructure.Auth;
using backend.Modules.Economy.Application;
using backend.Modules.Economy.Contracts;
using backend.Modules.Realtime;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Economy.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/loot")]
public sealed class LootController : ControllerBase
{
    private readonly IEconomyService _economy;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameAccessService _access;
    private readonly IGameRealtimeNotifier _realtime;

    public LootController(
        IEconomyService economy,
        ICurrentUserService currentUser,
        IGameAccessService access,
        IGameRealtimeNotifier realtime)
    {
        _economy = economy;
        _currentUser = currentUser;
        _access = access;
        _realtime = realtime;
    }

    [HttpGet]
    public async Task<ActionResult> GetLoot(Guid gameStateId, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanReadGame, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        return ToActionResult(await _economy.GetLootAsync(access.Value!.OwnerAccountId, gameStateId, cancellationToken));
    }

    [HttpGet("{lootContainerId:guid}")]
    public async Task<ActionResult> GetLootContainer(Guid gameStateId, Guid lootContainerId, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanReadGame, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        return ToActionResult(await _economy.GetLootContainerAsync(access.Value!.OwnerAccountId, gameStateId, lootContainerId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult> CreateLoot(Guid gameStateId, [FromBody] CreateLootContainerRequest? request, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanManageGame, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _economy.CreateLootAsync(access.Value!.OwnerAccountId, gameStateId, request ?? new CreateLootContainerRequest(), cancellationToken);
        await NotifyIfOk(result, gameStateId, GameRealtimeEvents.InventoryUpdated, "loot created", cancellationToken);
        return result.Status == RpgResultStatus.Ok
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{lootContainerId:guid}/claim")]
    public async Task<ActionResult> ClaimLoot(Guid gameStateId, Guid lootContainerId, [FromBody] ClaimLootRequest? request, CancellationToken cancellationToken)
    {
        var payload = request ?? new ClaimLootRequest();
        var access = await RequireAccessAsync(gameStateId, value => value.CanPlayGame && value.CanControlCharacter(payload.ResolvedCharacterId), cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _economy.ClaimLootAsync(access.Value!.OwnerAccountId, gameStateId, lootContainerId, payload, cancellationToken);
        await NotifyIfOk(result, gameStateId, GameRealtimeEvents.InventoryUpdated, "loot claimed", cancellationToken);
        return ToActionResult(result);
    }

    private async Task<(GameAccess? Value, ActionResult? Error)> RequireAccessAsync(Guid gameStateId, Func<GameAccess, bool> predicate, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = await _access.GetAccessAsync(current.AccountId, gameStateId, cancellationToken);
        if (access is null)
        {
            return (null, NotFound(new MessageResponse { Message = "GameState не найден" }));
        }

        if (!predicate(access))
        {
            return (access, StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для добычи." }));
        }

        return (access, null);
    }

    private async Task NotifyIfOk<T>(RpgResult<T> result, Guid gameStateId, string eventName, string reason, CancellationToken cancellationToken)
    {
        if (result.Status == RpgResultStatus.Ok)
        {
            await _realtime.NotifyAsync(gameStateId, eventName, reason, cancellationToken);
        }
    }

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Некорректный запрос." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Не найдено." }),
            RpgResultStatus.Conflict => Conflict(new MessageResponse { Message = result.Message ?? "Конфликт состояния." }),
            RpgResultStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = result.Message ?? "Недостаточно прав." }),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse { Message = result.Message ?? "Сервис временно недоступен." })
        };
}
