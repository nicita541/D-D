using System.Text.Json;
using backend.Contracts.Rpg.Characters;

namespace backend.Repositories.Rpg;

public interface ICharacterDomainRepository
{
    Task<IReadOnlyList<JsonElement>?> GetConditionsAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<Guid?> CreateConditionAsync(Guid accountId, Guid gameStateId, Guid characterId, ConditionRequest request, CancellationToken cancellationToken);
    Task<bool?> UpdateConditionAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid conditionId, ConditionRequest request, CancellationToken cancellationToken);
    Task<bool?> DeleteConditionAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid conditionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<JsonElement>?> GetLimitedResourcesAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<Guid?> CreateLimitedResourceAsync(Guid accountId, Guid gameStateId, Guid characterId, LimitedResourceRequest request, CancellationToken cancellationToken);
    Task<bool?> UpdateLimitedResourceAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid resourceId, LimitedResourceRequest request, CancellationToken cancellationToken);
    Task<bool?> DeleteLimitedResourceAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid resourceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<JsonElement>?> GetProficienciesAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<Guid?> CreateProficiencyAsync(Guid accountId, Guid gameStateId, Guid characterId, ProficiencyRequest request, CancellationToken cancellationToken);
    Task<bool?> DeleteProficiencyAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid proficiencyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<JsonElement>?> GetAbilitiesAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<Guid?> CreateAbilityAsync(Guid accountId, Guid gameStateId, Guid characterId, AbilityRequest request, CancellationToken cancellationToken);
    Task<bool?> UpdateAbilityAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid abilityId, AbilityRequest request, CancellationToken cancellationToken);
    Task<bool?> DeleteAbilityAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid abilityId, CancellationToken cancellationToken);

    Task<IReadOnlyList<JsonElement>?> GetInventoryAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<Guid?> CreateInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, InventoryItemRequest request, CancellationToken cancellationToken);
    Task<bool?> UpdateInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, InventoryItemRequest request, CancellationToken cancellationToken);
    Task<bool?> DeleteInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken);

    Task<JsonElement?> GetEquipmentAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<bool?> UpdateEquipmentAsync(Guid accountId, Guid gameStateId, Guid characterId, EquipmentRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<JsonElement>?> GetAttacksAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<Guid?> CreateAttackAsync(Guid accountId, Guid gameStateId, Guid characterId, AttackRequest request, CancellationToken cancellationToken);
    Task<bool?> UpdateAttackAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid attackId, AttackRequest request, CancellationToken cancellationToken);
    Task<bool?> DeleteAttackAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid attackId, CancellationToken cancellationToken);

    Task<JsonElement?> GetNeedsAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken);
    Task<bool?> UpdateNeedsAsync(Guid accountId, Guid gameStateId, Guid characterId, NeedsRequest request, CancellationToken cancellationToken);

    Task<bool?> UpdateProgressionAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterProgressionRequest request, CancellationToken cancellationToken);
    Task<bool?> UpdateResourcesAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterResourcesRequest request, CancellationToken cancellationToken);
    Task<bool?> UpdateAttributesAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterAttributesRequest request, CancellationToken cancellationToken);
    Task<bool?> UpdateWealthAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterWealthRequest request, CancellationToken cancellationToken);
    Task<bool?> UpdateCombatStatsAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterCombatStatsRequest request, CancellationToken cancellationToken);
}
