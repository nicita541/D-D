using backend.Contracts.Rpg.Characters;
using backend.Contracts.Rpg.Common;
using backend.Infrastructure.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/characters")]
public sealed class CharactersController : ControllerBase
{
    private readonly ICharacterService _characters;
    private readonly ICurrentUserService _currentUser;

    public CharactersController(ICharacterService characters, ICurrentUserService currentUser)
    {
        _characters = characters;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetCharacters(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return Ok(await _characters.GetCharactersAsync(current.AccountId, gameStateId, cancellationToken));
    }

    [HttpGet("{characterId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetCharacter(Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var character = await _characters.GetCharacterAsync(current.AccountId, gameStateId, characterId, cancellationToken);
        return character.HasValue ? Ok(character.Value) : NotFound(new MessageResponse { Message = "Персонаж не найден" });
    }

    [HttpPost]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> CreateCharacter(
        Guid gameStateId,
        [FromBody] CreateCharacterRequest? request,
        CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var id = await _characters.CreateCharacterAsync(current.AccountId, gameStateId, request ?? new CreateCharacterRequest(), cancellationToken);
        return id.HasValue
            ? CreatedAtAction(nameof(GetCharacter), new { gameStateId, characterId = id.Value }, new OperationResponse { Id = id.Value, Message = "Персонаж создан" })
            : NotFound(new MessageResponse { Message = "GameState не найден" });
    }

    [HttpPut("{characterId:guid}")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> UpdateCharacter(
        Guid gameStateId,
        Guid characterId,
        [FromBody] UpdateCharacterRequest? request,
        CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var updated = await _characters.UpdateCharacterAsync(current.AccountId, gameStateId, characterId, request ?? new UpdateCharacterRequest(), cancellationToken);
        return updated
            ? Ok(new OperationResponse { Id = characterId, Message = "Персонаж обновлён" })
            : NotFound(new MessageResponse { Message = "Персонаж не найден" });
    }

    [HttpDelete("{characterId:guid}")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> DeleteCharacter(Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var deleted = await _characters.DeleteCharacterAsync(current.AccountId, gameStateId, characterId, cancellationToken);
        return deleted
            ? Ok(new OperationResponse { Id = characterId, Message = "Персонаж удалён" })
            : NotFound(new MessageResponse { Message = "Персонаж не найден" });
    }
}
