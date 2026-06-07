using backend.Infrastructure.Auth;
using backend.Modules.Mechanics.Application;
using backend.Modules.Mechanics.Contracts;
using backend.Modules.Realtime;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Mechanics.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/checks")]
public sealed class ChecksController : ControllerBase
{
    private readonly IAbilityCheckService _checks;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameAccessService _access;
    private readonly IGameRealtimeNotifier _realtime;

    public ChecksController(
        IAbilityCheckService checks,
        ICurrentUserService currentUser,
        IGameAccessService access,
        IGameRealtimeNotifier realtime)
    {
        _checks = checks;
        _currentUser = currentUser;
        _access = access;
        _realtime = realtime;
    }

    [HttpPost("ability")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CreateAbilityCheck(Guid gameStateId, [FromBody] AbilityCheckRequest? request, CancellationToken cancellationToken)
    {
        var payload = request ?? new AbilityCheckRequest();
        var access = await RequireAccessAsync(gameStateId, value => value.CanPlayGame && value.CanControlCharacter(payload.ResolvedCharacterId), cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _checks.CreateAbilityCheckAsync(access.Value!.OwnerAccountId, gameStateId, payload, cancellationToken);
        await NotifyIfOk(result, gameStateId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetChecks(Guid gameStateId, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanReadGame, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        return ToActionResult(await _checks.GetChecksAsync(access.Value!.OwnerAccountId, gameStateId, limit, cancellationToken));
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
            return (access, StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для проверок." }));
        }

        return (access, null);
    }

    private async Task NotifyIfOk<T>(RpgResult<T> result, Guid gameStateId, CancellationToken cancellationToken)
    {
        if (result.Status == RpgResultStatus.Ok)
        {
            await _realtime.NotifyAsync(gameStateId, GameRealtimeEvents.GameUpdated, "ability check", cancellationToken);
        }
    }

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Некорректный запрос." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Не найдено." }),
            RpgResultStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = result.Message ?? "Недостаточно прав." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
}
