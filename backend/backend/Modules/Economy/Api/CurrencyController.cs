using backend.Infrastructure.Auth;
using backend.Modules.Economy.Application;
using backend.Modules.Economy.Contracts;
using backend.Modules.Realtime;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Economy.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/characters/{characterId:guid}/currency")]
public sealed class CurrencyController : ControllerBase
{
    private readonly IEconomyService _economy;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameAccessService _access;
    private readonly IGameRealtimeNotifier _realtime;

    public CurrencyController(
        IEconomyService economy,
        ICurrentUserService currentUser,
        IGameAccessService access,
        IGameRealtimeNotifier realtime)
    {
        _economy = economy;
        _currentUser = currentUser;
        _access = access;
        _realtime = realtime;
    }

    [HttpGet]
    public async Task<ActionResult> GetCurrency(Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        var access = await RequireCharacterAccessAsync(gameStateId, characterId, readOnly: true, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        return ToActionResult(await _economy.GetCurrencyAsync(access.Value!.OwnerAccountId, gameStateId, characterId, cancellationToken));
    }

    [HttpPost("add")]
    public async Task<ActionResult> AddCurrency(Guid gameStateId, Guid characterId, [FromBody] CurrencyChangeRequest? request, CancellationToken cancellationToken)
    {
        var access = await RequireCharacterAccessAsync(gameStateId, characterId, readOnly: false, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _economy.AddCurrencyAsync(access.Value!.OwnerAccountId, gameStateId, characterId, request ?? new CurrencyChangeRequest(), cancellationToken);
        await NotifyIfOk(result, gameStateId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("spend")]
    public async Task<ActionResult> SpendCurrency(Guid gameStateId, Guid characterId, [FromBody] CurrencyChangeRequest? request, CancellationToken cancellationToken)
    {
        var access = await RequireCharacterAccessAsync(gameStateId, characterId, readOnly: false, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var result = await _economy.SpendCurrencyAsync(access.Value!.OwnerAccountId, gameStateId, characterId, request ?? new CurrencyChangeRequest(), cancellationToken);
        await NotifyIfOk(result, gameStateId, cancellationToken);
        return ToActionResult(result);
    }

    private async Task<(GameAccess? Value, ActionResult? Error)> RequireCharacterAccessAsync(Guid gameStateId, Guid characterId, bool readOnly, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var access = await _access.GetAccessAsync(current.AccountId, gameStateId, cancellationToken);
        if (access is null)
        {
            return (null, NotFound(new MessageResponse { Message = "GameState не найден" }));
        }

        var allowed = readOnly
            ? access.CanReadGame
            : access.CanPlayGame && access.CanControlCharacter(characterId);
        if (!allowed)
        {
            return (access, StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = "Недостаточно прав для валюты персонажа." }));
        }

        return (access, null);
    }

    private async Task NotifyIfOk<T>(RpgResult<T> result, Guid gameStateId, CancellationToken cancellationToken)
    {
        if (result.Status == RpgResultStatus.Ok)
        {
            await _realtime.NotifyAsync(gameStateId, GameRealtimeEvents.InventoryUpdated, "currency changed", cancellationToken);
        }
    }

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Некорректный запрос." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Не найдено." }),
            RpgResultStatus.Conflict => Conflict(new MessageResponse { Message = result.Message ?? "Конфликт состояния." }),
            RpgResultStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new MessageResponse { Message = result.Message ?? "Недостаточно прав." }),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse { Message = result.Message ?? "Сервис временно недоступен." })
        };
}
