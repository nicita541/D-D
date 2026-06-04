using System.Text.Json;
using backend.Modules.Characters.Contracts;
using backend.Modules.Characters.Domain;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Mechanics.Contracts;
using backend.Modules.Memory.Contracts;
using backend.Infrastructure.Ai;
using backend.Modules.Combat.Application;
using backend.Modules.Combat.Contracts;
using backend.Modules.Combat.Domain;
using backend.Modules.Combat.Infrastructure;
using backend.Modules.Ai.Infrastructure;
using backend.Modules.Campaigns.Infrastructure;
using backend.Modules.Changes.Infrastructure;
using backend.Modules.Characters.Infrastructure;
using backend.Modules.GameStates.Infrastructure;
using backend.Modules.Mechanics.Infrastructure;
using backend.Modules.Memory.Infrastructure;
using backend.Modules.Party.Infrastructure;
using backend.Modules.Play.Infrastructure;
using backend.Modules.Story.Infrastructure;
using backend.Modules.Travel.Infrastructure;
using backend.Modules.Turns.Infrastructure;
using backend.Modules.World.Infrastructure;
using backend.Modules.Ai.Application;
using backend.Modules.Campaigns.Application;
using backend.Modules.Changes.Application;
using backend.Modules.Characters.Application;
using backend.Modules.GameStates.Application;
using backend.Modules.Mechanics.Application;
using backend.Modules.Memory.Application;
using backend.Modules.Party.Application;
using backend.Modules.Play.Application;
using backend.Modules.Story.Application;
using backend.Modules.Travel.Application;
using backend.Modules.Turns.Application;
using backend.Modules.World.Application;

namespace Tests.Services;

public sealed class Pass2MechanicsTests
{
    [Fact]
    public void CampaignMemoryMergeHelper_MergesSummarySceneAndDedupsArrays()
    {
        var current = JsonDocument.Parse("""
            {
              "резюме": "Старое резюме",
              "текущаяСцена": { "место": "лес" },
              "важныеФакты": [{ "название": "Следы у дороги" }],
              "открытыеЛинии": [],
              "закрытыеЛинии": [],
              "известныеNpc": [],
              "известныеЛокации": [],
              "секретыМастера": []
            }
            """).RootElement.Clone();
        var payload = JsonDocument.Parse("""
            {
              "добавитьКРезюме": "Новое событие",
              "текущаяСцена": { "настроение": "тревожное" },
              "важныеФактыДобавить": [
                { "название": "Следы у дороги" },
                { "название": "У колодца знак культа" }
              ]
            }
            """).RootElement.Clone();

        var merged = CampaignMemoryMergeHelper.Merge(current, payload);

        Assert.Contains("Старое резюме", merged.Summary);
        Assert.Contains("Новое событие", merged.Summary);
        Assert.Equal("лес", merged.CurrentScene.GetProperty("место").GetString());
        Assert.Equal("тревожное", merged.CurrentScene.GetProperty("настроение").GetString());
        Assert.Equal(2, merged.ImportantFacts.GetArrayLength());
        Assert.Contains("summary", merged.UpdatedFields);
    }

    [Fact]
    public void CampaignMemoryMergeHelper_RejectsEmptyPayload()
    {
        var current = JsonDocument.Parse("""{ "резюме": "", "текущаяСцена": {}, "важныеФакты": [] }""").RootElement.Clone();
        var payload = JsonDocument.Parse("""{}""").RootElement.Clone();

        Assert.Throws<RpgValidationException>(() => CampaignMemoryMergeHelper.Merge(current, payload));
    }

    [Fact]
    public async Task CampaignMemoryService_SummarizeInvalidAi_ReturnsServiceUnavailableWithoutApply()
    {
        var repository = new FakeCampaignMemoryRepository
        {
            RecentEntries = new[]
            {
                JsonSerializer.SerializeToElement(new { turnNumber = 1, type = "master", text = "Запись", important = false })
            }
        };
        var service = new CampaignMemoryService(repository, new FakeOllamaClient("not-json"));

        var result = await service.SummarizeMemoryAsync(Guid.NewGuid(), Guid.NewGuid(), new CampaignMemorySummarizeRequest(), CancellationToken.None);

        Assert.Equal(RpgResultStatus.ServiceUnavailable, result.Status);
        Assert.False(repository.ApplyCalled);
    }

