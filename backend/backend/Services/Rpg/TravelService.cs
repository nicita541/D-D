using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Play;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

public sealed class TravelService : ITravelService
{
    private readonly ITravelRepository _repository;

    public TravelService(ITravelRepository repository)
    {
        _repository = repository;
    }

    public async Task<RpgResult<JsonElement>> GetOptionsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        var options = await _repository.GetOptionsAsync(accountId, gameStateId, cancellationToken);
        return options.HasValue
            ? RpgResult<JsonElement>.Ok(options.Value)
            : RpgResult<JsonElement>.NotFound("GameState не найден.");
    }

    public async Task<RpgResult<JsonElement>> MoveAsync(Guid accountId, Guid gameStateId, PlayTravelRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _repository.MoveAsync(accountId, gameStateId, request, cancellationToken);
            return result.HasValue
                ? RpgResult<JsonElement>.Ok(result.Value)
                : RpgResult<JsonElement>.NotFound("GameState не найден.");
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<JsonElement>.BadRequest(ex.Message);
        }
    }
}
