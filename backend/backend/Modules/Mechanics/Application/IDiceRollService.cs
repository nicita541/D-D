using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Mechanics.Contracts;

namespace backend.Modules.Mechanics.Application;

public interface IDiceRollService
{
    Task<RpgResult<JsonElement>> RollAsync(Guid accountId, Guid gameStateId, RollDiceRequest request, CancellationToken cancellationToken);

    Task<RpgResult<IReadOnlyList<JsonElement>>> GetRollsAsync(Guid accountId, Guid gameStateId, int limit, CancellationToken cancellationToken);
}
