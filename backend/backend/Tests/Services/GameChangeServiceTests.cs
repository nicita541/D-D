using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Repositories.Rpg;
using backend.Services.Rpg;

namespace Tests.Services;

public sealed class GameChangeServiceTests
{
    [Fact]
    public async Task ApplyChange_ReturnsBadRequest_WhenOperationIsUnknown()
    {
        var service = new GameChangeService(new UnknownOperationRepository());

        var result = await service.ApplyChangeAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(RpgResultStatus.BadRequest, result.Status);
    }

    private sealed class UnknownOperationRepository : IGameChangeRepository
    {
        public Task<IReadOnlyList<JsonElement>?> GetChangesAsync(Guid accountId, Guid gameStateId, string? status, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<JsonElement>?>(Array.Empty<JsonElement>());

        public Task<JsonElement?> GetChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(null);

        public Task<JsonElement?> ApplyChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, CancellationToken cancellationToken)
            => throw new RpgValidationException("Unsupported change operation.");

        public Task<JsonElement?> RejectChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, string reason, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new { status = "rejected" }));
    }
}
