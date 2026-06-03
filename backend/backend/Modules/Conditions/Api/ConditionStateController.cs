using backend.Infrastructure.Auth;
using backend.Modules.Conditions.Application;
using backend.Modules.Conditions.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Conditions.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/characters/{characterId:guid}")]
public sealed class ConditionStateController : ControllerBase
{
    private readonly IConditionStateService _conditions;
    private readonly ICurrentUserService _currentUser;

    public ConditionStateController(IConditionStateService conditions, ICurrentUserService currentUser)
    {
        _conditions = conditions;
        _currentUser = currentUser;
    }

    [HttpPost("conditions/tick")]
    public async Task<ActionResult> TickConditions(Guid gameStateId, Guid characterId, [FromBody] TickConditionsRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _conditions.TickConditionsAsync(current.AccountId, gameStateId, characterId, request ?? new TickConditionsRequest(), cancellationToken));
    }

    [HttpPost("knockout")]
    public async Task<ActionResult> Knockout(Guid gameStateId, Guid characterId, [FromBody] KnockoutRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _conditions.KnockoutAsync(current.AccountId, gameStateId, characterId, request ?? new KnockoutRequest(), cancellationToken));
    }

    [HttpPost("revive")]
    public async Task<ActionResult> Revive(Guid gameStateId, Guid characterId, [FromBody] ReviveRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _conditions.ReviveAsync(current.AccountId, gameStateId, characterId, request ?? new ReviveRequest(), cancellationToken));
    }

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Некорректный запрос." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Не найдено." }),
            RpgResultStatus.Conflict => Conflict(new MessageResponse { Message = result.Message ?? "Конфликт состояния." }),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse { Message = result.Message ?? "Сервис временно недоступен." })
        };
}
