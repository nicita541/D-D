using backend.Infrastructure.Auth;
using backend.Modules.Economy.Application;
using backend.Modules.Economy.Contracts;
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

    public QuestRewardsController(IEconomyService economy, ICurrentUserService currentUser)
    {
        _economy = economy;
        _currentUser = currentUser;
    }

    [HttpPost("complete")]
    public async Task<ActionResult> CompleteQuest(Guid gameStateId, Guid questId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _economy.CompleteQuestAsync(current.AccountId, gameStateId, questId, cancellationToken));
    }

    [HttpPost("rewards/grant")]
    public async Task<ActionResult> GrantReward(Guid gameStateId, Guid questId, [FromBody] GrantQuestRewardRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _economy.GrantQuestRewardAsync(current.AccountId, gameStateId, questId, request ?? new GrantQuestRewardRequest(), cancellationToken));
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
