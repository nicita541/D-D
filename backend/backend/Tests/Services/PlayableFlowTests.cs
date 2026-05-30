using System.Text.Json;
using backend.Contracts.Rpg.Characters;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Mechanics;
using backend.Contracts.Rpg.Play;
using backend.Contracts.Rpg.Turns;
using backend.Repositories.Rpg;
using backend.Services.Rpg;

namespace Tests.Services;

public sealed class PlayableFlowTests
{
    [Fact]
    public void PlaySceneExtractor_ReadsSceneFromMemory()
    {
        var locationId = Guid.NewGuid();
        var npcId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var memory = JsonSerializer.SerializeToElement(new
        {
            currentScene = new
            {
                title = "Scene",
                summary = "Summary",
                currentObjective = "Objective",
                currentThreat = "Threat",
                locationId,
                activeNpcIds = new[] { npcId },
                activeQuestIds = new[] { questId },
                updatedAt = DateTimeOffset.UtcNow
            }
        });

        var scene = PlaySceneExtractor.Extract(memory);

        Assert.NotNull(scene);
        Assert.Equal("Scene", scene!.Title);
        Assert.Equal(locationId, scene.LocationId);
        Assert.Equal(npcId, scene.ActiveNpcIds.Single());
        Assert.Equal(questId, scene.ActiveQuestIds.Single());
    }

    [Fact]
    public void PlaySceneExtractor_ReturnsNullForMalformedMemory()
    {
        var memory = JsonSerializer.SerializeToElement(new { currentScene = "bad" });

        Assert.Null(PlaySceneExtractor.Extract(memory));
    }

    [Theory]
    [InlineData("добавить_запись_журнала", "add_journal_entry", GameChangeOperationClass.Safe, true, true)]
    [InlineData("обновить_память", "update_memory", GameChangeOperationClass.Safe, true, true)]
    [InlineData("add_journal_entry", "add_journal_entry", GameChangeOperationClass.Safe, true, true)]
    [InlineData("move_party_to_location", "move_party_to_location", GameChangeOperationClass.Dangerous, true, false)]
    [InlineData("start_combat", "start_combat", GameChangeOperationClass.Dangerous, false, false)]
    [InlineData("spawn_monster", "spawn_monster", GameChangeOperationClass.Unsupported, false, false)]
    [InlineData("неизвестно", "неизвестно", GameChangeOperationClass.Unknown, false, false)]
    public void GameChangeOperationPolicy_NormalizesAndClassifies(
        string input,
        string expectedCanonical,
        GameChangeOperationClass expectedClass,
        bool expectedSupported,
        bool expectedSafe)
    {
        var descriptor = GameChangeOperationPolicy.Describe(input);

        Assert.Equal(expectedCanonical, descriptor.CanonicalOperation);
        Assert.Equal(expectedClass, descriptor.Class);
        Assert.Equal(expectedSupported, descriptor.IsSupported);
        Assert.Equal(expectedSafe, descriptor.IsSafeAutoApply);
    }

    [Fact]
    public async Task PlayAct_ReturnsAwaitingRollWithoutCreatingTurn_WhenPendingMechanicRequestExists()
    {
        var turnService = new FakeTurnService();
        var service = new PlayOrchestratorService(
            new FakeGameStateService(),
            new FakeCharacterService(),
            turnService,
            new FakeGameChangeService(),
            new FakeMechanicRequestService(hasPendingRequest: true),
            new FakePlayStateService("awaiting_roll"));

        var result = await service.ActAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PlayActRequest { Message = "I continue." },
            CancellationToken.None);

