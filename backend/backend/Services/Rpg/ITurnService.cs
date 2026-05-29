using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Turns;

namespace backend.Services.Rpg;

public interface ITurnService
{
    Task<RpgResult<JsonElement>> CreateTurnAsync(Guid accountId, Guid gameStateId, CreateTurnRequest request, CancellationToken cancellationToken);

    Task<RpgResult<IReadOnlyList<JsonElement>>> GetTurnsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> GetTurnAsync(Guid accountId, Guid gameStateId, Guid turnId, CancellationToken cancellationToken);
}
