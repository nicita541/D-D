using System.Text.Json;
using backend.Modules.Rest.Contracts;
using backend.Modules.Rest.Infrastructure;
using backend.Shared.Kernel;

namespace backend.Modules.Rest.Application;

public sealed class RestService : IRestService
{
    private readonly IRestRepository _repository;

    public RestService(IRestRepository repository)
    {
        _repository = repository;
    }

    public Task<RpgResult<JsonElement>> ShortRestAsync(Guid accountId, Guid gameStateId, RestRequest request, CancellationToken cancellationToken)
        => RestAsync(accountId, gameStateId, request, isLongRest: false, cancellationToken);

    public Task<RpgResult<JsonElement>> LongRestAsync(Guid accountId, Guid gameStateId, RestRequest request, CancellationToken cancellationToken)
        => RestAsync(accountId, gameStateId, request, isLongRest: true, cancellationToken);

    private async Task<RpgResult<JsonElement>> RestAsync(Guid accountId, Guid gameStateId, RestRequest request, bool isLongRest, CancellationToken cancellationToken)
    {
        var minutes = request.ResolvedMinutes ?? (isLongRest ? 480 : 60);
        if (minutes <= 0)
        {
            return RpgResult<JsonElement>.BadRequest("minutes должен быть больше 0.");
        }

        if (minutes > 1440)
        {
            return RpgResult<JsonElement>.BadRequest("minutes не должен превышать 1440.");
        }

        var result = await _repository.RestAsync(
            accountId,
            gameStateId,
            request.ResolvedCharacterId,
            isLongRest,
            minutes,
            request.ResolvedReason,
            cancellationToken);

        return result.HasValue
            ? RpgResult<JsonElement>.Ok(result.Value)
            : RpgResult<JsonElement>.NotFound("GameState или персонаж не найден.");
    }
}
