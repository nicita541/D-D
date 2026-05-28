using System.Text.Json;
using backend.Contracts.Rpg.Story;

namespace backend.Repositories.Rpg;

public interface IStoryRepository
{
    Task<JsonElement?> GetStoryStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
    Task<Guid?> UpsertStoryStateAsync(Guid accountId, Guid gameStateId, CreateOrUpdateStoryStateRequest request, CancellationToken cancellationToken);
}
