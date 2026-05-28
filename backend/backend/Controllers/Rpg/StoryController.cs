using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Story;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[ApiController]
[Route("api/game-states/{gameStateId:guid}/story")]
public sealed class StoryController : ControllerBase
{
    private readonly IStoryService _story;

    public StoryController(IStoryService story)
    {
        _story = story;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetStory(Guid gameStateId, CancellationToken cancellationToken)
    {
        var story = await _story.GetStoryStateAsync(gameStateId, cancellationToken);
        return story.HasValue ? Ok(story.Value) : NotFound(new MessageResponse { Message = "Сюжетное состояние не найдено" });
    }

    [HttpPut]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationResponse>> UpsertStory(Guid gameStateId, [FromBody] CreateOrUpdateStoryStateRequest request, CancellationToken cancellationToken)
    {
        var id = await _story.UpsertStoryStateAsync(gameStateId, request ?? new CreateOrUpdateStoryStateRequest(), cancellationToken);
        return Ok(new OperationResponse { Id = id, Message = "Сюжетное состояние сохранено" });
    }
}
