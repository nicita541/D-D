using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Party.Contracts;
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

namespace backend.Modules.Party.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/party")]
public sealed class PartyController : ControllerBase
{
    private readonly IPartyService _party;
    private readonly ICurrentUserService _currentUser;

    public PartyController(IPartyService party, ICurrentUserService currentUser)
    {
        _party = party;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetParty(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var party = await _party.GetPartyAsync(current.AccountId, gameStateId, cancellationToken);
        return party.HasValue ? Ok(party.Value) : NotFound(new MessageResponse { Message = "Партия не найдена" });
    }

    [HttpPost]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> CreateParty(Guid gameStateId, [FromBody] CreatePartyRequest request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var id = await _party.CreatePartyAsync(current.AccountId, gameStateId, request ?? new CreatePartyRequest(), cancellationToken);
        return id.HasValue
            ? CreatedAtAction(nameof(GetParty), new { gameStateId }, new OperationResponse { Id = id.Value, Message = "Партия создана" })
            : NotFound(new MessageResponse { Message = "GameState не найден" });
    }

    [HttpPost("members")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> AddMember(Guid gameStateId, [FromBody] AddPartyMemberRequest request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var id = await _party.AddPartyMemberAsync(current.AccountId, gameStateId, request ?? new AddPartyMemberRequest(), cancellationToken);
        return id.HasValue
            ? CreatedAtAction(nameof(GetParty), new { gameStateId }, new OperationResponse { Id = id.Value, Message = "Участник добавлен в партию" })
            : NotFound(new MessageResponse { Message = "GameState или персонаж не найден" });
    }

    [HttpDelete("members/{memberId:guid}")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> RemoveMember(Guid gameStateId, Guid memberId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var deleted = await _party.RemovePartyMemberAsync(current.AccountId, gameStateId, memberId, cancellationToken);
        return deleted
            ? Ok(new OperationResponse { Id = memberId, Message = "Участник удалён из партии" })
            : NotFound(new MessageResponse { Message = "Участник партии не найден" });
    }
}
