using System.Text.Json;
using backend.Modules.World.Contracts;

namespace backend.Modules.World.Infrastructure;

public interface IWorldRepository
{
    Task<IReadOnlyList<JsonElement>?> ListAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid? parentId, CancellationToken cancellationToken);

    Task<JsonElement?> GetAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, CancellationToken cancellationToken);

    Task<Guid?> CreateAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid? parentId, JsonElement payload, CancellationToken cancellationToken);

    Task<bool?> UpdateAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, JsonElement payload, CancellationToken cancellationToken);

    Task<bool?> DeleteAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, CancellationToken cancellationToken);
}
