using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Changes.Application;
using backend.Modules.Changes.Contracts;
using backend.Modules.Changes.Domain;
using backend.Modules.Changes.Infrastructure;
using Npgsql;

namespace Tests.Services;

public sealed class GameChangeServiceTests
{
    [Fact]
    public async Task GameChangeDispatcher_UnknownOperationReturnsControlledFailure()
    {
        var dispatcher = new GameChangeDispatcher(new[] { new FakeChangeHandler("add_journal_entry") });
        var context = CreateFakeContext();

        var ex = await Assert.ThrowsAsync<RpgValidationException>(() =>
            dispatcher.DispatchAsync(context, "not_real", JsonSerializer.SerializeToElement(new { }), CancellationToken.None));

        Assert.Contains("Unknown change operation", ex.Message);
    }

    [Fact]
    public async Task GameChangeDispatcher_UnsupportedOperationReturnsControlledFailure()
    {
        var dispatcher = new GameChangeDispatcher(new[] { new FakeChangeHandler("add_journal_entry") });
        var context = CreateFakeContext();

        var ex = await Assert.ThrowsAsync<RpgValidationException>(() =>
            dispatcher.DispatchAsync(context, "transfer_currency", JsonSerializer.SerializeToElement(new { }), CancellationToken.None));

        Assert.Contains("Unsupported change operation", ex.Message);
    }

    [Fact]
    public void GameChangeDispatcher_DuplicateHandlerOperationThrows()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new GameChangeDispatcher(new[]
        {
            new FakeChangeHandler("add_journal_entry"),
            new FakeChangeHandler("add_journal_entry")
        }));

        Assert.Contains("Duplicate change handler", ex.Message);
    }

    [Fact]
    public async Task GameChangeDispatcher_SupportedPolicyWithoutHandlerReturnsControlledFailure()
    {
        var dispatcher = new GameChangeDispatcher(Array.Empty<IGameChangeHandler>());
        var context = CreateFakeContext();

        var ex = await Assert.ThrowsAsync<RpgValidationException>(() =>
            dispatcher.DispatchAsync(context, "add_journal_entry", JsonSerializer.SerializeToElement(new { }), CancellationToken.None));

        Assert.Contains("No change handler registered", ex.Message);
    }

    [Fact]
    public async Task GameChangeDispatcher_HandlerMarkedUnsupportedReturnsControlledFailure()
    {
        var dispatcher = new GameChangeDispatcher(new[] { new FakeChangeHandler("add_journal_entry", isSupported: false) });
        var context = CreateFakeContext();

        var ex = await Assert.ThrowsAsync<RpgValidationException>(() =>
            dispatcher.DispatchAsync(context, "add_journal_entry", JsonSerializer.SerializeToElement(new { }), CancellationToken.None));

        Assert.Contains("Unsupported change operation", ex.Message);
    }

    [Fact]
    public async Task GameChangeDispatcher_DispatchesSupportedHandler()
    {
        var handler = new FakeChangeHandler("add_journal_entry");
        var dispatcher = new GameChangeDispatcher(new[] { handler });
        var context = CreateFakeContext();

        var result = await dispatcher.DispatchAsync(
            context,
            "добавить_запись_журнала",
            JsonSerializer.SerializeToElement(new { text = "entry" }),
            CancellationToken.None);

        Assert.True(handler.Called);
        Assert.Equal("add_journal_entry", result.GetProperty("operation").GetString());
    }

    [Fact]
    public async Task ApplyChange_ReturnsBadRequest_WhenOperationIsUnknown()
    {
        var service = new GameChangeService(new UnknownOperationRepository());

        var result = await service.ApplyChangeAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(RpgResultStatus.BadRequest, result.Status);
    }

    private static GameChangeContext CreateFakeContext()
        => new(
            null!,
            null!,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            (operation, payload, cancellationToken) => Task.FromResult(JsonSerializer.SerializeToElement(new { operation })));

    private sealed class FakeChangeHandler : IGameChangeHandler
    {
        private readonly bool _isSupported;

        public FakeChangeHandler(string operation, bool isSupported = true)
        {
            Operation = operation;
            _isSupported = isSupported;
        }

        public string Operation { get; }

        public GameChangeOperationClass Class => GameChangeOperationClass.Safe;

        public bool IsSupported => _isSupported;

        public bool Called { get; private set; }

        public Task<JsonElement> ApplyAsync(GameChangeContext context, JsonElement payload, CancellationToken cancellationToken)
        {
            Called = true;
            return context.ApplyCanonicalOperationAsync(Operation, payload, cancellationToken);
        }
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
