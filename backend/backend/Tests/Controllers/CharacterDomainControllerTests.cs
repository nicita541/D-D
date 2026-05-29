using System.Text.Json;
using backend.Controllers.Rpg;
using backend.Contracts.Rpg.Characters;
using backend.Contracts.Rpg.Common;
using backend.Infrastructure.Auth;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Controllers;

public sealed class CharacterDomainControllerTests
{
    [Fact]
    public async Task GetConditions_ReturnsOk_FromService()
    {
        var service = new FakeCharacterDomainService
        {
            ListResult = RpgResult<IReadOnlyList<JsonElement>>.Ok(Array.Empty<JsonElement>())
        };
        var controller = new CharacterDomainController(service, new FakeCurrentUserService(Guid.NewGuid()));

        var result = await controller.GetConditions(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetConditions_ReturnsNotFound_FromService()
    {
        var service = new FakeCharacterDomainService
        {
            ListResult = RpgResult<IReadOnlyList<JsonElement>>.NotFound("missing")
        };
        var controller = new CharacterDomainController(service, new FakeCurrentUserService(Guid.NewGuid()));

        var result = await controller.GetConditions(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateAbility_ReturnsBadRequest_FromService()
    {
        var service = new FakeCharacterDomainService
        {
            GuidResult = RpgResult<Guid>.BadRequest("invalid")
        };
        var controller = new CharacterDomainController(service, new FakeCurrentUserService(Guid.NewGuid()));

        var result = await controller.CreateAbility(Guid.NewGuid(), Guid.NewGuid(), new AbilityRequest(), CancellationToken.None);

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

    private sealed class FakeCharacterDomainService : ICharacterDomainService
    {
        public RpgResult<IReadOnlyList<JsonElement>> ListResult { get; init; } = RpgResult<IReadOnlyList<JsonElement>>.Ok(Array.Empty<JsonElement>());
        public RpgResult<Guid> GuidResult { get; init; } = RpgResult<Guid>.Ok(Guid.NewGuid());
        public RpgResult<bool> BoolResult { get; init; } = RpgResult<bool>.Ok(true);
        public RpgResult<JsonElement> JsonResult { get; init; } = RpgResult<JsonElement>.Ok(JsonSerializer.SerializeToElement(new { ok = true }));

        public Task<RpgResult<IReadOnlyList<JsonElement>>> GetConditionsAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken) => Task.FromResult(ListResult);
        public Task<RpgResult<Guid>> CreateConditionAsync(Guid accountId, Guid gameStateId, Guid characterId, ConditionRequest request, CancellationToken cancellationToken) => Task.FromResult(GuidResult);
        public Task<RpgResult<bool>> UpdateConditionAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid conditionId, ConditionRequest request, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<bool>> DeleteConditionAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid conditionId, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<IReadOnlyList<JsonElement>>> GetLimitedResourcesAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken) => Task.FromResult(ListResult);
        public Task<RpgResult<Guid>> CreateLimitedResourceAsync(Guid accountId, Guid gameStateId, Guid characterId, LimitedResourceRequest request, CancellationToken cancellationToken) => Task.FromResult(GuidResult);
        public Task<RpgResult<bool>> UpdateLimitedResourceAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid resourceId, LimitedResourceRequest request, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<bool>> DeleteLimitedResourceAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid resourceId, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<IReadOnlyList<JsonElement>>> GetProficienciesAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken) => Task.FromResult(ListResult);
        public Task<RpgResult<Guid>> CreateProficiencyAsync(Guid accountId, Guid gameStateId, Guid characterId, ProficiencyRequest request, CancellationToken cancellationToken) => Task.FromResult(GuidResult);
        public Task<RpgResult<bool>> DeleteProficiencyAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid proficiencyId, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<IReadOnlyList<JsonElement>>> GetAbilitiesAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken) => Task.FromResult(ListResult);
        public Task<RpgResult<Guid>> CreateAbilityAsync(Guid accountId, Guid gameStateId, Guid characterId, AbilityRequest request, CancellationToken cancellationToken) => Task.FromResult(GuidResult);
        public Task<RpgResult<bool>> UpdateAbilityAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid abilityId, AbilityRequest request, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<bool>> DeleteAbilityAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid abilityId, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<IReadOnlyList<JsonElement>>> GetInventoryAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken) => Task.FromResult(ListResult);
        public Task<RpgResult<Guid>> CreateInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, InventoryItemRequest request, CancellationToken cancellationToken) => Task.FromResult(GuidResult);
        public Task<RpgResult<bool>> UpdateInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, InventoryItemRequest request, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<bool>> DeleteInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<JsonElement>> GetEquipmentAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken) => Task.FromResult(JsonResult);
        public Task<RpgResult<bool>> UpdateEquipmentAsync(Guid accountId, Guid gameStateId, Guid characterId, EquipmentRequest request, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<IReadOnlyList<JsonElement>>> GetAttacksAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken) => Task.FromResult(ListResult);
        public Task<RpgResult<Guid>> CreateAttackAsync(Guid accountId, Guid gameStateId, Guid characterId, AttackRequest request, CancellationToken cancellationToken) => Task.FromResult(GuidResult);
        public Task<RpgResult<bool>> UpdateAttackAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid attackId, AttackRequest request, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<bool>> DeleteAttackAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid attackId, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<JsonElement>> GetNeedsAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken) => Task.FromResult(JsonResult);
        public Task<RpgResult<bool>> UpdateNeedsAsync(Guid accountId, Guid gameStateId, Guid characterId, NeedsRequest request, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<bool>> UpdateProgressionAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterProgressionRequest request, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<bool>> UpdateResourcesAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterResourcesRequest request, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<bool>> UpdateAttributesAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterAttributesRequest request, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<bool>> UpdateWealthAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterWealthRequest request, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
        public Task<RpgResult<bool>> UpdateCombatStatsAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterCombatStatsRequest request, CancellationToken cancellationToken) => Task.FromResult(BoolResult);
    }
}
