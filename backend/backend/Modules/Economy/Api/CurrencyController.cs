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
[Route("api/game-states/{gameStateId:guid}/characters/{characterId:guid}/currency")]
public sealed class CurrencyController : ControllerBase
{
    private readonly IEconomyService _economy;
    private readonly ICurrentUserService _currentUser;

    public CurrencyController(IEconomyService economy, ICurrentUserService currentUser)
    {
        _economy = economy;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult> GetCurrency(Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _economy.GetCurrencyAsync(current.AccountId, gameStateId, characterId, cancellationToken));
    }

    [HttpPost("add")]
    public async Task<ActionResult> AddCurrency(Guid gameStateId, Guid characterId, [FromBody] CurrencyChangeRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _economy.AddCurrencyAsync(current.AccountId, gameStateId, characterId, request ?? new CurrencyChangeRequest(), cancellationToken));
    }

    [HttpPost("spend")]
    public async Task<ActionResult> SpendCurrency(Guid gameStateId, Guid characterId, [FromBody] CurrencyChangeRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return ToActionResult(await _economy.SpendCurrencyAsync(current.AccountId, gameStateId, characterId, request ?? new CurrencyChangeRequest(), cancellationToken));
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
