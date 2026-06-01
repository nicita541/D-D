using backend.Modules.Changes;
using backend.Contracts.Rpg.Mechanics;
using backend.Contracts.Rpg.Memory;
using backend.Contracts.Rpg.Turns;
using backend.Infrastructure.Auth;
using backend.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Play;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/play")]
public sealed class PlayController : ControllerBase
{
    private readonly IPlayApplicationService _play;
    private readonly IPlayTravelFacade _travel;
    private readonly IPlayCombatFacade _combat;
    private readonly ICurrentUserService _currentUser;

    public PlayController(
        IPlayApplicationService play,
        IPlayTravelFacade travel,
        IPlayCombatFacade combat,
        ICurrentUserService currentUser)
    {
        _play = play;
        _travel = travel;
        _combat = combat;
        _currentUser = currentUser;
    }

    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Status(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return this.ToActionResult(await _play.StatusAsync(current.AccountId, gameStateId, cancellationToken));
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
        return this.ToActionResult(await _play.ActAsync(current.AccountId, gameStateId, request ?? new PlayActRequest(), cancellationToken));
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
        return this.ToActionResult(await _play.StartAsync(current.AccountId, gameStateId, request, cancellationToken));
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
        return this.ToActionResult(await _play.MessageAsync(current.AccountId, gameStateId, request, cancellationToken));
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
        return this.ToActionResult(await _play.ResolveMechanicRequestAsync(current.AccountId, gameStateId, requestId, request, cancellationToken));
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
        return this.ToActionResult(await _play.ResolveAndContinueAsync(
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
        return this.ToActionResult(await _play.ContinueAsync(current.AccountId, gameStateId, request, cancellationToken));
    }

    [HttpPost("apply-change/{changeId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ApplyChange(Guid gameStateId, Guid changeId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return this.ToActionResult(await _play.ApplyChangeAsync(current.AccountId, gameStateId, changeId, cancellationToken));
    }

    [HttpPost("reject-change/{changeId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RejectChange(Guid gameStateId, Guid changeId, [FromBody] RejectGameChangeRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return this.ToActionResult(await _play.RejectChangeAsync(current.AccountId, gameStateId, changeId, request, cancellationToken));
    }

    [HttpPost("apply-safe-changes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ApplySafeChanges(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return this.ToActionResult(await _play.ApplySafeChangesAsync(current.AccountId, gameStateId, cancellationToken));
    }

    [HttpPost("travel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Travel(Guid gameStateId, [FromBody] PlayTravelRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return this.ToActionResult(await _travel.TravelAsync(current.AccountId, gameStateId, request ?? new PlayTravelRequest(), cancellationToken));
    }

    [HttpPost("location/move")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MoveLocation(Guid gameStateId, [FromBody] PlayTravelRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return this.ToActionResult(await _travel.MoveLocationAsync(current.AccountId, gameStateId, request ?? new PlayTravelRequest(), cancellationToken));
    }

    [HttpPost("combat/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> StartPlayCombat(Guid gameStateId, [FromBody] PlayCombatStartRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return this.ToActionResult(await _combat.StartAsync(current.AccountId, gameStateId, request ?? new PlayCombatStartRequest(), cancellationToken));
    }

    [HttpPost("combat/action")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> PlayCombatAction(Guid gameStateId, [FromBody] PlayCombatActionRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return this.ToActionResult(await _combat.ActionAsync(current.AccountId, gameStateId, request ?? new PlayCombatActionRequest(), cancellationToken));
    }

    [HttpPost("combat/end")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> EndPlayCombat(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return this.ToActionResult(await _combat.EndAsync(current.AccountId, gameStateId, cancellationToken));
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
        return this.ToActionResult(await _combat.ContinueAsync(current.AccountId, gameStateId, request ?? new PlayContinueRequest(), cancellationToken));
    }

    [HttpPost("summarize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Summarize(Guid gameStateId, [FromBody] CampaignMemorySummarizeRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return this.ToActionResult(await _play.SummarizeAsync(current.AccountId, gameStateId, request, cancellationToken));
    }

    [HttpPost("bootstrap")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Bootstrap(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return this.ToActionResult(await _play.BootstrapAsync(current.AccountId, gameStateId, cancellationToken));
    }
}
