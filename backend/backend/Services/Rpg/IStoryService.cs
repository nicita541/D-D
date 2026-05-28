using System.Text.Json;
using backend.Contracts.Rpg.Story;

namespace backend.Services.Rpg;

public interface IStoryService
{
    Task<JsonElement?> GetStoryStateAsync(Guid gameStateId, CancellationToken cancellationToken);
    Task<Guid> UpsertStoryStateAsync(Guid gameStateId, CreateOrUpdateStoryStateRequest request, CancellationToken cancellationToken);
}
