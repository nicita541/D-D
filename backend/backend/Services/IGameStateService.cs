using System.Text.Json;
using backend.Contracts.GameStates;

namespace backend.Services;

public interface IGameStateService
{
    Task<List<JsonElement>> GetGameStatesAsync(CancellationToken cancellationToken);

    Task<JsonElement?> GetGameStateAsync(Guid id, CancellationToken cancellationToken);

    Task<JsonElement?> GetGameStatePlayerAsync(Guid id, CancellationToken cancellationToken);

    Task<CreateGameStateResult> CreateGameStateAsync(string? name, CancellationToken cancellationToken);

    Task<bool> DeleteGameStateAsync(Guid id, CancellationToken cancellationToken);
}
