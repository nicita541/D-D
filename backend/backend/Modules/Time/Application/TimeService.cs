using System.Text.Json;
using backend.Modules.Time.Contracts;
using backend.Modules.Time.Infrastructure;
using backend.Shared.Kernel;

namespace backend.Modules.Time.Application;

public sealed class TimeService : ITimeService
{
    private readonly ITimeRepository _repository;

    public TimeService(ITimeRepository repository)
    {
        _repository = repository;
    }

    public async Task<RpgResult<JsonElement>> GetTimeAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        var result = await _repository.GetTimeAsync(accountId, gameStateId, cancellationToken);
        return result.HasValue
            ? RpgResult<JsonElement>.Ok(result.Value)
            : RpgResult<JsonElement>.NotFound("GameState не найден.");
    }

    public async Task<RpgResult<JsonElement>> AdvanceTimeAsync(Guid accountId, Guid gameStateId, AdvanceTimeRequest request, CancellationToken cancellationToken)
    {
        var minutes = request.ResolvedMinutes;
        if (!minutes.HasValue || minutes.Value <= 0)
        {
            return RpgResult<JsonElement>.BadRequest("minutes должен быть больше 0.");
        }

        if (minutes.Value > 1440 * 30)
        {
            return RpgResult<JsonElement>.BadRequest("minutes не должен превышать 43200.");
        }

        var result = await _repository.AdvanceTimeAsync(
            accountId,
            gameStateId,
            minutes.Value,
            request.ResolvedReason,
            request.ResolvedTickConditions,
            cancellationToken);

        return result.HasValue
            ? RpgResult<JsonElement>.Ok(result.Value)
            : RpgResult<JsonElement>.NotFound("GameState не найден.");
    }
}
