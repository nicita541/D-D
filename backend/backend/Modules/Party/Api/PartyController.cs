using backend.Infrastructure.Auth;
using backend.Modules.Party.Application;
using backend.Modules.Party.Contracts;
using backend.Modules.Realtime;
using backend.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Party.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/party")]
public sealed class PartyController : ControllerBase
{
    private readonly IPartyService _party;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameAccessService _access;
    private readonly IGameRealtimeNotifier _realtime;

    public PartyController(
        IPartyService party,
        ICurrentUserService currentUser,
        IGameAccessService access,
        IGameRealtimeNotifier realtime)
    {
        _party = party;
        _currentUser = currentUser;
        _access = access;
        _realtime = realtime;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetParty(Guid gameStateId, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(gameStateId, cancellationToken);
        if (access is null || !access.CanReadGame)
        {
            return AccessDenied(access);
        }

        var party = await _party.GetPartyAsync(access.OwnerAccountId, gameStateId, cancellationToken);
        return party.HasValue
            ? Ok(party.Value)
            : NotFound(new MessageResponse { Message = "Партия не найдена" });
    }

    [HttpPost]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> CreateParty(Guid gameStateId, [FromBody] CreatePartyRequest? request, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(gameStateId, cancellationToken);
        if (access is null || !access.CanManageGame)
        {
            return AccessDenied(access);
        }

        var id = await _party.CreatePartyAsync(access.OwnerAccountId, gameStateId, request ?? new CreatePartyRequest(), cancellationToken);
        if (!id.HasValue)
        {
            return NotFound(new MessageResponse { Message = "GameState не найден" });
        }

        await NotifyPartyUpdated(gameStateId, cancellationToken);
        return CreatedAtAction(nameof(GetParty), new { gameStateId }, new OperationResponse { Id = id.Value, Message = "Партия создана" });
    }

    [HttpPost("members")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> AddMember(Guid gameStateId, [FromBody] AddPartyMemberRequest? request, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(gameStateId, cancellationToken);
        if (access is null || !access.CanManageGame)
        {
            return AccessDenied(access);
        }

        var id = await _party.AddPartyMemberAsync(access.OwnerAccountId, gameStateId, request ?? new AddPartyMemberRequest(), cancellationToken);
        if (!id.HasValue)
        {
            return NotFound(new MessageResponse { Message = "GameState или персонаж не найден" });
        }

        await NotifyPartyUpdated(gameStateId, cancellationToken);
        return CreatedAtAction(nameof(GetParty), new { gameStateId }, new OperationResponse { Id = id.Value, Message = "Участник добавлен в партию" });
    }

    [HttpPatch("members/{memberId:guid}")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> UpdateMember(Guid gameStateId, Guid memberId, [FromBody] UpdatePartyMemberRequest? request, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(gameStateId, cancellationToken);
        if (access is null || !access.CanManageGame)
        {
            return AccessDenied(access);
        }

        var updated = await _party.UpdatePartyMemberAsync(access.OwnerAccountId, gameStateId, memberId, request ?? new UpdatePartyMemberRequest(), cancellationToken);
        if (!updated)
        {
            return NotFound(new MessageResponse { Message = "Участник партии не найден" });
        }

        await NotifyPartyUpdated(gameStateId, cancellationToken);
        return Ok(new OperationResponse { Id = memberId, Message = "Участник партии обновлён" });
    }

    [HttpPost("members/{memberId:guid}/character")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> AssignCharacter(Guid gameStateId, Guid memberId, [FromBody] AssignPartyMemberCharacterRequest? request, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(gameStateId, cancellationToken);
        if (access is null || !access.CanManageGame)
        {
            return AccessDenied(access);
        }

        var characterId = request?.ResolvedCharacterId;
        var updated = await _party.AssignPartyMemberCharacterAsync(access.OwnerAccountId, gameStateId, memberId, characterId, cancellationToken);
        if (!updated)
        {
            return NotFound(new MessageResponse { Message = "Участник партии или персонаж не найден" });
        }

        await NotifyPartyUpdated(gameStateId, cancellationToken);
        return Ok(new OperationResponse { Id = memberId, Message = "Персонаж назначен участнику партии" });
    }

    [HttpPost("members/me/character")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OperationResponse>> AssignMyCharacter(Guid gameStateId, [FromBody] AssignPartyMemberCharacterRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = await _access.GetAccessAsync(current.AccountId, gameStateId, cancellationToken);
        if (access is null || !access.CanPlayGame || !access.PartyMemberId.HasValue)
        {
            return AccessDenied(access);
        }

        var characterId = request?.ResolvedCharacterId;
        if (!characterId.HasValue)
        {
            return BadRequest(new MessageResponse { Message = "characterId обязателен." });
        }

        var updated = await _party.AssignCharacterToAccountAsync(access.OwnerAccountId, gameStateId, current.AccountId, characterId.Value, cancellationToken);
        if (!updated)
        {
            return Conflict(new MessageResponse { Message = "Персонаж не найден, не принадлежит игре или уже назначен другому участнику." });
        }

        await NotifyPartyUpdated(gameStateId, cancellationToken);
        return Ok(new OperationResponse { Id = characterId.Value, Message = "Ваш персонаж назначен." });
    }

    [HttpDelete("members/{memberId:guid}")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> RemoveMember(Guid gameStateId, Guid memberId, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(gameStateId, cancellationToken);
        if (access is null || !access.CanManageGame)
        {
            return AccessDenied(access);
        }

        var deleted = await _party.RemovePartyMemberAsync(access.OwnerAccountId, gameStateId, memberId, cancellationToken);
        if (!deleted)
        {
            return NotFound(new MessageResponse { Message = "Участник партии не найден" });
        }

        await NotifyPartyUpdated(gameStateId, cancellationToken);
        return Ok(new OperationResponse { Id = memberId, Message = "Участник удалён из партии" });
    }

    [HttpDelete("members/me")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> LeaveParty(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = await _access.GetAccessAsync(current.AccountId, gameStateId, cancellationToken);
        if (access is null || !access.CanReadGame)
        {
            return AccessDenied(access);
        }

        if (access.CanManageGame)
        {
            return Conflict(new MessageResponse { Message = "Host не может выйти из собственной партии." });
        }

        var left = await _party.LeavePartyAsync(current.AccountId, gameStateId, cancellationToken);
        if (!left)
        {
            return NotFound(new MessageResponse { Message = "Активное участие в партии не найдено" });
        }

        await NotifyPartyUpdated(gameStateId, cancellationToken);
        return Ok(new OperationResponse { Id = access.PartyMemberId ?? gameStateId, Message = "Вы вышли из партии" });
    }

    private async Task<GameAccess?> GetAccessAsync(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return await _access.GetAccessAsync(current.AccountId, gameStateId, cancellationToken);
    }

    private ActionResult AccessDenied(GameAccess? access)
        => access is null
            ? NotFound(new MessageResponse { Message = "GameState не найден" })
            : StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для партии." });

    private Task NotifyPartyUpdated(Guid gameStateId, CancellationToken cancellationToken)
        => _realtime.NotifyAsync(gameStateId, GameRealtimeEvents.PartyUpdated, "party updated", cancellationToken);
}
