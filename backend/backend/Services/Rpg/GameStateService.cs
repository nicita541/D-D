using System.Text.Json;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

public sealed class GameStateService : IGameStateService
{
    private readonly IGameStateRepository _repository;

    public GameStateService(IGameStateRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<JsonElement>> GetGameStatesAsync(Guid accountId, CancellationToken cancellationToken)
        => _repository.GetGameStatesAsync(accountId, cancellationToken);

    public Task<JsonElement?> GetGameStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => _repository.GetGameStateAsync(accountId, gameStateId, cancellationToken);

    public Task<Guid> CreateGameStateAsync(Guid accountId, string? name, CancellationToken cancellationToken)
        => _repository.CreateGameStateAsync(accountId, string.IsNullOrWhiteSpace(name) ? "Новая игра" : name.Trim(), cancellationToken);

    public Task<bool> DeleteGameStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => _repository.DeleteGameStateAsync(accountId, gameStateId, cancellationToken);
}
