using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Parties;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[ApiController]
[Route("api/game-states/{gameStateId:guid}/party")]
public sealed class PartyController : ControllerBase
{
    private readonly IPartyService _party;

    public PartyController(IPartyService party)
    {
        _party = party;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetParty(Guid gameStateId, CancellationToken cancellationToken)
    {
        var party = await _party.GetPartyAsync(gameStateId, cancellationToken);
        return party.HasValue ? Ok(party.Value) : NotFound(new MessageResponse { Message = "Партия не найдена" });
    }

    [HttpPost]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<OperationResponse>> CreateParty(Guid gameStateId, [FromBody] CreatePartyRequest request, CancellationToken cancellationToken)
    {
        var id = await _party.CreatePartyAsync(gameStateId, request ?? new CreatePartyRequest(), cancellationToken);
        return CreatedAtAction(nameof(GetParty), new { gameStateId }, new OperationResponse { Id = id, Message = "Партия создана" });
    }

    [HttpPost("members")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<OperationResponse>> AddMember(Guid gameStateId, [FromBody] AddPartyMemberRequest request, CancellationToken cancellationToken)
    {
        var id = await _party.AddPartyMemberAsync(gameStateId, request ?? new AddPartyMemberRequest(), cancellationToken);
        return CreatedAtAction(nameof(GetParty), new { gameStateId }, new OperationResponse { Id = id, Message = "Участник добавлен в партию" });
    }

    [HttpDelete("members/{memberId:guid}")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> RemoveMember(Guid gameStateId, Guid memberId, CancellationToken cancellationToken)
    {
        var deleted = await _party.RemovePartyMemberAsync(gameStateId, memberId, cancellationToken);
        return deleted
            ? Ok(new OperationResponse { Id = memberId, Message = "Участник удалён из партии" })
            : NotFound(new MessageResponse { Message = "Участник партии не найден" });
    }
}
