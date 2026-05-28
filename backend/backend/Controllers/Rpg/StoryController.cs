using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Story;
using backend.Infrastructure.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

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
