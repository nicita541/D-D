using System.Text.Json;
using backend.Modules.Play.Application;
using backend.Modules.Play.Contracts;
using backend.Modules.Play.Infrastructure;

namespace backend.Modules.Travel.Infrastructure;

public interface ITravelRepository
{
    Task<JsonElement?> GetOptionsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<JsonElement?> MoveAsync(
        Guid accountId,
        Guid gameStateId,
        PlayTravelRequest request,
        CancellationToken cancellationToken);
}
