using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Turns.Contracts;

namespace backend.Modules.Turns.Application;

public interface ITurnService
{
    Task<RpgResult<JsonElement>> CreateTurnAsync(Guid accountId, Guid gameStateId, CreateTurnRequest request, CancellationToken cancellationToken);

    Task<RpgResult<IReadOnlyList<JsonElement>>> GetTurnsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> GetTurnAsync(Guid accountId, Guid gameStateId, Guid turnId, CancellationToken cancellationToken);
}
