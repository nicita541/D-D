using backend.Contracts.Rpg.Characters;
using backend.Contracts.Rpg.Common;
using backend.Infrastructure.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/characters/{characterId:guid}")]
public sealed class CharacterProgressionController : ControllerBase
{
    private readonly ICharacterProgressionService _progression;
    private readonly ICurrentUserService _currentUser;

    public CharacterProgressionController(ICharacterProgressionService progression, ICurrentUserService currentUser)
    {
        _progression = progression;
        _currentUser = currentUser;
    }

    [HttpPost("experience")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AddExperience(Guid gameStateId, Guid characterId, [FromBody] AddExperienceRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _progression.AddExperienceAsync(current.AccountId, gameStateId, characterId, request ?? new AddExperienceRequest(), cancellationToken));
    }

    [HttpPost("level-up")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> LevelUp(Guid gameStateId, Guid characterId, [FromBody] LevelUpRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _progression.LevelUpAsync(current.AccountId, gameStateId, characterId, request ?? new LevelUpRequest(), cancellationToken));
    }

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Некорректный запрос." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Не найдено." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
}
