using System.Text.Json;
using backend.Contracts.GameStates;
using backend.Repositories;

namespace backend.Services;

public sealed class GameStateService : IGameStateService
{
    private const string DefaultSaveName = "Новая игра";

    private readonly IGameStateRepository _gameStates;

    public GameStateService(IGameStateRepository gameStates)
    {
        _gameStates = gameStates;
    }

    public Task<List<JsonElement>> GetGameStatesAsync(CancellationToken cancellationToken)
    {
        return _gameStates.GetGameStatesAsync(cancellationToken);
    }

    public Task<JsonElement?> GetGameStateAsync(Guid id, CancellationToken cancellationToken)
    {
        return _gameStates.GetGameStateAsync(id, cancellationToken);
    }

    public Task<JsonElement?> GetGameStatePlayerAsync(Guid id, CancellationToken cancellationToken)
    {
        return _gameStates.GetGameStatePlayerAsync(id, cancellationToken);
    }

    public Task<CreateGameStateResult> CreateGameStateAsync(string? name, CancellationToken cancellationToken)
    {
        var saveName = string.IsNullOrWhiteSpace(name)
            ? DefaultSaveName
            : name.Trim();

        return _gameStates.CreateGameStateAsync(saveName, cancellationToken);
    }

    public Task<bool> DeleteGameStateAsync(Guid id, CancellationToken cancellationToken)
    {
        return _gameStates.DeleteGameStateAsync(id, cancellationToken);
    }
}
