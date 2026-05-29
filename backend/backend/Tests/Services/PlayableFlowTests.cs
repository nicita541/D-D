using System.Text.Json;
using backend.Contracts.Rpg.Play;
using backend.Repositories.Rpg;
using backend.Services.Rpg;
using backend.Contracts.Rpg.Common;

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
    [InlineData("добавить_запись_журнала", "add_journal_entry", true)]
    [InlineData("обновить_память", "update_memory", true)]
    [InlineData("add_journal_entry", "add_journal_entry", true)]
    [InlineData("change_hp", "change_hp", false)]
    [InlineData("неизвестно", "неизвестно", false)]
    public void GameChangeOperationPolicy_NormalizesAndClassifies(string input, string expectedCanonical, bool expectedSafe)
    {
        var descriptor = GameChangeOperationPolicy.Describe(input);

        Assert.Equal(expectedCanonical, descriptor.CanonicalOperation);
        Assert.Equal(expectedSafe, descriptor.IsSafeAutoApply);
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
}
