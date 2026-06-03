using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.World.Contracts;
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

namespace backend.Modules.World.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/world")]
public sealed class WorldController : ControllerBase
{
    private readonly IWorldService _world;
    private readonly ICurrentUserService _currentUser;

    public WorldController(IWorldService world, ICurrentUserService currentUser)
    {
        _world = world;
        _currentUser = currentUser;
    }

    [HttpGet("locations")]
    public Task<ActionResult> GetLocations(Guid gameStateId, CancellationToken cancellationToken)
        => List(gameStateId, WorldEntityKind.Location, null, cancellationToken);

    [HttpPost("locations")]
    public Task<ActionResult> CreateLocation(Guid gameStateId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Create(gameStateId, WorldEntityKind.Location, null, payload, cancellationToken);

    [HttpGet("locations/{locationId:guid}")]
    public Task<ActionResult> GetLocation(Guid gameStateId, Guid locationId, CancellationToken cancellationToken)
        => Get(gameStateId, WorldEntityKind.Location, locationId, null, cancellationToken);

    [HttpPut("locations/{locationId:guid}")]
    public Task<ActionResult> UpdateLocation(Guid gameStateId, Guid locationId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Update(gameStateId, WorldEntityKind.Location, locationId, null, payload, cancellationToken);

    [HttpDelete("locations/{locationId:guid}")]
    public Task<ActionResult> DeleteLocation(Guid gameStateId, Guid locationId, CancellationToken cancellationToken)
        => Delete(gameStateId, WorldEntityKind.Location, locationId, null, cancellationToken);

    [HttpGet("locations/{locationId:guid}/exits")]
    public Task<ActionResult> GetLocationExits(Guid gameStateId, Guid locationId, CancellationToken cancellationToken)
        => List(gameStateId, WorldEntityKind.LocationExit, locationId, cancellationToken);

    [HttpPost("locations/{locationId:guid}/exits")]
    public Task<ActionResult> CreateLocationExit(Guid gameStateId, Guid locationId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Create(gameStateId, WorldEntityKind.LocationExit, locationId, payload, cancellationToken);

    [HttpPut("locations/{locationId:guid}/exits/{exitId:guid}")]
    public Task<ActionResult> UpdateLocationExit(Guid gameStateId, Guid locationId, Guid exitId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Update(gameStateId, WorldEntityKind.LocationExit, exitId, locationId, payload, cancellationToken);

    [HttpDelete("locations/{locationId:guid}/exits/{exitId:guid}")]
    public Task<ActionResult> DeleteLocationExit(Guid gameStateId, Guid locationId, Guid exitId, CancellationToken cancellationToken)
        => Delete(gameStateId, WorldEntityKind.LocationExit, exitId, locationId, cancellationToken);

    [HttpGet("objects")]
    public Task<ActionResult> GetObjects(Guid gameStateId, CancellationToken cancellationToken)
        => List(gameStateId, WorldEntityKind.WorldObject, null, cancellationToken);

    [HttpPost("objects")]
    public Task<ActionResult> CreateObject(Guid gameStateId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Create(gameStateId, WorldEntityKind.WorldObject, null, payload, cancellationToken);

    [HttpPut("objects/{objectId:guid}")]
    public Task<ActionResult> UpdateObject(Guid gameStateId, Guid objectId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Update(gameStateId, WorldEntityKind.WorldObject, objectId, null, payload, cancellationToken);

    [HttpDelete("objects/{objectId:guid}")]
    public Task<ActionResult> DeleteObject(Guid gameStateId, Guid objectId, CancellationToken cancellationToken)
        => Delete(gameStateId, WorldEntityKind.WorldObject, objectId, null, cancellationToken);

    [HttpGet("containers")]
    public Task<ActionResult> GetContainers(Guid gameStateId, CancellationToken cancellationToken)
        => List(gameStateId, WorldEntityKind.Container, null, cancellationToken);

    [HttpPost("containers")]
    public Task<ActionResult> CreateContainer(Guid gameStateId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Create(gameStateId, WorldEntityKind.Container, null, payload, cancellationToken);

    [HttpPut("containers/{containerId:guid}")]
    public Task<ActionResult> UpdateContainer(Guid gameStateId, Guid containerId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Update(gameStateId, WorldEntityKind.Container, containerId, null, payload, cancellationToken);

    [HttpDelete("containers/{containerId:guid}")]
    public Task<ActionResult> DeleteContainer(Guid gameStateId, Guid containerId, CancellationToken cancellationToken)
        => Delete(gameStateId, WorldEntityKind.Container, containerId, null, cancellationToken);

    [HttpGet("npcs")]
    public Task<ActionResult> GetNpcs(Guid gameStateId, CancellationToken cancellationToken)
        => List(gameStateId, WorldEntityKind.Npc, null, cancellationToken);

    [HttpPost("npcs")]
    public Task<ActionResult> CreateNpc(Guid gameStateId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Create(gameStateId, WorldEntityKind.Npc, null, payload, cancellationToken);

    [HttpPut("npcs/{npcId:guid}")]
    public Task<ActionResult> UpdateNpc(Guid gameStateId, Guid npcId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Update(gameStateId, WorldEntityKind.Npc, npcId, null, payload, cancellationToken);

    [HttpDelete("npcs/{npcId:guid}")]
    public Task<ActionResult> DeleteNpc(Guid gameStateId, Guid npcId, CancellationToken cancellationToken)
        => Delete(gameStateId, WorldEntityKind.Npc, npcId, null, cancellationToken);

    [HttpGet("factions")]
    public Task<ActionResult> GetFactions(Guid gameStateId, CancellationToken cancellationToken)
        => List(gameStateId, WorldEntityKind.Faction, null, cancellationToken);

    [HttpPost("factions")]
    public Task<ActionResult> CreateFaction(Guid gameStateId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Create(gameStateId, WorldEntityKind.Faction, null, payload, cancellationToken);

    [HttpPut("factions/{factionId:guid}")]
    public Task<ActionResult> UpdateFaction(Guid gameStateId, Guid factionId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Update(gameStateId, WorldEntityKind.Faction, factionId, null, payload, cancellationToken);

    [HttpDelete("factions/{factionId:guid}")]
    public Task<ActionResult> DeleteFaction(Guid gameStateId, Guid factionId, CancellationToken cancellationToken)
        => Delete(gameStateId, WorldEntityKind.Faction, factionId, null, cancellationToken);

    [HttpGet("quests")]
    public Task<ActionResult> GetQuests(Guid gameStateId, CancellationToken cancellationToken)
        => List(gameStateId, WorldEntityKind.Quest, null, cancellationToken);

    [HttpPost("quests")]
    public Task<ActionResult> CreateQuest(Guid gameStateId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Create(gameStateId, WorldEntityKind.Quest, null, payload, cancellationToken);

    [HttpPut("quests/{questId:guid}")]
    public Task<ActionResult> UpdateQuest(Guid gameStateId, Guid questId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Update(gameStateId, WorldEntityKind.Quest, questId, null, payload, cancellationToken);

    [HttpDelete("quests/{questId:guid}")]
    public Task<ActionResult> DeleteQuest(Guid gameStateId, Guid questId, CancellationToken cancellationToken)
        => Delete(gameStateId, WorldEntityKind.Quest, questId, null, cancellationToken);

    [HttpGet("quests/{questId:guid}/steps")]
    public Task<ActionResult> GetQuestSteps(Guid gameStateId, Guid questId, CancellationToken cancellationToken)
        => List(gameStateId, WorldEntityKind.QuestStep, questId, cancellationToken);

    [HttpPost("quests/{questId:guid}/steps")]
    public Task<ActionResult> CreateQuestStep(Guid gameStateId, Guid questId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Create(gameStateId, WorldEntityKind.QuestStep, questId, payload, cancellationToken);

    [HttpPut("quests/{questId:guid}/steps/{stepId:guid}")]
    public Task<ActionResult> UpdateQuestStep(Guid gameStateId, Guid questId, Guid stepId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Update(gameStateId, WorldEntityKind.QuestStep, stepId, questId, payload, cancellationToken);

    [HttpDelete("quests/{questId:guid}/steps/{stepId:guid}")]
    public Task<ActionResult> DeleteQuestStep(Guid gameStateId, Guid questId, Guid stepId, CancellationToken cancellationToken)
        => Delete(gameStateId, WorldEntityKind.QuestStep, stepId, questId, cancellationToken);

    [HttpGet("monsters")]
    public Task<ActionResult> GetMonsters(Guid gameStateId, CancellationToken cancellationToken)
        => List(gameStateId, WorldEntityKind.Monster, null, cancellationToken);

    [HttpPost("monsters")]
    public Task<ActionResult> CreateMonster(Guid gameStateId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Create(gameStateId, WorldEntityKind.Monster, null, payload, cancellationToken);

    [HttpGet("monsters/{monsterId:guid}")]
    public Task<ActionResult> GetMonster(Guid gameStateId, Guid monsterId, CancellationToken cancellationToken)
        => Get(gameStateId, WorldEntityKind.Monster, monsterId, null, cancellationToken);

    [HttpPut("monsters/{monsterId:guid}")]
    public Task<ActionResult> UpdateMonster(Guid gameStateId, Guid monsterId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
        => Update(gameStateId, WorldEntityKind.Monster, monsterId, null, payload, cancellationToken);

    [HttpDelete("monsters/{monsterId:guid}")]
    public Task<ActionResult> DeleteMonster(Guid gameStateId, Guid monsterId, CancellationToken cancellationToken)
        => Delete(gameStateId, WorldEntityKind.Monster, monsterId, null, cancellationToken);

    private async Task<ActionResult> List(Guid gameStateId, WorldEntityKind kind, Guid? parentId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _world.ListAsync(current.AccountId, gameStateId, kind, parentId, cancellationToken));
    }

    private async Task<ActionResult> Get(Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _world.GetAsync(current.AccountId, gameStateId, kind, entityId, parentId, cancellationToken));
    }

    private async Task<ActionResult> Create(Guid gameStateId, WorldEntityKind kind, Guid? parentId, JsonElement payload, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var result = await _world.CreateAsync(current.AccountId, gameStateId, kind, parentId, payload, cancellationToken);
        return result.Status switch
        {
            RpgResultStatus.Ok => StatusCode(StatusCodes.Status201Created, new OperationResponse { Id = result.Value, Message = "World entity created." }),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Bad request." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Not found." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private async Task<ActionResult> Update(Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, JsonElement payload, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToOperationResult(await _world.UpdateAsync(current.AccountId, gameStateId, kind, entityId, parentId, payload, cancellationToken), entityId, "World entity updated.");
    }

    private async Task<ActionResult> Delete(Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToOperationResult(await _world.DeleteAsync(current.AccountId, gameStateId, kind, entityId, parentId, cancellationToken), entityId, "World entity deleted.");
    }

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Bad request." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Not found." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };

    private ActionResult ToOperationResult(RpgResult<bool> result, Guid id, string message)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(new OperationResponse { Id = id, Message = message }),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Bad request." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Not found." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
}
