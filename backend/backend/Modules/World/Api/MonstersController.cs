using System.Text.Json;
using backend.Infrastructure.Auth;
using backend.Modules.World.Application;
using backend.Modules.World.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.World.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/monsters")]
public sealed class MonstersController : ControllerBase
{
    private readonly IWorldService _world;
    private readonly ICurrentUserService _currentUser;

    public MonstersController(IWorldService world, ICurrentUserService currentUser)
    {
        _world = world;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult> GetMonsters(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _world.ListAsync(current.AccountId, gameStateId, WorldEntityKind.Monster, null, cancellationToken));
    }

    [HttpGet("{monsterId:guid}")]
    public async Task<ActionResult> GetMonster(Guid gameStateId, Guid monsterId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _world.GetAsync(current.AccountId, gameStateId, WorldEntityKind.Monster, monsterId, null, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult> CreateMonster(Guid gameStateId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var result = await _world.CreateAsync(current.AccountId, gameStateId, WorldEntityKind.Monster, null, payload, cancellationToken);
        return result.Status == RpgResultStatus.Ok
            ? StatusCode(StatusCodes.Status201Created, new OperationResponse { Id = result.Value, Message = "Monster created." })
            : ToActionResult(result);
    }

    [HttpPost("spawn")]
    public async Task<ActionResult> SpawnMonster(Guid gameStateId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var normalizedPayload = MergePayload(payload, new Dictionary<string, object?>
        {
            ["isAlive"] = true,
            ["status"] = "alive"
        });
        var result = await _world.CreateAsync(current.AccountId, gameStateId, WorldEntityKind.Monster, null, normalizedPayload, cancellationToken);
        return result.Status == RpgResultStatus.Ok
            ? StatusCode(StatusCodes.Status201Created, new OperationResponse { Id = result.Value, Message = "Monster spawned." })
            : ToActionResult(result);
    }

    [HttpPost("{monsterId:guid}/kill")]
    public async Task<ActionResult> KillMonster(Guid gameStateId, Guid monsterId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var payload = JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["hpCurrent"] = 0,
            ["isAlive"] = false,
            ["status"] = "dead"
        });
        return ToOperationResult(
            await _world.UpdateAsync(current.AccountId, gameStateId, WorldEntityKind.Monster, monsterId, null, payload, cancellationToken),
            monsterId,
            "Monster killed.");
    }

    private static JsonElement MergePayload(JsonElement payload, IReadOnlyDictionary<string, object?> additions)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (payload.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in payload.EnumerateObject())
            {
                values[property.Name] = property.Value.Clone();
            }
        }

        foreach (var addition in additions)
        {
            values[addition.Key] = addition.Value;
        }

        return JsonSerializer.SerializeToElement(values);
    }

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Некорректный запрос." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Не найдено." }),
            RpgResultStatus.Conflict => Conflict(new MessageResponse { Message = result.Message ?? "Конфликт состояния." }),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse { Message = result.Message ?? "Сервис временно недоступен." })
        };

    private ActionResult ToOperationResult(RpgResult<bool> result, Guid id, string message)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(new OperationResponse { Id = id, Message = message }),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Некорректный запрос." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Не найдено." }),
            RpgResultStatus.Conflict => Conflict(new MessageResponse { Message = result.Message ?? "Конфликт состояния." }),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse { Message = result.Message ?? "Сервис временно недоступен." })
        };
}
