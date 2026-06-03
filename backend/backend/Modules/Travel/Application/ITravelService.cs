using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Play.Application;
using backend.Modules.Play.Contracts;
using backend.Modules.Play.Infrastructure;

namespace backend.Modules.Travel.Application;

public interface ITravelService
{
    Task<RpgResult<JsonElement>> GetOptionsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> MoveAsync(Guid accountId, Guid gameStateId, PlayTravelRequest request, CancellationToken cancellationToken);
}
