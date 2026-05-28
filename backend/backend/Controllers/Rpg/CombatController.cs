using backend.Contracts.Rpg.Combat;
using backend.Contracts.Rpg.Common;
using backend.Infrastructure.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/combat")]
public sealed class CombatController : ControllerBase
{
    private readonly ICombatService _combat;
    private readonly ICurrentUserService _currentUser;

    public CombatController(ICombatService combat, ICurrentUserService currentUser)
    {
        _combat = combat;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetCombat(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var combat = await _combat.GetCombatStateAsync(current.AccountId, gameStateId, cancellationToken);
        return combat.HasValue ? Ok(combat.Value) : NotFound(new MessageResponse { Message = "Бой не найден" });
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> StartCombat(Guid gameStateId, [FromBody] StartCombatRequest request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();

        try
        {
            var id = await _combat.StartCombatAsync(current.AccountId, gameStateId, request ?? new StartCombatRequest(), cancellationToken);
            return id.HasValue
                ? CreatedAtAction(nameof(GetCombat), new { gameStateId }, new OperationResponse { Id = id.Value, Message = "Бой начат" })
                : NotFound(new MessageResponse { Message = "GameState не найден" });
        }
        catch (CombatValidationException ex)
        {
            return BadRequest(new MessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("end")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> EndCombat(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var ended = await _combat.EndCombatAsync(current.AccountId, gameStateId, cancellationToken);
        return ended
            ? Ok(new OperationResponse { Id = gameStateId, Message = "Бой завершён" })
            : NotFound(new MessageResponse { Message = "Активный бой не найден" });
    }

    [HttpPost("participants")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> AddParticipant(Guid gameStateId, [FromBody] AddCombatParticipantRequest request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();

        try
        {
            var id = await _combat.AddParticipantAsync(current.AccountId, gameStateId, request ?? new AddCombatParticipantRequest(), cancellationToken);
            return id.HasValue
                ? CreatedAtAction(nameof(GetCombat), new { gameStateId }, new OperationResponse { Id = id.Value, Message = "Участник боя добавлен" })
                : NotFound(new MessageResponse { Message = "GameState не найден" });
        }
        catch (CombatValidationException ex)
        {
            return BadRequest(new MessageResponse { Message = ex.Message });
        }
    }
}
