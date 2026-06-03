using System.Text.Json;
using backend.Modules.Ai.Infrastructure;
using backend.Modules.Campaigns.Infrastructure;
using backend.Modules.Changes.Infrastructure;
using backend.Modules.Characters.Infrastructure;
using backend.Modules.Combat.Infrastructure;
using backend.Modules.GameStates.Infrastructure;
using backend.Modules.Mechanics.Infrastructure;
using backend.Modules.Memory.Infrastructure;
using backend.Modules.Party.Infrastructure;
using backend.Modules.Play.Infrastructure;
using backend.Modules.Story.Infrastructure;
using backend.Modules.Travel.Infrastructure;
using backend.Modules.Turns.Infrastructure;
using backend.Modules.World.Infrastructure;

namespace backend.Modules.GameStates.Application;

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

    public Task<Guid?> CreateGameStateAsync(Guid accountId, string? name, CancellationToken cancellationToken)
        => _repository.CreateGameStateAsync(accountId, string.IsNullOrWhiteSpace(name) ? "Новая игра" : name.Trim(), cancellationToken);

    public Task<bool> DeleteGameStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => _repository.DeleteGameStateAsync(accountId, gameStateId, cancellationToken);
}
