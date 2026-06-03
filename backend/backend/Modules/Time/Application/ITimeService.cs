using System.Text.Json;
using backend.Modules.Time.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.Time.Application;

public interface ITimeService
{
    Task<RpgResult<JsonElement>> GetTimeAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> AdvanceTimeAsync(Guid accountId, Guid gameStateId, AdvanceTimeRequest request, CancellationToken cancellationToken);
}
