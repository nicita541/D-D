using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Mechanics;

namespace backend.Services.Rpg;

public interface IDiceRollService
{
    Task<RpgResult<JsonElement>> RollAsync(Guid accountId, Guid gameStateId, RollDiceRequest request, CancellationToken cancellationToken);

    Task<RpgResult<IReadOnlyList<JsonElement>>> GetRollsAsync(Guid accountId, Guid gameStateId, int limit, CancellationToken cancellationToken);
}
