using backend.Infrastructure.Auth;
using backend.Modules.Combat.Application;
using backend.Modules.Combat.Contracts;
using backend.Modules.Combat.Domain;
using backend.Modules.Realtime;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Combat.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/combat")]
public sealed class CombatController : ControllerBase
{
    private readonly ICombatService _combat;
    private readonly ICombatOutcomeService _outcome;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameAccessService _access;
    private readonly IGameRealtimeNotifier _realtime;

    public CombatController(
        ICombatService combat,
        ICombatOutcomeService outcome,
        ICurrentUserService currentUser,
        IGameAccessService access,
        IGameRealtimeNotifier realtime)
    {
        _combat = combat;
        _outcome = outcome;
        _currentUser = currentUser;
        _access = access;
        _realtime = realtime;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetCombat(Guid gameStateId, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanReadGame, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var combat = await _combat.GetCombatStateAsync(access.Value!.OwnerAccountId, gameStateId, cancellationToken);
        return combat.HasValue ? Ok(combat.Value) : NotFound(new MessageResponse { Message = "Бой не найден" });
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> StartCombat(Guid gameStateId, [FromBody] StartCombatRequest? request, CancellationToken cancellationToken)
    {
        var payload = request ?? new StartCombatRequest();
        var access = await RequireAccessAsync(gameStateId, value => value.CanPlayGame && CanStartCombat(value, payload), cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        try
        {
            var id = await _combat.StartCombatAsync(access.Value!.OwnerAccountId, gameStateId, payload, cancellationToken);
            if (!id.HasValue)
            {
                return NotFound(new MessageResponse { Message = "GameState не найден" });
            }

            await NotifyCombatUpdated(gameStateId, "combat started", cancellationToken);
            return CreatedAtAction(nameof(GetCombat), new { gameStateId }, new OperationResponse { Id = id.Value, Message = "Бой начат" });
        }
        catch (CombatValidationException ex)
        {
            return BadRequest(new MessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("end")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> EndCombat(Guid gameStateId, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanPlayGame, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var ended = await _combat.EndCombatAsync(access.Value!.OwnerAccountId, gameStateId, cancellationToken);
        if (!ended)
        {
            return NotFound(new MessageResponse { Message = "Активный бой не найден" });
        }

        await NotifyCombatUpdated(gameStateId, "combat ended", cancellationToken);
        return Ok(new OperationResponse { Id = gameStateId, Message = "Бой завершён" });
    }

    [HttpPost("participants")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> AddParticipant(Guid gameStateId, [FromBody] AddCombatParticipantRequest? request, CancellationToken cancellationToken)
    {
        var payload = request ?? new AddCombatParticipantRequest();
        var access = await RequireAccessAsync(gameStateId, value => value.CanPlayGame && CanUseParticipantActor(value, payload), cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        try
        {
            var id = await _combat.AddParticipantAsync(access.Value!.OwnerAccountId, gameStateId, payload, cancellationToken);
            if (!id.HasValue)
            {
                return NotFound(new MessageResponse { Message = "GameState не найден" });
            }

            await NotifyCombatUpdated(gameStateId, "combat participant added", cancellationToken);
            return CreatedAtAction(nameof(GetCombat), new { gameStateId }, new OperationResponse { Id = id.Value, Message = "Участник боя добавлен" });
        }
        catch (CombatValidationException ex)
        {
            return BadRequest(new MessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("next-turn")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> NextTurn(Guid gameStateId, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanPlayGame, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        try
        {
            var combat = await _combat.NextTurnAsync(access.Value!.OwnerAccountId, gameStateId, cancellationToken);
            if (!combat.HasValue)
            {
                return NotFound(new MessageResponse { Message = "Активный бой не найден" });
            }

            await NotifyCombatUpdated(gameStateId, "combat next turn", cancellationToken);
            return Ok(combat.Value);
        }
        catch (CombatValidationException ex)
        {
            return BadRequest(new MessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("apply-damage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ApplyDamage(Guid gameStateId, [FromBody] ApplyCombatDamageRequest? request, CancellationToken cancellationToken)
    {
        var payload = request ?? new ApplyCombatDamageRequest();
        var access = await RequireAccessAsync(gameStateId, static value => value.CanPlayGame, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        if (payload.ResolvedTargetParticipantId.HasValue
            && !await CanControlParticipantAsync(access.Value!, gameStateId, payload.ResolvedTargetParticipantId.Value, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для участника боя." });
        }

        try
        {
            var combat = await _combat.ApplyDamageAsync(access.Value!.OwnerAccountId, gameStateId, payload, cancellationToken);
            if (!combat.HasValue)
            {
                return NotFound(new MessageResponse { Message = "Участник боя не найден" });
            }

            await NotifyCombatUpdated(gameStateId, "combat damage applied", cancellationToken);
            return Ok(combat.Value);
        }
        catch (CombatValidationException ex)
        {
            return BadRequest(new MessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("participants/{participantId:guid}/heal")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> HealParticipant(Guid gameStateId, Guid participantId, [FromBody] HealCombatParticipantRequest? request, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanPlayGame, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        if (!await CanControlParticipantAsync(access.Value!, gameStateId, participantId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для участника боя." });
        }

        try
        {
            var combat = await _combat.HealParticipantAsync(access.Value!.OwnerAccountId, gameStateId, participantId, request ?? new HealCombatParticipantRequest(), cancellationToken);
            if (!combat.HasValue)
            {
                return NotFound(new MessageResponse { Message = "Участник боя не найден" });
            }

            await NotifyCombatUpdated(gameStateId, "combat participant healed", cancellationToken);
            return Ok(combat.Value);
        }
        catch (CombatValidationException ex)
        {
            return BadRequest(new MessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("attack")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Attack(Guid gameStateId, [FromBody] CombatAttackRequest? request, CancellationToken cancellationToken)
    {
        var payload = request ?? new CombatAttackRequest();
        var access = await RequireAccessAsync(gameStateId, static value => value.CanPlayGame, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        if (payload.ResolvedAttackerParticipantId.HasValue
            && !await CanControlParticipantAsync(access.Value!, gameStateId, payload.ResolvedAttackerParticipantId.Value, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для атакующего участника." });
        }

        try
        {
            var result = await _combat.AttackAsync(access.Value!.OwnerAccountId, gameStateId, payload, cancellationToken);
            if (!result.HasValue)
            {
                return NotFound(new MessageResponse { Message = "Участники боя не найдены" });
            }

            await NotifyCombatUpdated(gameStateId, "combat attack", cancellationToken);
            return Ok(result.Value);
        }
        catch (CombatValidationException ex)
        {
            return BadRequest(new MessageResponse { Message = ex.Message });
        }
    }

    [HttpGet("outcome")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetOutcome(Guid gameStateId, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanReadGame, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _outcome.GetOutcomeAsync(access.Value!.OwnerAccountId, gameStateId, cancellationToken);
        return result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Активный бой не найден." }),
            RpgResultStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = result.Message ?? "Недостаточно прав." }),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse { Message = result.Message ?? "Сервис временно недоступен." })
        };
    }

    private async Task<(GameAccess? Value, ActionResult? Error)> RequireAccessAsync(Guid gameStateId, Func<GameAccess, bool> predicate, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = await _access.GetAccessAsync(current.AccountId, gameStateId, cancellationToken);
        if (access is null)
        {
            return (null, NotFound(new MessageResponse { Message = "GameState не найден" }));
        }

        if (!predicate(access))
        {
            return (access, StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для боя." }));
        }

        return (access, null);
    }

    private static bool CanStartCombat(GameAccess access, StartCombatRequest request)
        => access.CanManageGame
           || (request.Participants.Count > 0 && request.Participants.All(participant => CanUseParticipantActor(access, participant)));

    private static bool CanUseParticipantActor(GameAccess access, AddCombatParticipantRequest request)
        => access.CanManageGame
           || (string.Equals(request.ResolvedActorType, "character", StringComparison.OrdinalIgnoreCase)
               && access.CanControlCharacter(request.ResolvedActorId));

    private async Task<bool> CanControlParticipantAsync(GameAccess access, Guid gameStateId, Guid participantId, CancellationToken cancellationToken)
    {
        if (access.CanManageGame)
        {
            return true;
        }

        var combat = await _combat.GetCombatStateAsync(access.OwnerAccountId, gameStateId, cancellationToken);
        if (!combat.HasValue || !combat.Value.TryGetProperty("участники", out var participants) || participants.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            return false;
        }

        foreach (var participant in participants.EnumerateArray())
        {
            if (!participant.TryGetProperty("id", out var idProperty)
                || idProperty.GetGuid() != participantId
                || !participant.TryGetProperty("типАктера", out var actorTypeProperty)
                || !string.Equals(actorTypeProperty.GetString(), "character", StringComparison.OrdinalIgnoreCase)
                || !participant.TryGetProperty("actorId", out var actorIdProperty))
            {
                continue;
            }

            return access.CanControlCharacter(actorIdProperty.GetGuid());
        }

        return false;
    }

    private Task NotifyCombatUpdated(Guid gameStateId, string reason, CancellationToken cancellationToken)
        => _realtime.NotifyAsync(gameStateId, GameRealtimeEvents.CombatUpdated, reason, cancellationToken);
}
