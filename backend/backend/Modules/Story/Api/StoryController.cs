using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Story.Contracts;
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

namespace backend.Modules.Story.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/story")]
public sealed class StoryController : ControllerBase
{
    private readonly IStoryService _story;
    private readonly ICurrentUserService _currentUser;

    public StoryController(IStoryService story, ICurrentUserService currentUser)
    {
        _story = story;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetStory(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var story = await _story.GetStoryStateAsync(current.AccountId, gameStateId, cancellationToken);
        return story.HasValue ? Ok(story.Value) : NotFound(new MessageResponse { Message = "Сюжетное состояние не найдено" });
    }

    [HttpPut]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> UpsertStory(Guid gameStateId, [FromBody] CreateOrUpdateStoryStateRequest request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var id = await _story.UpsertStoryStateAsync(current.AccountId, gameStateId, request ?? new CreateOrUpdateStoryStateRequest(), cancellationToken);
        return id.HasValue
            ? Ok(new OperationResponse { Id = id.Value, Message = "Сюжетное состояние сохранено" })
            : NotFound(new MessageResponse { Message = "GameState не найден" });
    }
}
