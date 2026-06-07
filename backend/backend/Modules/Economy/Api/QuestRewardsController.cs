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
[Route("api/game-states/{gameStateId:guid}/quests/{questId:guid}")]
public sealed class QuestRewardsController : ControllerBase
{
    private readonly IEconomyService _economy;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameAccessService _access;
    private readonly IGameRealtimeNotifier _realtime;

    public QuestRewardsController(
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

    [HttpPost("complete")]
    public async Task<ActionResult> CompleteQuest(Guid gameStateId, Guid questId, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanManageGame, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _economy.CompleteQuestAsync(access.Value!.OwnerAccountId, gameStateId, questId, cancellationToken);
        await NotifyIfOk(result, gameStateId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("rewards/grant")]
    public async Task<ActionResult> GrantReward(Guid gameStateId, Guid questId, [FromBody] GrantQuestRewardRequest? request, CancellationToken cancellationToken)
    {
        var payload = request ?? new GrantQuestRewardRequest();
        var access = await RequireAccessAsync(gameStateId, value => value.CanPlayGame && value.CanControlCharacter(payload.ResolvedCharacterId), cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _economy.GrantQuestRewardAsync(access.Value!.OwnerAccountId, gameStateId, questId, payload, cancellationToken);
        await NotifyIfOk(result, gameStateId, cancellationToken);
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
            return (access, StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для наград." }));
        }

        return (access, null);
    }

    private async Task NotifyIfOk<T>(RpgResult<T> result, Guid gameStateId, CancellationToken cancellationToken)
    {
        if (result.Status == RpgResultStatus.Ok)
        {
            await _realtime.NotifyAsync(gameStateId, GameRealtimeEvents.InventoryUpdated, "quest reward changed", cancellationToken);
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
