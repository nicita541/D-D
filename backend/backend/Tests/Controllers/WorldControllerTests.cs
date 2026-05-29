using System.Text.Json;
using backend.Controllers.Rpg;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.World;
using backend.Infrastructure.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Controllers;

public sealed class WorldControllerTests
{
    [Fact]
    public async Task GetLocations_ReturnsOk_FromService()
    {
        var controller = new WorldController(new FakeWorldService(), new FakeCurrentUserService(Guid.NewGuid()));

        var result = await controller.GetLocations(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CreateMonster_ReturnsBadRequest_FromService()
    {
        var service = new FakeWorldService
        {
            GuidResult = RpgResult<Guid>.BadRequest("invalid")
        };
        var controller = new WorldController(service, new FakeCurrentUserService(Guid.NewGuid()));

        var result = await controller.CreateMonster(Guid.NewGuid(), JsonSerializer.SerializeToElement(new { }), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        private readonly Guid _accountId;

        public FakeCurrentUserService(Guid accountId)
        {
            _accountId = accountId;
        }

        public CurrentUser GetRequiredUser() => new(_accountId, "hero@example.com", "hero", "user");
    }

    private sealed class FakeWorldService : IWorldService
    {
        public RpgResult<IReadOnlyList<JsonElement>> ListResult { get; init; } = RpgResult<IReadOnlyList<JsonElement>>.Ok(Array.Empty<JsonElement>());
        public RpgResult<JsonElement> JsonResult { get; init; } = RpgResult<JsonElement>.Ok(JsonSerializer.SerializeToElement(new { ok = true }));
        public RpgResult<Guid> GuidResult { get; init; } = RpgResult<Guid>.Ok(Guid.NewGuid());
        public RpgResult<bool> BoolResult { get; init; } = RpgResult<bool>.Ok(true);

        public Task<RpgResult<IReadOnlyList<JsonElement>>> ListAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid? parentId, CancellationToken cancellationToken)
            => Task.FromResult(ListResult);

        public Task<RpgResult<JsonElement>> GetAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, CancellationToken cancellationToken)
            => Task.FromResult(JsonResult);

        public Task<RpgResult<Guid>> CreateAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid? parentId, JsonElement payload, CancellationToken cancellationToken)
            => Task.FromResult(GuidResult);

        public Task<RpgResult<bool>> UpdateAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, JsonElement payload, CancellationToken cancellationToken)
            => Task.FromResult(BoolResult);

        public Task<RpgResult<bool>> DeleteAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, CancellationToken cancellationToken)
            => Task.FromResult(BoolResult);
    }
}
