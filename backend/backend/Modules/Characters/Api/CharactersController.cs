using backend.Infrastructure.Auth;
using backend.Modules.Characters.Application;
using backend.Modules.Characters.Contracts;
using backend.Modules.Party.Application;
using backend.Modules.Realtime;
using backend.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Characters.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/characters")]
public sealed class CharactersController : ControllerBase
{
    private readonly ICharacterService _characters;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameAccessService? _access;
    private readonly IPartyService? _party;
    private readonly IGameRealtimeNotifier? _realtime;

    public CharactersController(
        ICharacterService characters,
        ICurrentUserService currentUser,
        IGameAccessService? access = null,
        IPartyService? party = null,
        IGameRealtimeNotifier? realtime = null)
    {
        _characters = characters;
        _currentUser = currentUser;
        _access = access;
        _party = party;
        _realtime = realtime;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetCharacters(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = await GetAccessOrNullAsync(current.AccountId, gameStateId, cancellationToken);
        if (_access is not null && (access is null || !access.CanReadGame))
        {
            return AccessDenied(access);
        }

        return Ok(await _characters.GetCharactersAsync(access?.OwnerAccountId ?? current.AccountId, gameStateId, cancellationToken));
    }

    [HttpGet("{characterId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetCharacter(Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = await GetAccessOrNullAsync(current.AccountId, gameStateId, cancellationToken);
        if (_access is not null && (access is null || !access.CanReadGame))
        {
            return AccessDenied(access);
        }

        var character = await _characters.GetCharacterAsync(access?.OwnerAccountId ?? current.AccountId, gameStateId, characterId, cancellationToken);
        return character.HasValue
            ? Ok(character.Value)
            : NotFound(new MessageResponse { Message = "Персонаж не найден" });
    }

    [HttpPost]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OperationResponse>> CreateCharacter(
        Guid gameStateId,
        [FromBody] CreateCharacterRequest? request,
        CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = await GetAccessOrNullAsync(current.AccountId, gameStateId, cancellationToken);
        if (_access is not null)
        {
            if (access is null || !access.CanPlayGame)
            {
                return AccessDenied(access);
            }

            if (!access.CanManageGame && access.CharacterId.HasValue)
            {
                return Conflict(new MessageResponse { Message = "К вашему участнику партии уже привязан персонаж." });
            }
        }

        var ownerAccountId = access?.OwnerAccountId ?? current.AccountId;
        var id = await _characters.CreateCharacterAsync(ownerAccountId, gameStateId, request ?? new CreateCharacterRequest(), cancellationToken);
        if (!id.HasValue)
        {
            return NotFound(new MessageResponse { Message = "GameState не найден" });
        }

        if (access is not null && !access.CanManageGame && _party is not null)
        {
            await _party.AssignCharacterToAccountAsync(ownerAccountId, gameStateId, current.AccountId, id.Value, cancellationToken);
        }

        await NotifyAsync(gameStateId, GameRealtimeEvents.PartyUpdated, "character created", cancellationToken);
        return CreatedAtAction(
            nameof(GetCharacter),
            new { gameStateId, characterId = id.Value },
            new OperationResponse { Id = id.Value, Message = "Персонаж создан" });
    }

    [HttpPut("{characterId:guid}")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> UpdateCharacter(
        Guid gameStateId,
        Guid characterId,
        [FromBody] UpdateCharacterRequest? request,
        CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = await GetAccessOrNullAsync(current.AccountId, gameStateId, cancellationToken);
        if (_access is not null && (access is null || !access.CanControlCharacter(characterId)))
        {
            return AccessDenied(access);
        }

        var updated = await _characters.UpdateCharacterAsync(
            access?.OwnerAccountId ?? current.AccountId,
            gameStateId,
            characterId,
            request ?? new UpdateCharacterRequest(),
            cancellationToken);

        if (!updated)
        {
            return NotFound(new MessageResponse { Message = "Персонаж не найден" });
        }

        await NotifyAsync(gameStateId, GameRealtimeEvents.GameUpdated, "character updated", cancellationToken);
        return Ok(new OperationResponse { Id = characterId, Message = "Персонаж обновлён" });
    }

    [HttpDelete("{characterId:guid}")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> DeleteCharacter(Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = await GetAccessOrNullAsync(current.AccountId, gameStateId, cancellationToken);
        if (_access is not null && (access is null || !access.CanManageGame))
        {
            return AccessDenied(access);
        }

        var deleted = await _characters.DeleteCharacterAsync(access?.OwnerAccountId ?? current.AccountId, gameStateId, characterId, cancellationToken);
        if (!deleted)
        {
            return NotFound(new MessageResponse { Message = "Персонаж не найден" });
        }

        await NotifyAsync(gameStateId, GameRealtimeEvents.PartyUpdated, "character deleted", cancellationToken);
        return Ok(new OperationResponse { Id = characterId, Message = "Персонаж удалён" });
    }

    private Task<GameAccess?> GetAccessOrNullAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => _access is null
            ? Task.FromResult<GameAccess?>(null)
            : _access.GetAccessAsync(accountId, gameStateId, cancellationToken);

    private ActionResult AccessDenied(GameAccess? access)
        => access is null
            ? NotFound(new MessageResponse { Message = "GameState не найден" })
            : StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для персонажей." });

    private Task NotifyAsync(Guid gameStateId, string eventName, string reason, CancellationToken cancellationToken)
        => _realtime?.NotifyAsync(gameStateId, eventName, reason, cancellationToken) ?? Task.CompletedTask;
}
