using System.Text.Json;
using backend.Contracts.Rpg.Changes;
using backend.Contracts.Rpg.Combat;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Mechanics;
using backend.Contracts.Rpg.Memory;
using backend.Contracts.Rpg.Play;
using backend.Contracts.Rpg.Turns;
using backend.Infrastructure.Auth;
using backend.Modules.Changes;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/play")]
public sealed class PlayController : ControllerBase
{
    private const string DefaultStartMessage = """
        Начни новую одиночную RPG-сцену для моего персонажа.

        Используй текущее состояние игры, персонажей, мира, локации, квестов и истории из контекста.
        Представь стартовую сцену, ближайшую цель, атмосферу, важную угрозу или NPC.
        Не создавай финал сразу.
        Дай игроку понятную ситуацию и 2-4 естественных варианта действия, но не ограничивай его только ими.
        Если нужна проверка навыка или характеристики — предложи её через поддерживаемый JSON changes.
        """;

    private const string DefaultContinueMessage = """
        Продолжи сцену после последнего результата проверки или механического действия.
        Учти последние resolved mechanic requests, броски, pending/applied changes и текущее состояние игры.
        """;

    private readonly IGameStateService _gameStates;
    private readonly ICharacterService _characters;
    private readonly ITurnService _turns;
    private readonly IGameChangeService _changes;
    private readonly IMechanicRequestService _mechanicRequests;
    private readonly ICampaignMemoryService _memory;
    private readonly ICombatService _combat;
    private readonly IPlayBootstrapService _bootstrap;
    private readonly IPlayStateService _playState;
    private readonly IPlayOrchestratorService _orchestrator;
    private readonly ITravelService _travel;
    private readonly ICurrentUserService _currentUser;

