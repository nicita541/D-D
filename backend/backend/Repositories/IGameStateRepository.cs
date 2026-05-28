using System.Text.Json;
using backend.Contracts.GameStates;

namespace backend.Repositories;

public interface IGameStateRepository
{
    Task<List<JsonElement>> GetGameStatesAsync(CancellationToken cancellationToken);

    Task<JsonElement?> GetGameStateAsync(Guid id, CancellationToken cancellationToken);

    Task<JsonElement?> GetGameStatePlayerAsync(Guid id, CancellationToken cancellationToken);

    Task<CreateGameStateResult> CreateGameStateAsync(string saveName, CancellationToken cancellationToken);

    Task<bool> DeleteGameStateAsync(Guid id, CancellationToken cancellationToken);
}
