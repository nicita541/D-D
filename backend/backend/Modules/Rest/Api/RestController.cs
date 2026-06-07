using backend.Infrastructure.Auth;
using backend.Modules.Realtime;
using backend.Modules.Rest.Application;
using backend.Modules.Rest.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Rest.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/rest")]
public sealed class RestController : ControllerBase
{
    private readonly IRestService _rest;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameAccessService _access;
    private readonly IGameRealtimeNotifier _realtime;

    public RestController(
        IRestService rest,
        ICurrentUserService currentUser,
        IGameAccessService access,
        IGameRealtimeNotifier realtime)
    {
        _rest = rest;
        _currentUser = currentUser;
        _access = access;
        _realtime = realtime;
    }

    [HttpPost("short")]
    public async Task<ActionResult> ShortRest(Guid gameStateId, [FromBody] RestRequest? request, CancellationToken cancellationToken)
    {
        var payload = request ?? new RestRequest();
        var access = await RequirePlayForCharacterAsync(gameStateId, payload.ResolvedCharacterId, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _rest.ShortRestAsync(access.Value!.OwnerAccountId, gameStateId, payload, cancellationToken);
        await NotifyIfOk(result, gameStateId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("long")]
    public async Task<ActionResult> LongRest(Guid gameStateId, [FromBody] RestRequest? request, CancellationToken cancellationToken)
    {
        var payload = request ?? new RestRequest();
        var access = await RequirePlayForCharacterAsync(gameStateId, payload.ResolvedCharacterId, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _rest.LongRestAsync(access.Value!.OwnerAccountId, gameStateId, payload, cancellationToken);
        await NotifyIfOk(result, gameStateId, cancellationToken);
        return ToActionResult(result);
    }

    private async Task<(GameAccess? Value, ActionResult? Error)> RequirePlayForCharacterAsync(Guid gameStateId, Guid? characterId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = await _access.GetAccessAsync(current.AccountId, gameStateId, cancellationToken);
        if (access is null)
        {
            return (null, NotFound(new MessageResponse { Message = "GameState не найден" }));
        }

        if (!access.CanPlayGame || !access.CanControlCharacter(characterId))
        {
            return (access, StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для отдыха этим персонажем." }));
        }

        return (access, null);
    }

    private async Task NotifyIfOk<T>(RpgResult<T> result, Guid gameStateId, CancellationToken cancellationToken)
    {
        if (result.Status == RpgResultStatus.Ok)
        {
            await _realtime.NotifyAsync(gameStateId, GameRealtimeEvents.RestCompleted, "rest completed", cancellationToken);
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
