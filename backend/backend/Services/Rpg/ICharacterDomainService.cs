using System.Text.Json;
using backend.Contracts.Rpg.Characters;
using backend.Contracts.Rpg.Common;

namespace backend.Services.Rpg;

public interface ICharacterDomainService
{
    Task<RpgResult<IReadOnlyList<JsonElement>>> GetConditionsAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<RpgResult<Guid>> CreateConditionAsync(Guid accountId, Guid gameStateId, Guid characterId, ConditionRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> UpdateConditionAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid conditionId, ConditionRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> DeleteConditionAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid conditionId, CancellationToken cancellationToken);

    Task<RpgResult<IReadOnlyList<JsonElement>>> GetLimitedResourcesAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<RpgResult<Guid>> CreateLimitedResourceAsync(Guid accountId, Guid gameStateId, Guid characterId, LimitedResourceRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> UpdateLimitedResourceAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid resourceId, LimitedResourceRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> DeleteLimitedResourceAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid resourceId, CancellationToken cancellationToken);

    Task<RpgResult<IReadOnlyList<JsonElement>>> GetProficienciesAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<RpgResult<Guid>> CreateProficiencyAsync(Guid accountId, Guid gameStateId, Guid characterId, ProficiencyRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> DeleteProficiencyAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid proficiencyId, CancellationToken cancellationToken);

    Task<RpgResult<IReadOnlyList<JsonElement>>> GetAbilitiesAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<RpgResult<Guid>> CreateAbilityAsync(Guid accountId, Guid gameStateId, Guid characterId, AbilityRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> UpdateAbilityAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid abilityId, AbilityRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> DeleteAbilityAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid abilityId, CancellationToken cancellationToken);

    Task<RpgResult<IReadOnlyList<JsonElement>>> GetInventoryAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<RpgResult<Guid>> CreateInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, InventoryItemRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> UpdateInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, InventoryItemRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> DeleteInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken);
    Task<RpgResult<bool>> EquipInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, InventoryItemActionRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> UnequipInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken);
    Task<RpgResult<bool>> UseInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> GetEquipmentAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<RpgResult<bool>> UpdateEquipmentAsync(Guid accountId, Guid gameStateId, Guid characterId, EquipmentRequest request, CancellationToken cancellationToken);

    Task<RpgResult<IReadOnlyList<JsonElement>>> GetAttacksAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<RpgResult<Guid>> CreateAttackAsync(Guid accountId, Guid gameStateId, Guid characterId, AttackRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> UpdateAttackAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid attackId, AttackRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> DeleteAttackAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid attackId, CancellationToken cancellationToken);

    Task<RpgResult<JsonElement>> GetNeedsAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<RpgResult<bool>> UpdateNeedsAsync(Guid accountId, Guid gameStateId, Guid characterId, NeedsRequest request, CancellationToken cancellationToken);

    Task<RpgResult<bool>> UpdateProgressionAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterProgressionRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> UpdateResourcesAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterResourcesRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> UpdateAttributesAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterAttributesRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> UpdateWealthAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterWealthRequest request, CancellationToken cancellationToken);
    Task<RpgResult<bool>> UpdateCombatStatsAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterCombatStatsRequest request, CancellationToken cancellationToken);
}
