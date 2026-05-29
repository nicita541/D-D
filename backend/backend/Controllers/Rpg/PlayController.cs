using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Turns;
using backend.Infrastructure.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/play")]
public sealed class PlayController : ControllerBase
{
    private const string DefaultStartMessage = """
        Начни новую одиночную RPG-сцену для моего персонажа.

        Используй текущее состояние игры, персонажей, мира, локации, квестов и истории из контекста.
        Представь стартовую сцену, ближайшую цель, атмосферу, важную угрозу или NPC.
        Не создавай финал сразу.
        Дай игроку понятную ситуацию и 2-4 естественных варианта действия, но не ограничивай его только ими.
        Если нужна проверка навыка или характеристики — предложи её через поддерживаемый JSON changes.
        """;

    private readonly IGameStateService _gameStates;
    private readonly ICharacterService _characters;
    private readonly ITurnService _turns;
    private readonly ICurrentUserService _currentUser;

    public PlayController(
        IGameStateService gameStates,
        ICharacterService characters,
        ITurnService turns,
        ICurrentUserService currentUser)
    {
        _gameStates = gameStates;
        _characters = characters;
        _turns = turns;
        _currentUser = currentUser;
    }

    [HttpPost("start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Start(Guid gameStateId, [FromBody] CreateTurnRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();

        var gameState = await _gameStates.GetGameStateAsync(current.AccountId, gameStateId, cancellationToken);
        if (!gameState.HasValue)
        {
            return NotFound(new MessageResponse { Message = "GameState не найден." });
        }

        var characters = await _characters.GetCharactersAsync(current.AccountId, gameStateId, cancellationToken);
        if (characters.Count == 0)
        {
            return BadRequest(new MessageResponse { Message = "Перед стартом сюжета нужно создать персонажа." });
        }

        var startRequest = new CreateTurnRequest
        {
            Message = string.IsNullOrWhiteSpace(request?.ResolvedPlayerMessage)
                ? DefaultStartMessage
                : request.ResolvedPlayerMessage
        };

        var result = await _turns.CreateTurnAsync(current.AccountId, gameStateId, startRequest, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("message")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Message(Guid gameStateId, [FromBody] CreateTurnRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var result = await _turns.CreateTurnAsync(current.AccountId, gameStateId, request ?? new CreateTurnRequest(), cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Bad request." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Not found." }),
            RpgResultStatus.Conflict => Conflict(new MessageResponse { Message = result.Message ?? "Conflict." }),
            RpgResultStatus.ServiceUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, result.Value),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
}