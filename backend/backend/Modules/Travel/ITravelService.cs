using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Modules.Play;

namespace backend.Modules.Travel;

public interface ITravelService
{
    Task<RpgResult<JsonElement>> GetOptionsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> MoveAsync(Guid accountId, Guid gameStateId, PlayTravelRequest request, CancellationToken cancellationToken);
}
