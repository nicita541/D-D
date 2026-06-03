using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Play.Application;
using backend.Modules.Play.Contracts;
using backend.Modules.Play.Infrastructure;
using backend.Modules.Ai.Infrastructure;
using backend.Modules.Campaigns.Infrastructure;
using backend.Modules.Changes.Infrastructure;
using backend.Modules.Characters.Infrastructure;
using backend.Modules.Combat.Infrastructure;
using backend.Modules.GameStates.Infrastructure;
using backend.Modules.Mechanics.Infrastructure;
using backend.Modules.Memory.Infrastructure;
using backend.Modules.Party.Infrastructure;
using backend.Modules.Play.Infrastructure;
using backend.Modules.Story.Infrastructure;
using backend.Modules.Travel.Infrastructure;
using backend.Modules.Turns.Infrastructure;
using backend.Modules.World.Infrastructure;

namespace backend.Modules.Travel.Application;

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
