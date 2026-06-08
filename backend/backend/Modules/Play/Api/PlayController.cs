using backend.Infrastructure.Auth;
using backend.Modules.AccountCharacters.Application;
using backend.Modules.Changes.Contracts;
using backend.Modules.Combat.Contracts;
using backend.Modules.Mechanics.Contracts;
using backend.Modules.Memory.Contracts;
using backend.Modules.Play.Application;
using backend.Modules.Play.Contracts;
using backend.Modules.Realtime;
using backend.Modules.Snapshots.Application;
using backend.Modules.Turns.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Play.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/play")]
public sealed class PlayController : ControllerBase
{
    private readonly IPlayApplicationService _play;
    private readonly IPlayTravelFacade _travel;
    private readonly IPlayCombatFacade _combat;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameAccessService _access;
    private readonly IGameRealtimeNotifier _realtime;
    private readonly ISnapshotService _snapshots;
    private readonly IAccountCharacterSyncService _characterSync;

    public PlayController(
        IPlayApplicationService play,
        IPlayTravelFacade travel,
        IPlayCombatFacade combat,
        ICurrentUserService currentUser,
        IGameAccessService access,
        IGameRealtimeNotifier realtime,
        ISnapshotService snapshots,
        IAccountCharacterSyncService characterSync)
    {
        _play = play;
        _travel = travel;
        _combat = combat;
        _currentUser = currentUser;
        _access = access;
        _realtime = realtime;
        _snapshots = snapshots;
        _characterSync = characterSync;
    }

    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Status(Guid gameStateId, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanReadGame, "Недостаточно прав для чтения игры.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        await _characterSync.ImportLatestProfileIntoGameAsync(access.Value!.OwnerAccountId, gameStateId, null, cancellationToken);
        var result = await _play.StatusAsync(access.Value.OwnerAccountId, gameStateId, cancellationToken);
        return this.ToActionResult(RedactIfNeeded(result, access.Value));
    }

