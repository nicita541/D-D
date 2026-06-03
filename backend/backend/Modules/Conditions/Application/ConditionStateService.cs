using System.Text.Json;
using backend.Modules.Conditions.Contracts;
using backend.Modules.Conditions.Infrastructure;
using backend.Shared.Kernel;

namespace backend.Modules.Conditions.Application;

public sealed class ConditionStateService : IConditionStateService
{
    private readonly IConditionStateRepository _repository;

    public ConditionStateService(IConditionStateRepository repository)
    {
        _repository = repository;
    }

    public async Task<RpgResult<JsonElement>> TickConditionsAsync(Guid accountId, Guid gameStateId, Guid characterId, TickConditionsRequest request, CancellationToken cancellationToken)
    {
        var result = await _repository.TickConditionsAsync(accountId, gameStateId, characterId, request.ResolvedTurns, cancellationToken);
        return result.HasValue
            ? RpgResult<JsonElement>.Ok(result.Value)
            : RpgResult<JsonElement>.NotFound("GameState или персонаж не найден.");
    }

    public async Task<RpgResult<JsonElement>> KnockoutAsync(Guid accountId, Guid gameStateId, Guid characterId, KnockoutRequest request, CancellationToken cancellationToken)
    {
        var result = await _repository.KnockoutAsync(accountId, gameStateId, characterId, request.ResolvedReason, cancellationToken);
        return result.HasValue
            ? RpgResult<JsonElement>.Ok(result.Value)
            : RpgResult<JsonElement>.NotFound("GameState или персонаж не найден.");
    }

    public async Task<RpgResult<JsonElement>> ReviveAsync(Guid accountId, Guid gameStateId, Guid characterId, ReviveRequest request, CancellationToken cancellationToken)
    {
        if (request.ResolvedHp > 10000)
        {
            return RpgResult<JsonElement>.BadRequest("hp не должен превышать 10000.");
        }

        var result = await _repository.ReviveAsync(accountId, gameStateId, characterId, request.ResolvedHp, request.ResolvedClearDead, request.ResolvedReason, cancellationToken);
        return result.HasValue
            ? RpgResult<JsonElement>.Ok(result.Value)
            : RpgResult<JsonElement>.NotFound("GameState или персонаж не найден.");
    }

    public async Task<RpgResult<IReadOnlyList<JsonElement>>> GetActiveConditionsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        var result = await _repository.GetActiveConditionsAsync(accountId, gameStateId, cancellationToken);
        return result is null
            ? RpgResult<IReadOnlyList<JsonElement>>.NotFound("GameState не найден.")
            : RpgResult<IReadOnlyList<JsonElement>>.Ok(result);
    }

    public async Task<RpgResult<IReadOnlyList<JsonElement>>> GetCharacterStatesAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        var result = await _repository.GetCharacterStatesAsync(accountId, gameStateId, cancellationToken);
        return result is null
            ? RpgResult<IReadOnlyList<JsonElement>>.NotFound("GameState не найден.")
            : RpgResult<IReadOnlyList<JsonElement>>.Ok(result);
    }
}
