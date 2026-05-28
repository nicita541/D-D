using backend.Contracts.Rpg.Combat;
using backend.Contracts.Rpg.Common;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[ApiController]
[Route("api/game-states/{gameStateId:guid}/combat")]
public sealed class CombatController : ControllerBase
{
    private readonly ICombatService _combat;

    public CombatController(ICombatService combat)
    {
        _combat = combat;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetCombat(Guid gameStateId, CancellationToken cancellationToken)
    {
        var combat = await _combat.GetCombatStateAsync(gameStateId, cancellationToken);
        return combat.HasValue ? Ok(combat.Value) : NotFound(new MessageResponse { Message = "Бой не найден" });
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<OperationResponse>> StartCombat(Guid gameStateId, [FromBody] StartCombatRequest request, CancellationToken cancellationToken)
    {
        var id = await _combat.StartCombatAsync(gameStateId, request ?? new StartCombatRequest(), cancellationToken);
        return CreatedAtAction(nameof(GetCombat), new { gameStateId }, new OperationResponse { Id = id, Message = "Бой начат" });
    }

    [HttpPost("end")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> EndCombat(Guid gameStateId, CancellationToken cancellationToken)
    {
        var ended = await _combat.EndCombatAsync(gameStateId, cancellationToken);
        return ended
            ? Ok(new OperationResponse { Id = gameStateId, Message = "Бой завершён" })
            : NotFound(new MessageResponse { Message = "Активный бой не найден" });
    }

    [HttpPost("participants")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<OperationResponse>> AddParticipant(Guid gameStateId, [FromBody] AddCombatParticipantRequest request, CancellationToken cancellationToken)
    {
        var id = await _combat.AddParticipantAsync(gameStateId, request, cancellationToken);
        return CreatedAtAction(nameof(GetCombat), new { gameStateId }, new OperationResponse { Id = id, Message = "Участник боя добавлен" });
    }
}
