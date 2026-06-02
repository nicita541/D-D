using backend.Modules.Combat.Application;
using backend.Modules.Combat.Contracts;
using backend.Modules.Combat.Domain;
using backend.Modules.Combat.Infrastructure;
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

namespace backend.Modules.Combat.Api;

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

    [HttpPost("next-turn")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> NextTurn(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();

        try
        {
            var combat = await _combat.NextTurnAsync(current.AccountId, gameStateId, cancellationToken);
            return combat.HasValue ? Ok(combat.Value) : NotFound(new MessageResponse { Message = "Активный бой не найден" });
        }
        catch (CombatValidationException ex)
        {
            return BadRequest(new MessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("apply-damage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ApplyDamage(Guid gameStateId, [FromBody] ApplyCombatDamageRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();

        try
        {
            var combat = await _combat.ApplyDamageAsync(current.AccountId, gameStateId, request ?? new ApplyCombatDamageRequest(), cancellationToken);
            return combat.HasValue ? Ok(combat.Value) : NotFound(new MessageResponse { Message = "Участник боя не найден" });
        }
        catch (CombatValidationException ex)
        {
            return BadRequest(new MessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("participants/{participantId:guid}/heal")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> HealParticipant(Guid gameStateId, Guid participantId, [FromBody] HealCombatParticipantRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();

        try
        {
            var combat = await _combat.HealParticipantAsync(current.AccountId, gameStateId, participantId, request ?? new HealCombatParticipantRequest(), cancellationToken);
            return combat.HasValue ? Ok(combat.Value) : NotFound(new MessageResponse { Message = "Участник боя не найден" });
        }
        catch (CombatValidationException ex)
        {
            return BadRequest(new MessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("attack")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Attack(Guid gameStateId, [FromBody] CombatAttackRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();

        try
        {
            var result = await _combat.AttackAsync(current.AccountId, gameStateId, request ?? new CombatAttackRequest(), cancellationToken);
            return result.HasValue ? Ok(result.Value) : NotFound(new MessageResponse { Message = "Участники боя не найдены" });
        }
        catch (CombatValidationException ex)
        {
            return BadRequest(new MessageResponse { Message = ex.Message });
        }
    }
}
