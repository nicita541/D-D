using System.Text.Json;
using backend.Modules.Combat.Contracts;
using backend.Modules.Combat.Infrastructure;
using backend.Shared.Kernel;

namespace backend.Modules.Combat.Application;

public sealed class CombatOutcomeService : ICombatOutcomeService
{
    private readonly ICombatOutcomeRepository _repository;

    public CombatOutcomeService(ICombatOutcomeRepository repository)
    {
        _repository = repository;
    }

    public async Task<RpgResult<JsonElement>> GetOutcomeAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        var result = await _repository.GetOutcomeAsync(accountId, gameStateId, cancellationToken);
        return result.HasValue
            ? RpgResult<JsonElement>.Ok(result.Value)
            : RpgResult<JsonElement>.NotFound("Активный бой не найден.");
    }

    public async Task<RpgResult<JsonElement>> ResolveOutcomeAsync(Guid accountId, Guid gameStateId, PlayCombatResolveOutcomeRequest request, CancellationToken cancellationToken)
    {
        var result = await _repository.ResolveOutcomeAsync(accountId, gameStateId, request, cancellationToken);
        return result.HasValue
            ? RpgResult<JsonElement>.Ok(result.Value)
            : RpgResult<JsonElement>.NotFound("Активный бой не найден.");
    }
}
