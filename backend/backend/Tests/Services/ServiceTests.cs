using System.Text.Json;
using backend.Contracts.GameStates;
using backend.Models;
using backend.Repositories;
using backend.Services;

namespace Tests.Services;

public class ServiceTests
{
    [Fact]
    public async Task PlayerService_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        var player = new Player { Id = id };
        var repository = new FakePlayerRepository { Player = player, CreatedId = id };
        var service = new PlayerService(repository);

        var players = await service.GetPlayersAsync(CancellationToken.None);
        var found = await service.GetPlayerByIdAsync(id, CancellationToken.None);
        var createdId = await service.CreatePlayerAsync(player, CancellationToken.None);

        Assert.Single(players);
        Assert.Same(player, found);
        Assert.Equal(id, createdId);
        Assert.Same(player, repository.CreatedPlayer);
    }

    [Fact]
    public async Task GameStateService_UsesDefaultName_WhenNameIsMissing()
    {
        var repository = new FakeGameStateRepository();
        var service = new GameStateService(repository);

        await service.CreateGameStateAsync("  ", CancellationToken.None);

        Assert.Equal("Новая игра", repository.LastSaveName);
    }

    [Fact]
    public async Task GameStateService_TrimsProvidedName()
    {
        var repository = new FakeGameStateRepository();
        var service = new GameStateService(repository);

        await service.CreateGameStateAsync("  Save name  ", CancellationToken.None);

        Assert.Equal("Save name", repository.LastSaveName);
    }

    private sealed class FakePlayerRepository : IPlayerRepository
    {
        public Player? Player { get; init; }

        public Guid CreatedId { get; init; } = Guid.NewGuid();

        public Player? CreatedPlayer { get; private set; }

        public Task<List<Player>> GetPlayersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Player == null ? new List<Player>() : [Player]);
        }

        public Task<Player?> GetPlayerByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Player);
        }

        public Task<Guid> CreatePlayerAsync(Player player, CancellationToken cancellationToken)
        {
            CreatedPlayer = player;
            return Task.FromResult(CreatedId);
        }
    }

    private sealed class FakeGameStateRepository : IGameStateRepository
    {
        public string? LastSaveName { get; private set; }

        public Task<List<JsonElement>> GetGameStatesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new List<JsonElement>());
        }

        public Task<JsonElement?> GetGameStateAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult<JsonElement?>(null);
        }

        public Task<JsonElement?> GetGameStatePlayerAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult<JsonElement?>(null);
        }

        public Task<CreateGameStateResult> CreateGameStateAsync(
            string saveName,
            CancellationToken cancellationToken)
        {
            LastSaveName = saveName;

            return Task.FromResult(new CreateGameStateResult
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                SaveName = saveName
            });
        }

        public Task<bool> DeleteGameStateAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