    public PlayController(
        IGameStateService gameStates,
        ICharacterService characters,
        ITurnService turns,
        IGameChangeService changes,
        IMechanicRequestService mechanicRequests,
        ICampaignMemoryService memory,
        ICombatService combat,
        IPlayBootstrapService bootstrap,
        IPlayStateService playState,
        IPlayOrchestratorService orchestrator,
        ITravelService travel,
        ICurrentUserService currentUser)
    {
        _gameStates = gameStates;
        _characters = characters;
        _turns = turns;
        _changes = changes;
        _mechanicRequests = mechanicRequests;
        _memory = memory;
        _combat = combat;
        _bootstrap = bootstrap;
        _playState = playState;
        _orchestrator = orchestrator;
        _travel = travel;
        _currentUser = currentUser;
    }

    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Status(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _playState.BuildAsync(current.AccountId, gameStateId, new PlayStateBuildRequest(), cancellationToken));
    }

    [HttpPost("act")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Act(Guid gameStateId, [FromBody] PlayActRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _orchestrator.ActAsync(current.AccountId, gameStateId, request ?? new PlayActRequest(), cancellationToken));
    }

    [HttpPost("start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Start(Guid gameStateId, [FromBody] CreateTurnRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();

        var gameState = await _gameStates.GetGameStateAsync(current.AccountId, gameStateId, cancellationToken);
        if (!gameState.HasValue)
        {
            return NotFound(new MessageResponse { Message = "GameState не найден." });
        }

        var characters = await _characters.GetCharactersAsync(current.AccountId, gameStateId, cancellationToken);
        if (characters.Count == 0)
        {
            return BadRequest(new MessageResponse { Message = "Перед стартом сюжета нужно создать персонажа." });
        }

        var startRequest = new CreateTurnRequest
        {
            Message = string.IsNullOrWhiteSpace(request?.ResolvedPlayerMessage)
                ? DefaultStartMessage
                : request.ResolvedPlayerMessage
        };

        var result = await _turns.CreateTurnAsync(current.AccountId, gameStateId, startRequest, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("message")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Message(Guid gameStateId, [FromBody] CreateTurnRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var result = await _turns.CreateTurnAsync(current.AccountId, gameStateId, request ?? new CreateTurnRequest(), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("resolve-mechanic-request/{requestId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> ResolveMechanicRequest(
        Guid gameStateId,
        Guid requestId,
        [FromBody] MechanicRequestResolveAbilityCheckRequest? request,
        CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var result = await _mechanicRequests.ResolveAbilityCheckAsync(
            current.AccountId,
            gameStateId,
            requestId,
            request ?? new MechanicRequestResolveAbilityCheckRequest(),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("resolve-and-continue/{requestId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> ResolveAndContinue(
        Guid gameStateId,
        Guid requestId,
        [FromBody] PlayResolveAndContinueRequest? request,
        CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _orchestrator.ResolveAndContinueAsync(
            current.AccountId,
            gameStateId,
            requestId,
            request ?? new PlayResolveAndContinueRequest(),
            cancellationToken));
    }

    [HttpPost("continue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Continue(Guid gameStateId, [FromBody] PlayContinueRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var message = BuildContinueMessage(request ?? new PlayContinueRequest());
        var result = await _turns.CreateTurnAsync(
            current.AccountId,
            gameStateId,
            new CreateTurnRequest { Message = message },
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("apply-change/{changeId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ApplyChange(Guid gameStateId, Guid changeId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var result = await _changes.ApplyChangeAsync(current.AccountId, gameStateId, changeId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("reject-change/{changeId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RejectChange(Guid gameStateId, Guid changeId, [FromBody] RejectGameChangeRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var reason = string.IsNullOrWhiteSpace(request?.ResolvedReason)
            ? "Rejected by player."
            : request.ResolvedReason;
        var result = await _changes.RejectChangeAsync(current.AccountId, gameStateId, changeId, reason, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("apply-safe-changes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ApplySafeChanges(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _orchestrator.ApplySafeChangesAsync(current.AccountId, gameStateId, cancellationToken));
    }

    [HttpPost("travel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Travel(Guid gameStateId, [FromBody] PlayTravelRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var move = await _travel.MoveAsync(current.AccountId, gameStateId, request ?? new PlayTravelRequest(), cancellationToken);
        if (move.Status != RpgResultStatus.Ok)
        {
            return ToActionResult(move);
        }

        return ToActionResult(await _playState.BuildAsync(
            current.AccountId,
            gameStateId,
            new PlayStateBuildRequest(PreferredMode: "travel"),
            cancellationToken));
    }

    [HttpPost("location/move")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult> MoveLocation(Guid gameStateId, [FromBody] PlayTravelRequest? request, CancellationToken cancellationToken)
        => Travel(gameStateId, request, cancellationToken);

    [HttpPost("combat/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> StartPlayCombat(Guid gameStateId, [FromBody] PlayCombatStartRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var startRequest = new Contracts.Rpg.Combat.StartCombatRequest();
        var participants = request?.ResolvedParticipants ?? Array.Empty<Contracts.Rpg.Combat.AddCombatParticipantRequest>();
        if (participants.Count == 0)
        {
            return BadRequest(new MessageResponse { Message = "Для play/combat/start передайте participants. Автодобавление партии будет включено только после безопасного маппинга персонажей." });
        }

        startRequest.Participants.AddRange(participants);
        try
        {
            var id = await _combat.StartCombatAsync(current.AccountId, gameStateId, startRequest, cancellationToken);
            if (!id.HasValue)
            {
                return NotFound(new MessageResponse { Message = "GameState не найден." });
            }

            return ToActionResult(await _playState.BuildAsync(current.AccountId, gameStateId, new PlayStateBuildRequest(PreferredMode: "combat"), cancellationToken));
        }
        catch (CombatValidationException ex)
        {
            return BadRequest(new MessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("combat/action")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> PlayCombatAction(Guid gameStateId, [FromBody] PlayCombatActionRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var action = request?.ResolvedAction ?? string.Empty;
        if (!string.Equals(action, "attack", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new MessageResponse { Message = "В этом tranche play/combat/action поддерживает только action=attack." });
        }

        try
        {
            var result = await _combat.AttackAsync(current.AccountId, gameStateId, request?.Attack ?? new Contracts.Rpg.Combat.CombatAttackRequest(), cancellationToken);
            if (!result.HasValue)
            {
                return NotFound(new MessageResponse { Message = "Участники боя не найдены." });
            }

            return ToActionResult(await _playState.BuildAsync(current.AccountId, gameStateId, new PlayStateBuildRequest(PreferredMode: "combat"), cancellationToken));
        }
        catch (CombatValidationException ex)
        {
            return BadRequest(new MessageResponse { Message = ex.Message });
        }
    }

    [HttpPost("combat/end")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> EndPlayCombat(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var ended = await _combat.EndCombatAsync(current.AccountId, gameStateId, cancellationToken);
        if (!ended)
        {
            return NotFound(new MessageResponse { Message = "Активный бой не найден." });
        }

        return ToActionResult(await _playState.BuildAsync(current.AccountId, gameStateId, new PlayStateBuildRequest(), cancellationToken));
    }

    [HttpPost("combat/continue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> ContinuePlayCombat(Guid gameStateId, [FromBody] PlayContinueRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var message = BuildContinueMessage(request ?? new PlayContinueRequest());
        var result = await _turns.CreateTurnAsync(
            current.AccountId,
            gameStateId,
            new CreateTurnRequest { Message = $"Продолжи активный бой. {message}" },
            cancellationToken);
        if (result.Status != RpgResultStatus.Ok)
        {
            return ToActionResult(result);
        }

        var summary = await _orchestrator.ApplySafeChangesAsync(current.AccountId, gameStateId, cancellationToken);
        if (summary.Status != RpgResultStatus.Ok)
        {
            return ToActionResult(summary);
        }

        return ToActionResult(await _playState.BuildAsync(
            current.AccountId,
            gameStateId,
            new PlayStateBuildRequest(PreferredMode: "combat", MasterAnswer: GetOptionalString(result.Value, "masterAnswer"), ChangeSummary: summary.Value),
            cancellationToken));
    }

    [HttpPost("summarize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Summarize(Guid gameStateId, [FromBody] CampaignMemorySummarizeRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var result = await _memory.SummarizeMemoryAsync(
            current.AccountId,
            gameStateId,
            request ?? new CampaignMemorySummarizeRequest(),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("bootstrap")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Bootstrap(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var result = await _bootstrap.BootstrapAsync(current.AccountId, gameStateId, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Bad request." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Not found." }),
            RpgResultStatus.Conflict => Conflict(new MessageResponse { Message = result.Message ?? "Conflict." }),
            RpgResultStatus.ServiceUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, result.Value is null
                ? new MessageResponse { Message = result.Message ?? "Service unavailable." }
                : result.Value),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };

    private static string BuildContinueMessage(PlayContinueRequest request)
    {
        var message = string.IsNullOrWhiteSpace(request.ResolvedPlayerMessage)
            ? DefaultContinueMessage
            : request.ResolvedPlayerMessage;

        return string.IsNullOrWhiteSpace(request.ResolvedNote)
            ? message
            : $"{message.Trim()}{Environment.NewLine}{Environment.NewLine}Дополнительная заметка игрока: {request.ResolvedNote}";
    }

    private static string GetSkipReason(GameChangeOperationDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.OriginalOperation))
        {
            return "Operation отсутствует.";
        }

        return descriptor.Class switch
        {
            GameChangeOperationClass.Unknown => "unknown operation",
            GameChangeOperationClass.Dangerous => "dangerous operation",
            GameChangeOperationClass.Safe when !descriptor.IsSupported => "unsupported operation",
            GameChangeOperationClass.Unsupported => "unsupported operation",
            _ => "unsupported operation"
        };
    }

    private static string? GetOptionalString(JsonElement source, string name)
    {
        return source.ValueKind == JsonValueKind.Object
            && source.TryGetProperty(name, out var element)
            && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    private static Guid? GetOptionalGuid(JsonElement source, string name)
    {
        if (source.ValueKind != JsonValueKind.Object
            || !source.TryGetProperty(name, out var element)
            || element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.String && Guid.TryParse(element.GetString(), out var value)
            ? value
            : null;
    }
}
