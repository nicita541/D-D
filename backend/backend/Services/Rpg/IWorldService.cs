using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.World;

namespace backend.Services.Rpg;

public interface IWorldService
{
    Task<RpgResult<IReadOnlyList<JsonElement>>> ListAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid? parentId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> GetAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, CancellationToken cancellationToken);

    Task<RpgResult<Guid>> CreateAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid? parentId, JsonElement payload, CancellationToken cancellationToken);

    Task<RpgResult<bool>> UpdateAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, JsonElement payload, CancellationToken cancellationToken);

    Task<RpgResult<bool>> DeleteAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, CancellationToken cancellationToken);
}