    [Fact]
    public async Task MechanicRequestService_ResolveAbilityCheckRequiresCharacterId()
    {
        var service = new MechanicRequestService(
            new FakeMechanicRequestRepository(JsonDocument.Parse("""{ "тип": "ability_check", "характеристика": "ловкость", "сложность": 12 }""").RootElement.Clone()),
            new FakeAbilityCheckService());

        var result = await service.ResolveAbilityCheckAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new MechanicRequestResolveAbilityCheckRequest(), CancellationToken.None);

        Assert.Equal(RpgResultStatus.BadRequest, result.Status);
    }

    [Fact]
    public async Task MechanicRequestService_ResolveAbilityCheckMarksRequestResolved()
    {
        var characterId = Guid.NewGuid();
        var repository = new FakeMechanicRequestRepository(JsonDocument.Parse("""{ "тип": "ability_check", "характеристика": "сила", "сложность": 10 }""").RootElement.Clone());
        var service = new MechanicRequestService(repository, new FakeAbilityCheckService());

        var result = await service.ResolveAbilityCheckAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new MechanicRequestResolveAbilityCheckRequest { CharacterId = characterId },
            CancellationToken.None);

        Assert.Equal(RpgResultStatus.Ok, result.Status);
        Assert.True(repository.Resolved);
    }

    [Fact]
    public void CombatRules_DamageClampsHpAndAddsDefeatedOnce()
    {
        var hp = CombatRules.ApplyDamage(3, 10);
        var conditions = CombatRules.AddConditionOnce(new[] { "повержен" }, CombatRules.DefeatedCondition);

        Assert.Equal(0, hp);
        Assert.Single(conditions);
        Assert.Equal("повержен", conditions[0]);
    }

    [Fact]
    public void CombatRules_HealingClampsAtMax()
    {
        Assert.Equal(12, CombatRules.ApplyHealing(10, 12, 20));
    }

    [Fact]
    public void ProgressionRules_ReturnExpectedMvpThresholds()
    {
        Assert.Equal(0, ProgressionRules.GetLevelThreshold(1));
        Assert.Equal(300, ProgressionRules.GetLevelThreshold(2));
        Assert.Equal(900, ProgressionRules.GetLevelThreshold(3));
        Assert.Equal(2700, ProgressionRules.GetLevelThreshold(4));
        Assert.Equal(6500, ProgressionRules.GetLevelThreshold(5));
    }

    [Fact]
    public void ProgressionRules_CanLevelUpUsesNextThreshold()
    {
        Assert.False(ProgressionRules.CanLevelUp(1, 299));
        Assert.True(ProgressionRules.CanLevelUp(1, 300));
        Assert.True(ProgressionRules.CanLevelUp(2, 900));
        Assert.False(ProgressionRules.CanLevelUp(5, 999999));
    }

    [Fact]
    public void ProgressionRules_ProficiencyBonusUsesMvpBands()
    {
        Assert.Equal(2, ProgressionRules.GetProficiencyBonus(1));
        Assert.Equal(2, ProgressionRules.GetProficiencyBonus(4));
        Assert.Equal(3, ProgressionRules.GetProficiencyBonus(5));
    }

    [Fact]
    public async Task CharacterProgressionService_RejectsInvalidExperience()
    {
        var service = new CharacterProgressionService(new FakeCharacterProgressionRepository());

        var result = await service.AddExperienceAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new AddExperienceRequest { Experience = 0 }, CancellationToken.None);

        Assert.Equal(RpgResultStatus.BadRequest, result.Status);
    }

    [Fact]
    public async Task CharacterProgressionService_AddExperienceReturnsCanLevelUp()
    {
        var service = new CharacterProgressionService(new FakeCharacterProgressionRepository());

        var result = await service.AddExperienceAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new AddExperienceRequest { Experience = 50 }, CancellationToken.None);

        Assert.Equal(RpgResultStatus.Ok, result.Status);
        Assert.True(result.Value.GetProperty("canLevelUp").GetBoolean());
    }

    [Fact]
    public async Task CharacterProgressionService_GetProgressionReturnsCurrentState()
    {
        var service = new CharacterProgressionService(new FakeCharacterProgressionRepository());

        var result = await service.GetProgressionAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(RpgResultStatus.Ok, result.Status);
        Assert.True(result.Value.GetProperty("levelUpAvailable").GetBoolean());
    }

    [Fact]
    public async Task CharacterProgressionService_RejectsTooLargeHpBonus()
    {
        var service = new CharacterProgressionService(new FakeCharacterProgressionRepository());

        var result = await service.LevelUpAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new LevelUpRequest { HpMaxAdd = 51 }, CancellationToken.None);

        Assert.Equal(RpgResultStatus.BadRequest, result.Status);
    }

    private sealed class FakeCampaignMemoryRepository : ICampaignMemoryRepository
    {
        public IReadOnlyList<JsonElement>? RecentEntries { get; init; }

        public bool ApplyCalled { get; private set; }

        public Task<JsonElement?> GetMemoryAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new { резюме = "" }));

        public Task<JsonElement?> UpdateMemoryAsync(Guid accountId, Guid gameStateId, CampaignMemoryRequest request, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new { резюме = request.ResolvedSummary ?? "" }));

        public Task<JsonElement?> ApplyMemoryPatchAsync(Guid accountId, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
        {
            ApplyCalled = true;
            return Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new { резюме = "merged" }));
        }

        public Task<IReadOnlyList<JsonElement>?> GetRecentLogEntriesAsync(Guid accountId, Guid gameStateId, int limit, CancellationToken cancellationToken)
            => Task.FromResult(RecentEntries);

        public Task EnsureMemoryAsync(Guid gameStateId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeOllamaClient : IOllamaClient
    {
        private readonly string _response;

        public FakeOllamaClient(string response)
        {
            _response = response;
        }

        public Task<OllamaGenerateResult> GenerateAsync(string prompt, CancellationToken cancellationToken)
            => Task.FromResult(new OllamaGenerateResult("fake", _response, "{}"));
    }

    private sealed class FakeMechanicRequestRepository : IMechanicRequestRepository
    {
        private readonly JsonElement _payload;

        public FakeMechanicRequestRepository(JsonElement payload)
        {
            _payload = payload;
        }

        public bool Resolved { get; private set; }

        public Task<IReadOnlyList<JsonElement>?> GetRequestsAsync(Guid accountId, Guid gameStateId, string? status, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<JsonElement>?>(Array.Empty<JsonElement>());

        public Task<MechanicRequestRecord?> GetPendingAbilityCheckRequestAsync(Guid accountId, Guid gameStateId, Guid requestId, CancellationToken cancellationToken)
            => Task.FromResult<MechanicRequestRecord?>(new MechanicRequestRecord(
                requestId,
                gameStateId,
                null,
                null,
                "ability_check",
                _payload,
                "pending",
                JsonSerializer.SerializeToElement(new { }),
                DateTimeOffset.UtcNow,
                null));

        public Task<JsonElement?> ResolveAsync(Guid accountId, Guid gameStateId, Guid requestId, JsonElement result, CancellationToken cancellationToken)
        {
            Resolved = true;
            return Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new { id = requestId, status = "resolved", result }));
        }
    }

    private sealed class FakeAbilityCheckService : IAbilityCheckService
    {
        public Task<RpgResult<JsonElement>> CreateAbilityCheckAsync(Guid accountId, Guid gameStateId, AbilityCheckRequest request, CancellationToken cancellationToken)
            => Task.FromResult(RpgResult<JsonElement>.Ok(JsonSerializer.SerializeToElement(new { success = true, total = 15 })));

        public Task<RpgResult<IReadOnlyList<JsonElement>>> GetChecksAsync(Guid accountId, Guid gameStateId, int limit, CancellationToken cancellationToken)
            => Task.FromResult(RpgResult<IReadOnlyList<JsonElement>>.Ok(Array.Empty<JsonElement>()));
    }

    private sealed class FakeCharacterProgressionRepository : ICharacterProgressionRepository
    {
        public Task<JsonElement?> GetProgressionAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new
            {
                characterId,
                level = 1,
                experience = 350,
                experienceToNextLevel = 300,
                levelUpAvailable = true,
                proficiencyBonus = 2,
                nextLevelThreshold = 300,
                hpMax = 12,
                hpCurrent = 12
            }));

        public Task<JsonElement?> AddExperienceAsync(Guid accountId, Guid gameStateId, Guid characterId, int experience, string reason, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new
            {
                characterId,
                experience = 350,
                level = 1,
                experienceToNextLevel = 300,
                levelUpAvailable = true,
                canLevelUp = true
            }));

        public Task<JsonElement?> LevelUpAsync(Guid accountId, Guid gameStateId, Guid characterId, int? requestedNewLevel, int? hpMaxAdd, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new { characterId, oldLevel = 1, newLevel = 2, hpMax = 17, hpCurrent = 17 }));
    }
}
