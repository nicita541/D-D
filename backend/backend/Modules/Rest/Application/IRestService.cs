using System.Text.Json;
using backend.Modules.Rest.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.Rest.Application;

public interface IRestService
{
    Task<RpgResult<JsonElement>> ShortRestAsync(Guid accountId, Guid gameStateId, RestRequest request, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> LongRestAsync(Guid accountId, Guid gameStateId, RestRequest request, CancellationToken cancellationToken);
}
