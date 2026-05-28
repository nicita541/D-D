using System.Text.Json;
using backend.Contracts.Common;
using backend.Contracts.GameStates;
using backend.Controllers;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Controllers;

public class GameStatesControllerTests
{
    [Fact]
    public async Task GetGameState_ReturnsOk_WhenDocumentExists()
    {
        var document = Json("""{"id":"game-1"}""");
        var controller = new GameStatesController(new FakeGameStateService { GameState = document });

        var result = await controller.GetGameState(Guid.NewGuid(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(document.GetRawText(), ((JsonElement)ok.Value!).GetRawText());
    }

    [Fact]
    public async Task GetGameState_ReturnsNotFound_WhenDocumentDoesNotExist()
    {
        var id = Guid.NewGuid();
        var controller = new GameStatesController(new FakeGameStateService());

        var result = await controller.GetGameState(id, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<MessageWithIdResponse>(notFound.Value);
        Assert.Equal(id, response.Id);
    }

    [Fact]
    public async Task GetGameStatePlayer_ReturnsOk_WhenPlayerExists()
    {
        var player = Json("""{"id":"player-1"}""");
        var controller = new GameStatesController(new FakeGameStateService { Player = player });

        var result = await controller.GetGameStatePlayer(Guid.NewGuid(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(player.GetRawText(), ((JsonElement)ok.Value!).GetRawText());
    }

    [Fact]
    public async Task GetGameStatePlayer_ReturnsNotFound_WhenPlayerDoesNotExist()
    {
        var id = Guid.NewGuid();
        var controller = new GameStatesController(new FakeGameStateService());

        var result = await controller.GetGameStatePlayer(id, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<MessageWithIdResponse>(notFound.Value);
        Assert.Equal(id, response.Id);
    }

    [Fact]
    public async Task CreateGameState_ReturnsCreated()
    {
        var id = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var controller = new GameStatesController(new FakeGameStateService
        {
            Created = new CreateGameStateResult
            {
                Id = id,
                AccountId = accountId,
                SaveName = "Save"
            }
        });

        var result = await controller.CreateGameState(
            new CreateGameStateRequest { Name = "Save" },
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<CreateGameStateResponse>(created.Value);
        Assert.Equal(nameof(GameStatesController.GetGameState), created.ActionName);
        Assert.Equal(id, response.Id);
        Assert.Equal(accountId, response.AccountId);
        Assert.Equal("Save", response.SaveName);
    }

    [Fact]
    public async Task DeleteGameState_ReturnsOk_WhenDeleted()
    {
        var id = Guid.NewGuid();
        var controller = new GameStatesController(new FakeGameStateService { Deleted = true });

        var result = await controller.DeleteGameState(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MessageWithIdResponse>(ok.Value);
        Assert.Equal(id, response.Id);
    }

    [Fact]
    public async Task DeleteGameState_ReturnsNotFound_WhenNotDeleted()
    {
        var id = Guid.NewGuid();
        var controller = new GameStatesController(new FakeGameStateService { Deleted = false });

        var result = await controller.DeleteGameState(id, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<MessageWithIdResponse>(notFound.Value);
        Assert.Equal(id, response.Id);
    }

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class FakeGameStateService : IGameStateService
    {
        public JsonElement? GameState { get; init; }

        public JsonElement? Player { get; init; }

        public bool Deleted { get; init; }

        public CreateGameStateResult Created { get; init; } = new()
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            SaveName = "Save"
        };

        public Task<List<JsonElement>> GetGameStatesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(GameState.HasValue ? [GameState.Value] : new List<JsonElement>());
        }

        public Task<JsonElement?> GetGameStateAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(GameState);
        }

        public Task<JsonElement?> GetGameStatePlayerAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Player);
        }

        public Task<CreateGameStateResult> CreateGameStateAsync(string? name, CancellationToken cancellationToken)
        {
            return Task.FromResult(Created);
        }

        public Task<bool> DeleteGameStateAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Deleted);
        }
    }
}
