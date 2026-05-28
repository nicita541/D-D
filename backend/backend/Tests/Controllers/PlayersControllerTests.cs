using backend.Contracts.Common;
using backend.Contracts.Players;
using backend.Controllers;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Controllers;

public class PlayersControllerTests
{
    [Fact]
    public async Task GetPlayer_ReturnsOk_WhenPlayerExists()
    {
        var id = Guid.NewGuid();
        var player = new Player { Id = id };
        var service = new FakePlayerService { Player = player };
        var controller = new PlayersController(service);

        var result = await controller.GetPlayer(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(player, ok.Value);
    }

    [Fact]
    public async Task GetPlayer_ReturnsNotFound_WhenPlayerDoesNotExist()
    {
        var id = Guid.NewGuid();
        var controller = new PlayersController(new FakePlayerService());

        var result = await controller.GetPlayer(id, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<MessageWithIdResponse>(notFound.Value);
        Assert.Equal(id, response.Id);
    }

    [Fact]
    public async Task CreatePlayer_ReturnsBadRequest_WhenBodyIsNull()
    {
        var controller = new PlayersController(new FakePlayerService());

        var result = await controller.CreatePlayer(null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<MessageResponse>(badRequest.Value);
    }

    [Fact]
    public async Task CreatePlayer_ReturnsBadRequest_WhenNameIsMissing()
    {
        var controller = new PlayersController(new FakePlayerService());

        var result = await controller.CreatePlayer(new Player(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<MessageResponse>(badRequest.Value);
    }

    [Fact]
    public async Task CreatePlayer_ReturnsCreated_WhenPlayerIsValid()
    {
        var id = Guid.NewGuid();
        var service = new FakePlayerService { CreatedId = id };
        var controller = new PlayersController(service);
        var player = new Player();
        player.Character.Name = "Test hero";

        var result = await controller.CreatePlayer(player, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<CreatePlayerResponse>(created.Value);
        Assert.Equal(nameof(PlayersController.GetPlayer), created.ActionName);
        Assert.Equal(id, response.Id);
        Assert.Same(player, service.CreatedPlayer);
    }

    private sealed class FakePlayerService : IPlayerService
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
}
