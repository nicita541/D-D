using System.Text.Json;
using backend.Modules.Play;

namespace backend.Modules.Travel;

public interface ITravelRepository
{
    Task<JsonElement?> GetOptionsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<JsonElement?> MoveAsync(
        Guid accountId,
        Guid gameStateId,
        PlayTravelRequest request,
        CancellationToken cancellationToken);
}
