using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Turns;
using backend.Infrastructure.Ai;
using backend.Repositories.Rpg;
using backend.Services.Rpg;

namespace Tests.Services;

public sealed class TurnServiceTests
{
    [Fact]
    public async Task CreateTurn_ReturnsServiceUnavailable_WhenOllamaFails()
    {
        var repository = new FakeTurnRepository();
        var service = new TurnService(
            repository,
            new FakeContextService(),
            new FakePromptBuilder(),
            new FailingOllamaClient());

        var result = await service.CreateTurnAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateTurnRequest { Message = "go" }, CancellationToken.None);

        Assert.Equal(RpgResultStatus.ServiceUnavailable, result.Status);
        Assert.True(repository.FailCalled);
    }

    private sealed class FakeTurnRepository : ITurnRepository
    {
        public bool FailCalled { get; private set; }

        public Task<PendingTurn?> CreatePendingTurnAsync(Guid accountId, Guid gameStateId, string playerMessage, CancellationToken cancellationToken)
            => Task.FromResult<PendingTurn?>(new PendingTurn(Guid.NewGuid(), gameStateId, accountId, 1, playerMessage));

        public Task<JsonElement?> CompleteTurnAsync(PendingTurn turn, string masterAnswer, string rawAiResponse, string aiModel, IReadOnlyList<GameChangeProposal> changes, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new { status = "completed" }));

        public Task<JsonElement?> FailTurnAsync(PendingTurn turn, string errorMessage, string? rawAiResponse, string? aiModel, CancellationToken cancellationToken)
        {
            FailCalled = true;
            return Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new { status = "failed", errorMessage }));
        }

        public Task<IReadOnlyList<JsonElement>?> GetTurnsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<JsonElement>?>(Array.Empty<JsonElement>());

        public Task<JsonElement?> GetTurnAsync(Guid accountId, Guid gameStateId, Guid turnId, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new { id = turnId }));
    }

    private sealed class FakeContextService : IAiMasterContextService
    {
        public Task<JsonElement?> GetContextAsync(Guid accountId, Guid gameStateId, int recentEventsLimit, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new { gameStateId }));
    }

    private sealed class FakePromptBuilder : IPromptBuilder
    {
        public string BuildTurnPrompt(JsonElement context, string playerMessage) => playerMessage;
    }

    private sealed class FailingOllamaClient : IOllamaClient
    {
        public Task<OllamaGenerateResult> GenerateAsync(string prompt, CancellationToken cancellationToken)
            => throw new OllamaClientException("unavailable");
    }
}
