using backend.Infrastructure.Auth;
using backend.Modules.Economy.Application;
using backend.Modules.Economy.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Economy.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/loot")]
public sealed class LootController : ControllerBase
{
    private readonly IEconomyService _economy;
    private readonly ICurrentUserService _currentUser;

    public LootController(IEconomyService economy, ICurrentUserService currentUser)
    {
        _economy = economy;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult> GetLoot(Guid gameStateId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _economy.GetLootAsync(current.AccountId, gameStateId, cancellationToken));
    }

    [HttpGet("{lootContainerId:guid}")]
    public async Task<ActionResult> GetLootContainer(Guid gameStateId, Guid lootContainerId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _economy.GetLootContainerAsync(current.AccountId, gameStateId, lootContainerId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult> CreateLoot(Guid gameStateId, [FromBody] CreateLootContainerRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var result = await _economy.CreateLootAsync(current.AccountId, gameStateId, request ?? new CreateLootContainerRequest(), cancellationToken);
        return result.Status == RpgResultStatus.Ok
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{lootContainerId:guid}/claim")]
    public async Task<ActionResult> ClaimLoot(Guid gameStateId, Guid lootContainerId, [FromBody] ClaimLootRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _economy.ClaimLootAsync(current.AccountId, gameStateId, lootContainerId, request ?? new ClaimLootRequest(), cancellationToken));
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
}