    [HttpPost("act")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Act(Guid gameStateId, [FromBody] PlayActRequest? request, CancellationToken cancellationToken)
    {
        var payload = request ?? new PlayActRequest();
        var access = await RequirePlayForCharacterAsync(gameStateId, payload.ResolvedCharacterId, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        await _characterSync.ImportLatestProfileIntoGameAsync(access.Value!.OwnerAccountId, gameStateId, payload.ResolvedCharacterId, cancellationToken);
        var result = await _play.ActAsync(access.Value.OwnerAccountId, gameStateId, payload, cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.TurnAdded, "play act", cancellationToken);
        return this.ToActionResult(RedactIfNeeded(result, access.Value));
    }

    [HttpPost("start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Start(Guid gameStateId, [FromBody] CreateTurnRequest? request, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanPlayGame, "Недостаточно прав для хода.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        await _characterSync.ImportLatestProfileIntoGameAsync(access.Value!.OwnerAccountId, gameStateId, null, cancellationToken);
        var result = await _play.StartAsync(access.Value.OwnerAccountId, gameStateId, request, cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.TurnAdded, "play started", cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("message")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Message(Guid gameStateId, [FromBody] CreateTurnRequest? request, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanPlayGame, "Недостаточно прав для сообщения.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        await _characterSync.ImportLatestProfileIntoGameAsync(access.Value!.OwnerAccountId, gameStateId, null, cancellationToken);
        var result = await _play.MessageAsync(access.Value.OwnerAccountId, gameStateId, request, cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.TurnAdded, "play message", cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("resolve-mechanic-request/{requestId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> ResolveMechanicRequest(
        Guid gameStateId,
        Guid requestId,
        [FromBody] MechanicRequestResolveAbilityCheckRequest? request,
        CancellationToken cancellationToken)
    {
        var payload = request ?? new MechanicRequestResolveAbilityCheckRequest();
        var access = await RequirePlayForCharacterAsync(gameStateId, payload.ResolvedCharacterId, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _play.ResolveMechanicRequestAsync(access.Value!.OwnerAccountId, gameStateId, requestId, payload, cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.GameUpdated, "mechanic request resolved", cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("resolve-and-continue/{requestId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> ResolveAndContinue(
        Guid gameStateId,
        Guid requestId,
        [FromBody] PlayResolveAndContinueRequest? request,
        CancellationToken cancellationToken)
    {
        var payload = request ?? new PlayResolveAndContinueRequest();
        var access = await RequirePlayForCharacterAsync(gameStateId, payload.ResolvedCharacterId, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _play.ResolveAndContinueAsync(access.Value!.OwnerAccountId, gameStateId, requestId, payload, cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.TurnAdded, "mechanic request resolved and continued", cancellationToken);
        return this.ToActionResult(RedactIfNeeded(result, access.Value));
    }

    [HttpPost("continue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Continue(Guid gameStateId, [FromBody] PlayContinueRequest? request, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanPlayGame, "Недостаточно прав для продолжения.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var snapshotError = await CreateAutoSnapshotAsync(access.Value!, gameStateId, "auto: before play continue", cancellationToken);
        if (snapshotError is not null)
        {
            return snapshotError;
        }

        var result = await _play.ContinueAsync(access.Value!.OwnerAccountId, gameStateId, request, cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.TurnAdded, "play continued", cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("apply-change/{changeId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ApplyChange(Guid gameStateId, Guid changeId, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanManageGame, "Только host может применять pending changes напрямую.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var snapshotError = await CreateAutoSnapshotAsync(access.Value!, gameStateId, $"auto: before apply change {changeId}", cancellationToken);
        if (snapshotError is not null)
        {
            return snapshotError;
        }

        var result = await _play.ApplyChangeAsync(access.Value!.OwnerAccountId, gameStateId, changeId, cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.GameUpdated, "change applied", cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("reject-change/{changeId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RejectChange(Guid gameStateId, Guid changeId, [FromBody] RejectGameChangeRequest? request, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanManageGame, "Только host может отклонять pending changes напрямую.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _play.RejectChangeAsync(access.Value!.OwnerAccountId, gameStateId, changeId, request, cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.GameUpdated, "change rejected", cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("apply-safe-changes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ApplySafeChanges(Guid gameStateId, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanManageGame, "Только host может применять safe changes вручную.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _play.ApplySafeChangesAsync(access.Value!.OwnerAccountId, gameStateId, cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.GameUpdated, "safe changes applied", cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("travel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Travel(Guid gameStateId, [FromBody] PlayTravelRequest? request, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanPlayGame, "Недостаточно прав для путешествия.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _travel.TravelAsync(access.Value!.OwnerAccountId, gameStateId, request ?? new PlayTravelRequest(), cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.TravelUpdated, "travel", cancellationToken);
        return this.ToActionResult(RedactIfNeeded(result, access.Value));
    }

    [HttpPost("location/move")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MoveLocation(Guid gameStateId, [FromBody] PlayTravelRequest? request, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanPlayGame, "Недостаточно прав для перемещения.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _travel.MoveLocationAsync(access.Value!.OwnerAccountId, gameStateId, request ?? new PlayTravelRequest(), cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.TravelUpdated, "location moved", cancellationToken);
        return this.ToActionResult(RedactIfNeeded(result, access.Value));
    }

    [HttpPost("combat/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> StartPlayCombat(Guid gameStateId, [FromBody] PlayCombatStartRequest? request, CancellationToken cancellationToken)
    {
        var payload = request ?? new PlayCombatStartRequest();
        var access = await RequireAccessAsync(gameStateId, value => value.CanPlayGame && CanUseCombatParticipants(value, payload), "Недостаточно прав для старта боя.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var snapshotError = await CreateAutoSnapshotAsync(access.Value!, gameStateId, "auto: before combat start", cancellationToken);
        if (snapshotError is not null)
        {
            return snapshotError;
        }

        var result = await _combat.StartAsync(access.Value!.OwnerAccountId, gameStateId, payload, cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.CombatUpdated, "combat started", cancellationToken);
        return this.ToActionResult(RedactIfNeeded(result, access.Value));
    }

    [HttpPost("combat/action")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> PlayCombatAction(Guid gameStateId, [FromBody] PlayCombatActionRequest? request, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanPlayGame, "Недостаточно прав для действия в бою.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _combat.ActionAsync(access.Value!.OwnerAccountId, gameStateId, request ?? new PlayCombatActionRequest(), cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.CombatUpdated, "combat action", cancellationToken);
        return this.ToActionResult(RedactIfNeeded(result, access.Value));
    }

    [HttpPost("combat/end")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> EndPlayCombat(Guid gameStateId, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanPlayGame, "Недостаточно прав для завершения боя.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _combat.EndAsync(access.Value!.OwnerAccountId, gameStateId, cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.CombatUpdated, "combat ended", cancellationToken);
        return this.ToActionResult(RedactIfNeeded(result, access.Value));
    }

    [HttpPost("combat/continue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> ContinuePlayCombat(Guid gameStateId, [FromBody] PlayContinueRequest? request, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanPlayGame, "Недостаточно прав для продолжения боя.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _combat.ContinueAsync(access.Value!.OwnerAccountId, gameStateId, request ?? new PlayContinueRequest(), cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.TurnAdded, "combat continued", cancellationToken);
        return this.ToActionResult(RedactIfNeeded(result, access.Value));
    }

    [HttpPost("combat/resolve-outcome")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> ResolveCombatOutcome(Guid gameStateId, [FromBody] PlayCombatResolveOutcomeRequest? request, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanPlayGame, "Недостаточно прав для исхода боя.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _combat.ResolveOutcomeAsync(access.Value!.OwnerAccountId, gameStateId, request ?? new PlayCombatResolveOutcomeRequest(), cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.CombatUpdated, "combat outcome resolved", cancellationToken);
        return this.ToActionResult(RedactIfNeeded(result, access.Value));
    }

    [HttpPost("summarize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Summarize(Guid gameStateId, [FromBody] CampaignMemorySummarizeRequest? request, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanManageGame, "Только host может суммаризировать память.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        return this.ToActionResult(await _play.SummarizeAsync(access.Value!.OwnerAccountId, gameStateId, request, cancellationToken));
    }

    [HttpPost("bootstrap")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Bootstrap(Guid gameStateId, CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, static value => value.CanManageGame, "Только host может выполнить bootstrap.", cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var snapshotError = await CreateAutoSnapshotAsync(access.Value!, gameStateId, "auto: before bootstrap", cancellationToken);
        if (snapshotError is not null)
        {
            return snapshotError;
        }

        var result = await _play.BootstrapAsync(access.Value!.OwnerAccountId, gameStateId, cancellationToken);
        await NotifyIfOk(result, access.Value, gameStateId, GameRealtimeEvents.GameUpdated, "game bootstrapped", cancellationToken);
        return this.ToActionResult(result);
    }

    private async Task<(GameAccess? Value, ActionResult? Error)> RequirePlayForCharacterAsync(
        Guid gameStateId,
        Guid? characterId,
        CancellationToken cancellationToken)
    {
        var access = await RequireAccessAsync(gameStateId, value => value.CanPlayGame && value.CanControlCharacter(characterId), "Недостаточно прав для этого персонажа.", cancellationToken);
        return access;
    }

    private async Task<(GameAccess? Value, ActionResult? Error)> RequireAccessAsync(
        Guid gameStateId,
        Func<GameAccess, bool> predicate,
        string forbiddenMessage,
        CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = await _access.GetAccessAsync(current.AccountId, gameStateId, cancellationToken);
        if (access is null)
        {
            return (null, NotFound(new MessageResponse { Message = "GameState не найден" }));
        }

        if (!predicate(access))
        {
            return (access, StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = forbiddenMessage }));
        }

        return (access, null);
    }

    private static bool CanUseCombatParticipants(GameAccess access, PlayCombatStartRequest request)
    {
        if (access.CanManageGame || request.ResolvedParticipants.Count == 0)
        {
            return true;
        }

        return request.ResolvedParticipants.All(participant =>
            !string.Equals(participant.ResolvedActorType, "character", StringComparison.OrdinalIgnoreCase)
            || access.CanControlCharacter(participant.ResolvedActorId));
    }

    private static RpgResult<PlayStateResponse> RedactIfNeeded(RpgResult<PlayStateResponse> result, GameAccess access)
    {
        if (result.Status != RpgResultStatus.Ok || result.Value is null)
        {
            return result;
        }

        var value = result.Value with
        {
            Permissions = new PlayPermissionsDto(
                access.CanReadGame,
                access.CanPlayGame,
                access.CanManageGame,
                access.CanViewSecrets,
                access.CanControlCharacter(access.CharacterId)),
            CurrentPartyMember = new CurrentPartyMemberDto(
                access.PartyMemberId,
                access.Role,
                access.CharacterId,
                access.IsHost)
        };

        return RpgResult<PlayStateResponse>.Ok(access.CanViewSecrets ? value : SecretRedactor.Redact(value));
    }

    private async Task<ActionResult?> CreateAutoSnapshotAsync(GameAccess access, Guid gameStateId, string reason, CancellationToken cancellationToken)
    {
        var result = await _snapshots.CreateSnapshotAsync(access.OwnerAccountId, gameStateId, access.RequestAccountId, reason, cancellationToken);
        return result.Status == RpgResultStatus.Ok
            ? null
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse { Message = result.Message ?? "Не удалось создать auto-snapshot." });
    }

    private async Task NotifyIfOk<T>(
        RpgResult<T> result,
        GameAccess access,
        Guid gameStateId,
        string eventName,
        string reason,
        CancellationToken cancellationToken)
    {
        if (result.Status == RpgResultStatus.Ok)
        {
            await _characterSync.ExportGameCharactersAsync(access.OwnerAccountId, gameStateId, cancellationToken);
            await _realtime.NotifyAsync(gameStateId, eventName, reason, cancellationToken);
        }
    }
}
