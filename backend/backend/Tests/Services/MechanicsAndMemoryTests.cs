using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Mechanics;
using backend.Contracts.Rpg.Memory;
using backend.Repositories.Rpg;
using backend.Services.Rpg;

namespace Tests.Services;

public sealed class MechanicsAndMemoryTests
{
    [Theory]
    [InlineData("d20", 1, 20, 0, "1d20")]
    [InlineData("1d20", 1, 20, 0, "1d20")]
    [InlineData("1d20+4", 1, 20, 4, "1d20+4")]
    [InlineData("1d20-1", 1, 20, -1, "1d20-1")]
    [InlineData("2d6", 2, 6, 0, "2d6")]
    [InlineData("1d8+2", 1, 8, 2, "1d8+2")]
    public void DiceRoller_ParsesValidFormula(string formula, int count, int sides, int modifier, string normalized)
    {
        var parsed = new DiceRoller().Parse(formula);

        Assert.Equal(count, parsed.DiceCount);
        Assert.Equal(sides, parsed.DiceSides);
        Assert.Equal(modifier, parsed.Modifier);
        Assert.Equal(normalized, parsed.Normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0d20")]
    [InlineData("101d6")]
    [InlineData("1d1001")]
    [InlineData("1d20+10001")]
    public void DiceRoller_RejectsInvalidFormula(string formula)
    {
        Assert.Throws<RpgValidationException>(() => new DiceRoller().Parse(formula));
    }

    [Fact]
    public void DiceRoller_RollResultStaysInRange()
    {
        var result = new DiceRoller().Roll("2d6+3");

        Assert.Equal(2, result.Rolls.Count);
        Assert.All(result.Rolls, roll => Assert.InRange(roll, 1, 6));
        Assert.InRange(result.Total, 5, 15);
    }

    [Theory]
    [InlineData("strength", "сила")]
    [InlineData("ловкость", "ловкость")]
    [InlineData("constitution", "телосложение")]
    [InlineData("intelligence", "интеллект")]
    [InlineData("wisdom", "мудрость")]
    [InlineData("charisma", "харизма")]
    public void AbilityRules_NormalizesAliases(string input, string expected)
    {
        Assert.Equal(expected, AbilityRules.NormalizeAbility(input));
    }

    [Theory]
    [InlineData(8, -1)]
    [InlineData(9, -1)]
    [InlineData(10, 0)]
    [InlineData(12, 1)]
    [InlineData(18, 4)]
    public void AbilityRules_CalculatesModifier(int score, int expected)
    {
        Assert.Equal(expected, AbilityRules.CalculateModifier(score));
    }

    [Fact]
    public async Task DiceRollService_ReturnsBadRequest_ForInvalidFormula()
    {
        var service = new DiceRollService(new FakeDiceRollRepository(), new DiceRoller());

        var result = await service.RollAsync(Guid.NewGuid(), Guid.NewGuid(), new RollDiceRequest { Formula = "bad" }, CancellationToken.None);

        Assert.Equal(RpgResultStatus.BadRequest, result.Status);
    }

    [Fact]
    public async Task DiceRollService_ReturnsNotFound_WhenRepositoryCannotFindOwnership()
    {
        var service = new DiceRollService(new FakeDiceRollRepository { ReturnNullOnCreate = true }, new DiceRoller());

        var result = await service.RollAsync(Guid.NewGuid(), Guid.NewGuid(), new RollDiceRequest { Formula = "1d20" }, CancellationToken.None);

        Assert.Equal(RpgResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task AbilityCheckService_ReturnsSuccess_WhenTotalMeetsDifficultyClass()
    {
        var service = new AbilityCheckService(new FakeAbilityCheckRepository { AbilityScore = 20 }, new FixedDiceRoller(15));

        var result = await service.CreateAbilityCheckAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new AbilityCheckRequest
            {
                CharacterId = Guid.NewGuid(),
                Ability = "strength",
                DifficultyClass = 18
            },
            CancellationToken.None);

        Assert.Equal(RpgResultStatus.Ok, result.Status);
        Assert.True(result.Value.GetProperty("success").GetBoolean());
        Assert.Equal(5, result.Value.GetProperty("modifier").GetInt32());
    }

    [Fact]
    public async Task AbilityCheckService_ReturnsFailure_WhenTotalIsBelowDifficultyClass()
    {
        var service = new AbilityCheckService(new FakeAbilityCheckRepository { AbilityScore = 10 }, new FixedDiceRoller(5));

        var result = await service.CreateAbilityCheckAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new AbilityCheckRequest
            {
                CharacterId = Guid.NewGuid(),
                Ability = "ловкость",
                DifficultyClass = 10
            },
            CancellationToken.None);

        Assert.Equal(RpgResultStatus.Ok, result.Status);
        Assert.False(result.Value.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task AbilityCheckService_ReturnsBadRequest_ForInvalidAbility()
    {
        var service = new AbilityCheckService(new FakeAbilityCheckRepository { AbilityScore = 10 }, new FixedDiceRoller(10));

        var result = await service.CreateAbilityCheckAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new AbilityCheckRequest
            {
                CharacterId = Guid.NewGuid(),
                Ability = "удача",
                DifficultyClass = 10
            },
            CancellationToken.None);

        Assert.Equal(RpgResultStatus.BadRequest, result.Status);
    }

    [Fact]
    public async Task CampaignMemoryService_ReturnsMemory_FromRepository()
    {
        var service = new CampaignMemoryService(new FakeCampaignMemoryRepository());

        var result = await service.GetMemoryAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(RpgResultStatus.Ok, result.Status);
        Assert.Equal("", result.Value.GetProperty("резюме").GetString());
    }

    [Fact]
    public async Task CampaignMemoryService_RejectsWrongJsonShape()
    {
        var service = new CampaignMemoryService(new FakeCampaignMemoryRepository());
        var request = new CampaignMemoryRequest
        {
            ImportantFacts = JsonSerializer.SerializeToElement(new { wrong = true })
        };

        var result = await service.UpdateMemoryAsync(Guid.NewGuid(), Guid.NewGuid(), request, CancellationToken.None);

        Assert.Equal(RpgResultStatus.BadRequest, result.Status);
    }

    [Fact]
    public async Task CampaignMemoryService_UpdatesMemory()
    {
        var service = new CampaignMemoryService(new FakeCampaignMemoryRepository());
        var request = new CampaignMemoryRequest
        {
            Summary = "Новая память",
            ImportantFacts = JsonSerializer.SerializeToElement(new[] { "факт" })
        };

        var result = await service.UpdateMemoryAsync(Guid.NewGuid(), Guid.NewGuid(), request, CancellationToken.None);

        Assert.Equal(RpgResultStatus.Ok, result.Status);
        Assert.Equal("Новая память", result.Value.GetProperty("резюме").GetString());
    }

    private sealed class FakeDiceRollRepository : IDiceRollRepository
    {
        public bool ReturnNullOnCreate { get; init; }

        public Task<JsonElement?> CreateRollAsync(Guid accountId, Guid gameStateId, Guid? characterId, string reason, DiceRollResult roll, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(ReturnNullOnCreate
                ? null
                : JsonSerializer.SerializeToElement(new { total = roll.Total, rolls = roll.Rolls }));

        public Task<IReadOnlyList<JsonElement>?> GetRollsAsync(Guid accountId, Guid gameStateId, int limit, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<JsonElement>?>(Array.Empty<JsonElement>());
    }

    private sealed class FakeAbilityCheckRepository : IAbilityCheckRepository
    {
        public int? AbilityScore { get; init; }

        public Task<int?> GetAbilityScoreAsync(Guid accountId, Guid gameStateId, Guid characterId, string normalizedAbility, CancellationToken cancellationToken)
            => Task.FromResult(AbilityScore);

        public Task<JsonElement?> CreateAbilityCheckAsync(Guid accountId, Guid gameStateId, Guid characterId, string ability, int abilityScore, int modifier, int difficultyClass, bool success, string reason, DiceRollResult roll, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(new { characterId, ability, abilityScore, modifier, difficultyClass, total = roll.Total, success }));

        public Task<IReadOnlyList<JsonElement>?> GetChecksAsync(Guid accountId, Guid gameStateId, int limit, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<JsonElement>?>(Array.Empty<JsonElement>());
    }

    private sealed class FakeCampaignMemoryRepository : ICampaignMemoryRepository
    {
        public Task<JsonElement?> GetMemoryAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(CreateMemory(""));

        public Task<JsonElement?> UpdateMemoryAsync(Guid accountId, Guid gameStateId, CampaignMemoryRequest request, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(CreateMemory(request.ResolvedSummary ?? ""));

        public Task<JsonElement?> ApplyMemoryPatchAsync(Guid accountId, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
            => Task.FromResult<JsonElement?>(CreateMemory("merged"));

        public Task<IReadOnlyList<JsonElement>?> GetRecentLogEntriesAsync(Guid accountId, Guid gameStateId, int limit, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<JsonElement>?>(Array.Empty<JsonElement>());

        public Task EnsureMemoryAsync(Guid gameStateId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        private static JsonElement CreateMemory(string summary)
            => JsonSerializer.SerializeToElement(new
            {
                резюме = summary,
                текущаяСцена = new { },
                важныеФакты = Array.Empty<string>()
            });
    }

    private sealed class FixedDiceRoller : IDiceRoller
    {
        private readonly int _baseRoll;

        public FixedDiceRoller(int baseRoll)
        {
            _baseRoll = baseRoll;
        }

        public DiceFormula Parse(string formula) => new DiceRoller().Parse(formula);

        public DiceRollResult Roll(string formula)
        {
            var parsed = Parse(formula);
            return Roll(parsed);
        }

        public DiceRollResult Roll(DiceFormula formula)
        {
            var total = _baseRoll + formula.Modifier;
            return new DiceRollResult(formula, new[] { _baseRoll }, total);
        }
    }
}
