using System.Text.Json;
using backend.Modules.Combat.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.Combat.Application;

public interface ICombatOutcomeService
{
    Task<RpgResult<JsonElement>> GetOutcomeAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> ResolveOutcomeAsync(Guid accountId, Guid gameStateId, PlayCombatResolveOutcomeRequest request, CancellationToken cancellationToken);
}
