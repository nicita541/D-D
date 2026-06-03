using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.Changes.Application;

public interface IGameChangeService
{
    Task<RpgResult<IReadOnlyList<JsonElement>>> GetChangesAsync(Guid accountId, Guid gameStateId, string? status, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> GetChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> ApplyChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> RejectChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, string reason, CancellationToken cancellationToken);
}
