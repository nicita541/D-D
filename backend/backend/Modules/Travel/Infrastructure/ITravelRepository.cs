using System.Text.Json;
using backend.Contracts.Rpg.Play;

namespace backend.Repositories.Rpg;

public interface ITravelRepository
{
    Task<JsonElement?> GetOptionsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<JsonElement?> MoveAsync(
        Guid accountId,
        Guid gameStateId,
        PlayTravelRequest request,
        CancellationToken cancellationToken);
}
