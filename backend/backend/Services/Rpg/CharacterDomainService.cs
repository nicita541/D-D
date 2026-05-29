using System.Text.Json;
using backend.Contracts.Rpg.Characters;
using backend.Contracts.Rpg.Common;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

public sealed class CharacterDomainService : ICharacterDomainService
{
    private const string CharacterNotFoundMessage = "Character or game state was not found.";
    private const string EntityNotFoundMessage = "Entity was not found.";

    private readonly ICharacterDomainRepository _repository;

    public CharacterDomainService(ICharacterDomainRepository repository)
    {
        _repository = repository;
    }

    public Task<RpgResult<IReadOnlyList<JsonElement>>> GetConditionsAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => List(() => _repository.GetConditionsAsync(accountId, gameStateId, characterId, cancellationToken));

    public Task<RpgResult<Guid>> CreateConditionAsync(Guid accountId, Guid gameStateId, Guid characterId, ConditionRequest request, CancellationToken cancellationToken)
        => Create(() => _repository.CreateConditionAsync(accountId, gameStateId, characterId, request, cancellationToken));

    public Task<RpgResult<bool>> UpdateConditionAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid conditionId, ConditionRequest request, CancellationToken cancellationToken)
        => Update(() => _repository.UpdateConditionAsync(accountId, gameStateId, characterId, conditionId, request, cancellationToken));

    public Task<RpgResult<bool>> DeleteConditionAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid conditionId, CancellationToken cancellationToken)
        => Update(() => _repository.DeleteConditionAsync(accountId, gameStateId, characterId, conditionId, cancellationToken));

    public Task<RpgResult<IReadOnlyList<JsonElement>>> GetLimitedResourcesAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => List(() => _repository.GetLimitedResourcesAsync(accountId, gameStateId, characterId, cancellationToken));

    public Task<RpgResult<Guid>> CreateLimitedResourceAsync(Guid accountId, Guid gameStateId, Guid characterId, LimitedResourceRequest request, CancellationToken cancellationToken)
        => Create(() => _repository.CreateLimitedResourceAsync(accountId, gameStateId, characterId, request, cancellationToken));

    public Task<RpgResult<bool>> UpdateLimitedResourceAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid resourceId, LimitedResourceRequest request, CancellationToken cancellationToken)
        => Update(() => _repository.UpdateLimitedResourceAsync(accountId, gameStateId, characterId, resourceId, request, cancellationToken));

    public Task<RpgResult<bool>> DeleteLimitedResourceAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid resourceId, CancellationToken cancellationToken)
        => Update(() => _repository.DeleteLimitedResourceAsync(accountId, gameStateId, characterId, resourceId, cancellationToken));

    public Task<RpgResult<IReadOnlyList<JsonElement>>> GetProficienciesAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => List(() => _repository.GetProficienciesAsync(accountId, gameStateId, characterId, cancellationToken));

    public Task<RpgResult<Guid>> CreateProficiencyAsync(Guid accountId, Guid gameStateId, Guid characterId, ProficiencyRequest request, CancellationToken cancellationToken)
        => Create(() => _repository.CreateProficiencyAsync(accountId, gameStateId, characterId, request, cancellationToken));

    public Task<RpgResult<bool>> DeleteProficiencyAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid proficiencyId, CancellationToken cancellationToken)
        => Update(() => _repository.DeleteProficiencyAsync(accountId, gameStateId, characterId, proficiencyId, cancellationToken));

    public Task<RpgResult<IReadOnlyList<JsonElement>>> GetAbilitiesAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => List(() => _repository.GetAbilitiesAsync(accountId, gameStateId, characterId, cancellationToken));

    public Task<RpgResult<Guid>> CreateAbilityAsync(Guid accountId, Guid gameStateId, Guid characterId, AbilityRequest request, CancellationToken cancellationToken)
        => Create(() => _repository.CreateAbilityAsync(accountId, gameStateId, characterId, request, cancellationToken));

    public Task<RpgResult<bool>> UpdateAbilityAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid abilityId, AbilityRequest request, CancellationToken cancellationToken)
        => Update(() => _repository.UpdateAbilityAsync(accountId, gameStateId, characterId, abilityId, request, cancellationToken));

    public Task<RpgResult<bool>> DeleteAbilityAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid abilityId, CancellationToken cancellationToken)
        => Update(() => _repository.DeleteAbilityAsync(accountId, gameStateId, characterId, abilityId, cancellationToken));

    public Task<RpgResult<IReadOnlyList<JsonElement>>> GetInventoryAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => List(() => _repository.GetInventoryAsync(accountId, gameStateId, characterId, cancellationToken));

    public Task<RpgResult<Guid>> CreateInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, InventoryItemRequest request, CancellationToken cancellationToken)
        => Create(() => _repository.CreateInventoryItemAsync(accountId, gameStateId, characterId, request, cancellationToken));

    public Task<RpgResult<bool>> UpdateInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, InventoryItemRequest request, CancellationToken cancellationToken)
        => Update(() => _repository.UpdateInventoryItemAsync(accountId, gameStateId, characterId, itemId, request, cancellationToken));

    public Task<RpgResult<bool>> DeleteInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken)
        => Update(() => _repository.DeleteInventoryItemAsync(accountId, gameStateId, characterId, itemId, cancellationToken));

    public Task<RpgResult<bool>> EquipInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, InventoryItemActionRequest request, CancellationToken cancellationToken)
        => Update(() => _repository.EquipInventoryItemAsync(accountId, gameStateId, characterId, itemId, request.ResolvedSlot, cancellationToken));

    public Task<RpgResult<bool>> UnequipInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken)
        => Update(() => _repository.UnequipInventoryItemAsync(accountId, gameStateId, characterId, itemId, cancellationToken));

    public Task<RpgResult<bool>> UseInventoryItemAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken)
        => Update(() => _repository.UseInventoryItemAsync(accountId, gameStateId, characterId, itemId, cancellationToken));

    public Task<RpgResult<JsonElement>> GetEquipmentAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => Value(() => _repository.GetEquipmentAsync(accountId, gameStateId, characterId, cancellationToken));

    public Task<RpgResult<bool>> UpdateEquipmentAsync(Guid accountId, Guid gameStateId, Guid characterId, EquipmentRequest request, CancellationToken cancellationToken)
        => Update(() => _repository.UpdateEquipmentAsync(accountId, gameStateId, characterId, request, cancellationToken));

    public Task<RpgResult<IReadOnlyList<JsonElement>>> GetAttacksAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => List(() => _repository.GetAttacksAsync(accountId, gameStateId, characterId, cancellationToken));

    public Task<RpgResult<Guid>> CreateAttackAsync(Guid accountId, Guid gameStateId, Guid characterId, AttackRequest request, CancellationToken cancellationToken)
        => Create(() => _repository.CreateAttackAsync(accountId, gameStateId, characterId, request, cancellationToken));

    public Task<RpgResult<bool>> UpdateAttackAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid attackId, AttackRequest request, CancellationToken cancellationToken)
        => Update(() => _repository.UpdateAttackAsync(accountId, gameStateId, characterId, attackId, request, cancellationToken));

    public Task<RpgResult<bool>> DeleteAttackAsync(Guid accountId, Guid gameStateId, Guid characterId, Guid attackId, CancellationToken cancellationToken)
        => Update(() => _repository.DeleteAttackAsync(accountId, gameStateId, characterId, attackId, cancellationToken));

    public Task<RpgResult<JsonElement>> GetNeedsAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => Value(() => _repository.GetNeedsAsync(accountId, gameStateId, characterId, cancellationToken));

    public Task<RpgResult<bool>> UpdateNeedsAsync(Guid accountId, Guid gameStateId, Guid characterId, NeedsRequest request, CancellationToken cancellationToken)
        => Update(() => _repository.UpdateNeedsAsync(accountId, gameStateId, characterId, request, cancellationToken));

    public Task<RpgResult<bool>> UpdateProgressionAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterProgressionRequest request, CancellationToken cancellationToken)
        => Update(() => _repository.UpdateProgressionAsync(accountId, gameStateId, characterId, request, cancellationToken));

    public Task<RpgResult<bool>> UpdateResourcesAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterResourcesRequest request, CancellationToken cancellationToken)
        => Update(() => _repository.UpdateResourcesAsync(accountId, gameStateId, characterId, request, cancellationToken));

    public Task<RpgResult<bool>> UpdateAttributesAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterAttributesRequest request, CancellationToken cancellationToken)
        => Update(() => _repository.UpdateAttributesAsync(accountId, gameStateId, characterId, request, cancellationToken));

    public Task<RpgResult<bool>> UpdateWealthAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterWealthRequest request, CancellationToken cancellationToken)
        => Update(() => _repository.UpdateWealthAsync(accountId, gameStateId, characterId, request, cancellationToken));

    public Task<RpgResult<bool>> UpdateCombatStatsAsync(Guid accountId, Guid gameStateId, Guid characterId, CharacterCombatStatsRequest request, CancellationToken cancellationToken)
        => Update(() => _repository.UpdateCombatStatsAsync(accountId, gameStateId, characterId, request, cancellationToken));

    private static async Task<RpgResult<IReadOnlyList<JsonElement>>> List(Func<Task<IReadOnlyList<JsonElement>?>> action)
    {
        try
        {
            var result = await action();
            return result is null
                ? RpgResult<IReadOnlyList<JsonElement>>.NotFound(CharacterNotFoundMessage)
                : RpgResult<IReadOnlyList<JsonElement>>.Ok(result);
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<IReadOnlyList<JsonElement>>.BadRequest(ex.Message);
        }
        catch (RpgConflictException ex)
        {
            return RpgResult<IReadOnlyList<JsonElement>>.Conflict(ex.Message);
        }
    }

    private static async Task<RpgResult<JsonElement>> Value(Func<Task<JsonElement?>> action)
    {
        try
        {
            var result = await action();
            return result.HasValue
                ? RpgResult<JsonElement>.Ok(result.Value)
                : RpgResult<JsonElement>.NotFound(CharacterNotFoundMessage);
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<JsonElement>.BadRequest(ex.Message);
        }
        catch (RpgConflictException ex)
        {
            return RpgResult<JsonElement>.Conflict(ex.Message);
        }
    }

    private static async Task<RpgResult<Guid>> Create(Func<Task<Guid?>> action)
    {
        try
        {
            var result = await action();
            return result.HasValue
                ? RpgResult<Guid>.Ok(result.Value)
                : RpgResult<Guid>.NotFound(CharacterNotFoundMessage);
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<Guid>.BadRequest(ex.Message);
        }
        catch (RpgConflictException ex)
        {
            return RpgResult<Guid>.Conflict(ex.Message);
        }
    }

    private static async Task<RpgResult<bool>> Update(Func<Task<bool?>> action)
    {
        try
        {
            var result = await action();
            return result switch
            {
                null => RpgResult<bool>.NotFound(CharacterNotFoundMessage),
                true => RpgResult<bool>.Ok(true),
                false => RpgResult<bool>.NotFound(EntityNotFoundMessage)
            };
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<bool>.BadRequest(ex.Message);
        }
        catch (RpgConflictException ex)
        {
            return RpgResult<bool>.Conflict(ex.Message);
        }
    }
}
