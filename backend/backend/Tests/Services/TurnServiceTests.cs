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
    public async Task CreateTurn_ReturnsBadRequest_WhenMessageIsTooLong()
    {
        var repository = new FakeTurnRepository();
        var service = CreateService(repository, new QueueOllamaClient());

        var result = await service.CreateTurnAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new CreateTurnRequest { Message = new string('a', 4001) },
            CancellationToken.None);

        Assert.Equal(RpgResultStatus.BadRequest, result.Status);
        Assert.False(repository.CreateCalled);
    }

    [Fact]
    public async Task CreateTurn_ReturnsConflict_WhenPendingTurnAlreadyExists()
    {
        var repository = new FakeTurnRepository
        {
            CreationResult = PendingTurnCreationResult.Conflict()
        };
        var service = CreateService(repository, new QueueOllamaClient());

        var result = await service.CreateTurnAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateTurnRequest { Message = "go" }, CancellationToken.None);

        Assert.Equal(RpgResultStatus.Conflict, result.Status);
        Assert.False(repository.CompleteCalled);
        Assert.False(repository.FailCalled);
    }

    [Fact]
    public async Task CreateTurn_Completes_WhenAiReturnsValidJson()
    {
        var repository = new FakeTurnRepository();
        var service = CreateService(
            repository,
            new QueueOllamaClient(new OllamaGenerateResult("model", """{"master_answer":"Ответ мастера","changes":[]}""", """{"response":"ok"}""")));

        var result = await service.CreateTurnAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateTurnRequest { Message = "go" }, CancellationToken.None);

        Assert.Equal(RpgResultStatus.Ok, result.Status);
        Assert.True(repository.CompleteCalled);
        Assert.False(repository.FailCalled);
        Assert.Empty(repository.LastChanges!);
    }

    [Fact]
    public async Task CreateTurn_RetriesAndCompletes_WhenFirstAiJsonIsInvalid()
    {
        var repository = new FakeTurnRepository();
        var promptBuilder = new FakePromptBuilder();
        var service = CreateService(
            repository,
            new QueueOllamaClient(
                new OllamaGenerateResult("model", "plain text", """{"response":"plain text"}"""),
                new OllamaGenerateResult("model", """{"master_answer":"Исправленный ответ","changes":[{"operation":"добавить_запись_журнала","payload":{"тип":"master","текст":"Запись","важное":false}}]}""", """{"response":"fixed"}""")),
            promptBuilder);

        var result = await service.CreateTurnAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateTurnRequest { Message = "go" }, CancellationToken.None);

        Assert.Equal(RpgResultStatus.Ok, result.Status);
        Assert.True(repository.CompleteCalled);
        Assert.False(repository.FailCalled);
        Assert.True(promptBuilder.RepairCalled);
        Assert.Single(repository.LastChanges!);
    }

    [Fact]
    public async Task CreateTurn_FailsWithoutChanges_WhenRetryJsonIsInvalid()
    {
        var repository = new FakeTurnRepository();
        var service = CreateService(
            repository,
            new QueueOllamaClient(
                new OllamaGenerateResult("model", "plain text", """{"response":"plain text"}"""),
                new OllamaGenerateResult("model", "[]", """{"response":"[]"}""")));

        var result = await service.CreateTurnAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateTurnRequest { Message = "go" }, CancellationToken.None);

        Assert.Equal(RpgResultStatus.ServiceUnavailable, result.Status);
        Assert.True(repository.FailCalled);
        Assert.False(repository.CompleteCalled);
        Assert.Contains("невалидный JSON", repository.LastFailMessage);
    }

    [Fact]
    public async Task CreateTurn_FailsWithoutChanges_WhenAiOperationIsUnknown()
    {
        var repository = new FakeTurnRepository();
        var invalid = """{"master_answer":"Ответ","changes":[{"operation":"неизвестно","payload":{}}]}""";
        var service = CreateService(
            repository,
            new QueueOllamaClient(
                new OllamaGenerateResult("model", invalid, """{"response":"bad-op"}"""),
                new OllamaGenerateResult("model", invalid, """{"response":"bad-op-again"}""")));

        var result = await service.CreateTurnAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateTurnRequest { Message = "go" }, CancellationToken.None);

        Assert.Equal(RpgResultStatus.ServiceUnavailable, result.Status);
        Assert.True(repository.FailCalled);
        Assert.False(repository.CompleteCalled);
        Assert.Null(repository.LastChanges);
    }

    [Fact]
    public async Task CreateTurn_ReturnsServiceUnavailable_WhenOllamaFails()
    {
        var repository = new FakeTurnRepository();
        var service = CreateService(repository, new QueueOllamaClient(new OllamaClientException("unavailable")));

        var result = await service.CreateTurnAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateTurnRequest { Message = "go" }, CancellationToken.None);

        Assert.Equal(RpgResultStatus.ServiceUnavailable, result.Status);
        Assert.True(repository.FailCalled);
        Assert.False(repository.CompleteCalled);
    }

    private static TurnService CreateService(FakeTurnRepository repository, IOllamaClient ollama, IPromptBuilder? promptBuilder = null)
    {
        return new TurnService(
            repository,
            new FakeContextService(),
            promptBuilder ?? new FakePromptBuilder(),
            ollama);
    }

    private sealed class FakeTurnRepository : ITurnRepository
    {
        public PendingTurnCreationResult? CreationResult { get; init; }
        public bool CreateCalled { get; private set; }
        public bool CompleteCalled { get; private set; }
        public bool FailCalled { get; private set; }
        public IReadOnlyList<GameChangeProposal>? LastChanges { get; private set; }
        public string? LastFailMessage { get; private set; }

        public Task<PendingTurnCreationResult> CreatePendingTurnAsync(Guid accountId, Guid gameStateId, string playerMessage, CancellationToken cancellationToken)
        {
            CreateCalled = true;
            return Task.FromResult(CreationResult ?? PendingTurnCreationResult.Created(new PendingTurn(Guid.NewGuid(), gameStateId, accountId, 1, playerMessage)));
        }

        public Task<JsonElement?> CompleteTurnAsync(PendingTurn turn, string masterAnswer, string rawAiResponse, string aiModel, IReadOnlyList<GameChangeProposal> changes, CancellationToken cancellationToken)
        {
            CompleteCalled = true;
            LastChanges = changes;
            return Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new { status = "completed", masterAnswer, changesCount = changes.Count }));
        }

        public Task<JsonElement?> FailTurnAsync(PendingTurn turn, string errorMessage, string? rawAiResponse, string? aiModel, CancellationToken cancellationToken)
        {
            FailCalled = true;
            LastFailMessage = errorMessage;
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
            => Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new { gameStateId, персонажи = Array.Empty<object>() }));
    }

    private sealed class FakePromptBuilder : IPromptBuilder
    {
        public bool RepairCalled { get; private set; }

        public string BuildTurnPrompt(JsonElement context, string playerMessage) => playerMessage;

        public string BuildRepairPrompt(string invalidResponse, string validationError)
        {
            RepairCalled = true;
            return "repair";
        }
    }

    private sealed class QueueOllamaClient : IOllamaClient
    {
        private readonly Queue<object> _results;

        public QueueOllamaClient(params object[] results)
        {
            _results = new Queue<object>(results);
        }

        public Task<OllamaGenerateResult> GenerateAsync(string prompt, CancellationToken cancellationToken)
        {
            if (_results.Count == 0)
            {
                throw new OllamaClientException("No fake AI result configured.");
            }

            var result = _results.Dequeue();
            if (result is OllamaClientException exception)
            {
                throw exception;
            }

            return Task.FromResult((OllamaGenerateResult)result);
        }
    }
}
