using System.Text.Json;

namespace backend.Services.Rpg;

public interface IGameStateService
{
    Task<IReadOnlyList<JsonElement>> GetGameStatesAsync(Guid accountId, CancellationToken cancellationToken);

    Task<JsonElement?> GetGameStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);

    Task<Guid> CreateGameStateAsync(Guid accountId, string? name, CancellationToken cancellationToken);

    Task<bool> DeleteGameStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
}
