using System.Text.Json;
using backend.Modules.Story.Contracts;

namespace backend.Modules.Story.Infrastructure;

public interface IStoryRepository
{
    Task<JsonElement?> GetStoryStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
    Task<Guid?> UpsertStoryStateAsync(Guid accountId, Guid gameStateId, CreateOrUpdateStoryStateRequest request, CancellationToken cancellationToken);
}
