using System.Text.Json;
using backend.Modules.Story.Contracts;

namespace backend.Modules.Story.Application;

public interface IStoryService
{
    Task<JsonElement?> GetStoryStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
    Task<Guid?> UpsertStoryStateAsync(Guid accountId, Guid gameStateId, CreateOrUpdateStoryStateRequest request, CancellationToken cancellationToken);
}
