using System.Text.Json;
using backend.Modules.Combat.Contracts;

namespace backend.Modules.Combat.Infrastructure;

public interface ICombatOutcomeRepository
{
    Task<JsonElement?> GetOutcomeAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<JsonElement?> ResolveOutcomeAsync(Guid accountId, Guid gameStateId, PlayCombatResolveOutcomeRequest request, CancellationToken cancellationToken);
}