        Assert.Equal(RpgResultStatus.Ok, result.Status);
        Assert.Equal("awaiting_roll", result.Value!.Mode);
        Assert.Equal(0, turnService.CreateTurnCalls);
    }

    [Fact]
    public async Task PlayAct_MapsTurnConflictToConflict()
    {
        var turnService = new FakeTurnService
        {
            CreateTurnResult = RpgResult<JsonElement>.Conflict("В этой игре уже обрабатывается ход.")
        };
        var service = new PlayOrchestratorService(
            new FakeGameStateService(),
            new FakeCharacterService(),
            turnService,
            new FakeGameChangeService(),
            new FakeMechanicRequestService(hasPendingRequest: false),
            new FakePlayStateService("narration"));

        var result = await service.ActAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PlayActRequest { Message = "I continue." },
            CancellationToken.None);

        Assert.Equal(RpgResultStatus.Conflict, result.Status);
        Assert.Equal(1, turnService.CreateTurnCalls);
    }

    [Fact]
    public async Task ApplySafeChanges_SkipsDangerousUnsupportedAndUnknownOperations()
    {
        var changes = new FakeGameChangeService
        {
            PendingChanges =
            [
                JsonSerializer.SerializeToElement(new { id = Guid.NewGuid(), operation = "start_combat" }),
                JsonSerializer.SerializeToElement(new { id = Guid.NewGuid(), operation = "spawn_monster" }),
                JsonSerializer.SerializeToElement(new { id = Guid.NewGuid(), operation = "unknown_operation" })
            ]
        };
        var service = new PlayOrchestratorService(
            new FakeGameStateService(),
            new FakeCharacterService(),
            new FakeTurnService(),
            changes,
            new FakeMechanicRequestService(hasPendingRequest: false),
            new FakePlayStateService("narration"));

        var result = await service.ApplySafeChangesAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(RpgResultStatus.Ok, result.Status);
        Assert.Empty(result.Value!.Applied);
        Assert.Equal(3, result.Value.Skipped.Count);
        Assert.Contains(result.Value.Skipped, item => item.Operation == "start_combat" && item.Reason == "dangerous operation");
        Assert.Contains(result.Value.Skipped, item => item.Operation == "spawn_monster" && item.Reason == "unsupported operation");
        Assert.Contains(result.Value.Skipped, item => item.Operation == "unknown_operation" && item.Reason == "unknown operation");
        Assert.Equal(0, changes.ApplyCalls);
    }

    [Fact]
    public async Task ApplySafeChanges_AppliesSupportedSafeOperation()
    {
        var changeId = Guid.NewGuid();
        var changes = new FakeGameChangeService
        {
            PendingChanges =
            [
                JsonSerializer.SerializeToElement(new { id = changeId, operation = "добавить_запись_журнала" })
            ]
        };
        var service = new PlayOrchestratorService(
            new FakeGameStateService(),
            new FakeCharacterService(),
            new FakeTurnService(),
            changes,
            new FakeMechanicRequestService(hasPendingRequest: false),
            new FakePlayStateService("narration"));

        var result = await service.ApplySafeChangesAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(RpgResultStatus.Ok, result.Status);
        Assert.Single(result.Value!.Applied);
        Assert.Empty(result.Value.Skipped);
        Assert.Equal(1, changes.ApplyCalls);
        Assert.Equal(changeId, changes.AppliedChangeIds.Single());
    }

    [Fact]
    public async Task PlayBootstrapService_RequiresCharacters()
    {
        var service = new PlayBootstrapService(new FakePlayBootstrapRepository(PlayBootstrapRepositoryStatus.NoCharacters));

        var result = await service.BootstrapAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(RpgResultStatus.BadRequest, result.Status);
    }

    [Fact]
    public async Task PlayBootstrapService_ReturnsConflictWhenAlreadyBootstrapped()
    {
        var service = new PlayBootstrapService(new FakePlayBootstrapRepository(PlayBootstrapRepositoryStatus.AlreadyBootstrapped));

        var result = await service.BootstrapAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(RpgResultStatus.Conflict, result.Status);
    }

    private sealed class FakePlayBootstrapRepository : IPlayBootstrapRepository
    {
        private readonly PlayBootstrapRepositoryStatus _status;

        public FakePlayBootstrapRepository(PlayBootstrapRepositoryStatus status)
        {
            _status = status;
        }

        public Task<PlayBootstrapRepositoryResult> BootstrapAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        {
            var response = _status == PlayBootstrapRepositoryStatus.Ok
                ? new PlayBootstrapResponse(
                    true,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new PlaySceneDto("title", "summary", "objective", null, null, Array.Empty<Guid>(), Array.Empty<Guid>(), DateTimeOffset.UtcNow))
                : null;

            return Task.FromResult(new PlayBootstrapRepositoryResult(_status, response));
        }
    }

    private sealed class FakeGameStateService : IGameStateService
    {
        public Task<IReadOnlyList<JsonElement>> GetGameStatesAsync(Guid accountId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<JsonElement>>(Array.Empty<JsonElement>());

        public Task<JsonElement?> GetGameStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new { id = gameStateId }));

        public Task<Guid> CreateGameStateAsync(Guid accountId, string? name, CancellationToken cancellationToken)
            => Task.FromResult(Guid.NewGuid());

        public Task<bool> DeleteGameStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }

    private sealed class FakeCharacterService : ICharacterService
    {
        public Task<IReadOnlyList<JsonElement>> GetCharactersAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<JsonElement>>(new[] { JsonSerializer.SerializeToElement(new { id = Guid.NewGuid() }) });

        public Task<JsonElement?> GetCharacterAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new { id = characterId }));

        public Task<Guid?> CreateCharacterAsync(Guid accountId, Guid gameStateId, CreateCharacterRequest request, CancellationToken cancellationToken)
            => Task.FromResult<Guid?>(Guid.NewGuid());

        public Task<bool> UpdateCharacterAsync(Guid accountId, Guid gameStateId, Guid characterId, UpdateCharacterRequest request, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<bool> DeleteCharacterAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }

    private sealed class FakeTurnService : ITurnService
    {
        public int CreateTurnCalls { get; private set; }

        public RpgResult<JsonElement> CreateTurnResult { get; set; } =
            RpgResult<JsonElement>.Ok(JsonSerializer.SerializeToElement(new { masterAnswer = "ok" }));

        public Task<RpgResult<JsonElement>> CreateTurnAsync(Guid accountId, Guid gameStateId, CreateTurnRequest request, CancellationToken cancellationToken)
        {
            CreateTurnCalls++;
            return Task.FromResult(CreateTurnResult);
        }

        public Task<RpgResult<IReadOnlyList<JsonElement>>> GetTurnsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
            => Task.FromResult(RpgResult<IReadOnlyList<JsonElement>>.Ok(Array.Empty<JsonElement>()));

        public Task<RpgResult<JsonElement>> GetTurnAsync(Guid accountId, Guid gameStateId, Guid turnId, CancellationToken cancellationToken)
            => Task.FromResult(RpgResult<JsonElement>.Ok(JsonSerializer.SerializeToElement(new { id = turnId })));
    }

    private sealed class FakeGameChangeService : IGameChangeService
    {
        public IReadOnlyList<JsonElement> PendingChanges { get; init; } = Array.Empty<JsonElement>();

        public int ApplyCalls { get; private set; }

        public List<Guid> AppliedChangeIds { get; } = new();

        public Task<RpgResult<IReadOnlyList<JsonElement>>> GetChangesAsync(Guid accountId, Guid gameStateId, string? status, CancellationToken cancellationToken)
            => Task.FromResult(RpgResult<IReadOnlyList<JsonElement>>.Ok(PendingChanges));

        public Task<RpgResult<JsonElement>> GetChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, CancellationToken cancellationToken)
            => Task.FromResult(RpgResult<JsonElement>.Ok(JsonSerializer.SerializeToElement(new { id = changeId })));

        public Task<RpgResult<JsonElement>> ApplyChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, CancellationToken cancellationToken)
        {
            ApplyCalls++;
            AppliedChangeIds.Add(changeId);
            return Task.FromResult(RpgResult<JsonElement>.Ok(JsonSerializer.SerializeToElement(new { id = changeId, status = "applied" })));
        }

        public Task<RpgResult<JsonElement>> RejectChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, string reason, CancellationToken cancellationToken)
            => Task.FromResult(RpgResult<JsonElement>.Ok(JsonSerializer.SerializeToElement(new { id = changeId, status = "rejected" })));
    }

    private sealed class FakeMechanicRequestService : IMechanicRequestService
    {
        private readonly bool _hasPendingRequest;

        public FakeMechanicRequestService(bool hasPendingRequest)
        {
            _hasPendingRequest = hasPendingRequest;
        }

        public Task<RpgResult<IReadOnlyList<JsonElement>>> GetRequestsAsync(Guid accountId, Guid gameStateId, string? status, CancellationToken cancellationToken)
        {
            IReadOnlyList<JsonElement> requests = _hasPendingRequest
                ? new[] { JsonSerializer.SerializeToElement(new { id = Guid.NewGuid(), status = "pending" }) }
                : Array.Empty<JsonElement>();

            return Task.FromResult(RpgResult<IReadOnlyList<JsonElement>>.Ok(requests));
        }

        public Task<RpgResult<JsonElement>> ResolveAbilityCheckAsync(Guid accountId, Guid gameStateId, Guid requestId, MechanicRequestResolveAbilityCheckRequest request, CancellationToken cancellationToken)
            => Task.FromResult(RpgResult<JsonElement>.Ok(JsonSerializer.SerializeToElement(new { id = requestId, status = "resolved" })));
    }

    private sealed class FakePlayStateService : IPlayStateService
    {
        private readonly string _mode;

        public FakePlayStateService(string mode)
        {
            _mode = mode;
        }

        public Task<RpgResult<PlayStateResponse>> BuildAsync(Guid accountId, Guid gameStateId, PlayStateBuildRequest request, CancellationToken cancellationToken)
            => Task.FromResult(RpgResult<PlayStateResponse>.Ok(new PlayStateResponse(
                _mode,
                request.MasterAnswer,
                JsonSerializer.SerializeToElement(new { id = gameStateId }),
                null,
                Array.Empty<JsonElement>(),
                Array.Empty<JsonElement>(),
                Array.Empty<JsonElement>(),
                _mode == "awaiting_roll"
                    ? new[] { JsonSerializer.SerializeToElement(new { id = Guid.NewGuid(), status = "pending" }) }
                    : Array.Empty<JsonElement>(),
                null,
                Array.Empty<JsonElement>(),
                request.ChangeSummary?.Applied ?? Array.Empty<PlayChangeApplicationItem>(),
                request.ChangeSummary?.Skipped ?? Array.Empty<PlayChangeApplicationItem>(),
                request.ChangeSummary?.Failed ?? Array.Empty<PlayChangeApplicationItem>(),
                null,
                DateTimeOffset.UtcNow)));
    }
}
