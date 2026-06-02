using backend.Modules.Characters.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Infrastructure.Auth;
using backend.Modules.Ai.Application;
using backend.Modules.Campaigns.Application;
using backend.Modules.Changes.Application;
using backend.Modules.Characters.Application;
using backend.Modules.Combat.Application;
using backend.Modules.GameStates.Application;
using backend.Modules.Mechanics.Application;
using backend.Modules.Memory.Application;
using backend.Modules.Party.Application;
using backend.Modules.Play.Application;
using backend.Modules.Story.Application;
using backend.Modules.Travel.Application;
using backend.Modules.Turns.Application;
using backend.Modules.World.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Characters.Api;

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
