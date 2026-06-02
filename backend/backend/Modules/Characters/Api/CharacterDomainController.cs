using System.Text.Json;
using backend.Modules.Characters.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Infrastructure.Auth;
using backend.Modules.Ai.Application;
using backend.Modules.Campaigns.Application;
using backend.Modules.Changes.Application;
using backend.Modules.Characters.Application;
using backend.Modules.Combat.Application;
using backend.Modules.GameStates.Application;
using backend.Modules.Mechanics.Application;
using backend.Modules.Memory.Application;
using backend.Modules.Party.Application;
using backend.Modules.Play.Application;
using backend.Modules.Story.Application;
using backend.Modules.Travel.Application;
using backend.Modules.Turns.Application;
using backend.Modules.World.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Characters.Api;

[Authorize]
[ApiController]
[Route("api/game-states/{gameStateId:guid}/characters/{characterId:guid}")]
public sealed class CharacterDomainController : ControllerBase
{
    private readonly ICharacterDomainService _domain;
    private readonly ICurrentUserService _currentUser;

    public CharacterDomainController(ICharacterDomainService domain, ICurrentUserService currentUser)
    {
        _domain = domain;
        _currentUser = currentUser;
    }

    [HttpGet("conditions")]
    public async Task<ActionResult> GetConditions(Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => ToActionResult(await _domain.GetConditionsAsync(AccountId(), gameStateId, characterId, cancellationToken));

    [HttpPost("conditions")]
    public async Task<ActionResult> CreateCondition(Guid gameStateId, Guid characterId, [FromBody] ConditionRequest request, CancellationToken cancellationToken)
        => ToCreatedResult(await _domain.CreateConditionAsync(AccountId(), gameStateId, characterId, request, cancellationToken), "Condition created.");

    [HttpPut("conditions/{conditionId:guid}")]
    public async Task<ActionResult> UpdateCondition(Guid gameStateId, Guid characterId, Guid conditionId, [FromBody] ConditionRequest request, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.UpdateConditionAsync(AccountId(), gameStateId, characterId, conditionId, request, cancellationToken), conditionId, "Condition updated.");

    [HttpDelete("conditions/{conditionId:guid}")]
    public async Task<ActionResult> DeleteCondition(Guid gameStateId, Guid characterId, Guid conditionId, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.DeleteConditionAsync(AccountId(), gameStateId, characterId, conditionId, cancellationToken), conditionId, "Condition deleted.");

    [HttpGet("limited-resources")]
    public async Task<ActionResult> GetLimitedResources(Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => ToActionResult(await _domain.GetLimitedResourcesAsync(AccountId(), gameStateId, characterId, cancellationToken));

    [HttpPost("limited-resources")]
    public async Task<ActionResult> CreateLimitedResource(Guid gameStateId, Guid characterId, [FromBody] LimitedResourceRequest request, CancellationToken cancellationToken)
        => ToCreatedResult(await _domain.CreateLimitedResourceAsync(AccountId(), gameStateId, characterId, request, cancellationToken), "Limited resource created.");

    [HttpPut("limited-resources/{resourceId:guid}")]
    public async Task<ActionResult> UpdateLimitedResource(Guid gameStateId, Guid characterId, Guid resourceId, [FromBody] LimitedResourceRequest request, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.UpdateLimitedResourceAsync(AccountId(), gameStateId, characterId, resourceId, request, cancellationToken), resourceId, "Limited resource updated.");

    [HttpDelete("limited-resources/{resourceId:guid}")]
    public async Task<ActionResult> DeleteLimitedResource(Guid gameStateId, Guid characterId, Guid resourceId, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.DeleteLimitedResourceAsync(AccountId(), gameStateId, characterId, resourceId, cancellationToken), resourceId, "Limited resource deleted.");

    [HttpGet("proficiencies")]
    public async Task<ActionResult> GetProficiencies(Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => ToActionResult(await _domain.GetProficienciesAsync(AccountId(), gameStateId, characterId, cancellationToken));

    [HttpPost("proficiencies")]
    public async Task<ActionResult> CreateProficiency(Guid gameStateId, Guid characterId, [FromBody] ProficiencyRequest request, CancellationToken cancellationToken)
        => ToCreatedResult(await _domain.CreateProficiencyAsync(AccountId(), gameStateId, characterId, request, cancellationToken), "Proficiency created.");

    [HttpDelete("proficiencies/{proficiencyId:guid}")]
    public async Task<ActionResult> DeleteProficiency(Guid gameStateId, Guid characterId, Guid proficiencyId, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.DeleteProficiencyAsync(AccountId(), gameStateId, characterId, proficiencyId, cancellationToken), proficiencyId, "Proficiency deleted.");

    [HttpGet("abilities")]
    public async Task<ActionResult> GetAbilities(Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => ToActionResult(await _domain.GetAbilitiesAsync(AccountId(), gameStateId, characterId, cancellationToken));

    [HttpPost("abilities")]
    public async Task<ActionResult> CreateAbility(Guid gameStateId, Guid characterId, [FromBody] AbilityRequest request, CancellationToken cancellationToken)
        => ToCreatedResult(await _domain.CreateAbilityAsync(AccountId(), gameStateId, characterId, request, cancellationToken), "Ability created.");

    [HttpPut("abilities/{abilityId:guid}")]
    public async Task<ActionResult> UpdateAbility(Guid gameStateId, Guid characterId, Guid abilityId, [FromBody] AbilityRequest request, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.UpdateAbilityAsync(AccountId(), gameStateId, characterId, abilityId, request, cancellationToken), abilityId, "Ability updated.");

    [HttpDelete("abilities/{abilityId:guid}")]
    public async Task<ActionResult> DeleteAbility(Guid gameStateId, Guid characterId, Guid abilityId, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.DeleteAbilityAsync(AccountId(), gameStateId, characterId, abilityId, cancellationToken), abilityId, "Ability deleted.");

    [HttpGet("inventory")]
    public async Task<ActionResult> GetInventory(Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => ToActionResult(await _domain.GetInventoryAsync(AccountId(), gameStateId, characterId, cancellationToken));

    [HttpPost("inventory")]
    public async Task<ActionResult> CreateInventoryItem(Guid gameStateId, Guid characterId, [FromBody] InventoryItemRequest request, CancellationToken cancellationToken)
        => ToCreatedResult(await _domain.CreateInventoryItemAsync(AccountId(), gameStateId, characterId, request, cancellationToken), "Inventory item created.");

    [HttpPost("inventory/items")]
    public async Task<ActionResult> CreateInventoryItemAlias(Guid gameStateId, Guid characterId, [FromBody] InventoryItemRequest request, CancellationToken cancellationToken)
        => ToCreatedResult(await _domain.CreateInventoryItemAsync(AccountId(), gameStateId, characterId, request, cancellationToken), "Inventory item created.");

    [HttpPut("inventory/{itemId:guid}")]
    public async Task<ActionResult> UpdateInventoryItem(Guid gameStateId, Guid characterId, Guid itemId, [FromBody] InventoryItemRequest request, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.UpdateInventoryItemAsync(AccountId(), gameStateId, characterId, itemId, request, cancellationToken), itemId, "Inventory item updated.");

    [HttpDelete("inventory/{itemId:guid}")]
    public async Task<ActionResult> DeleteInventoryItem(Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.DeleteInventoryItemAsync(AccountId(), gameStateId, characterId, itemId, cancellationToken), itemId, "Inventory item deleted.");

    [HttpDelete("inventory/items/{itemId:guid}")]
    public async Task<ActionResult> DeleteInventoryItemAlias(Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.DeleteInventoryItemAsync(AccountId(), gameStateId, characterId, itemId, cancellationToken), itemId, "Inventory item deleted.");

    [HttpPost("inventory/items/{itemId:guid}/equip")]
    public async Task<ActionResult> EquipInventoryItem(Guid gameStateId, Guid characterId, Guid itemId, [FromBody] InventoryItemActionRequest? request, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.EquipInventoryItemAsync(AccountId(), gameStateId, characterId, itemId, request ?? new InventoryItemActionRequest(), cancellationToken), itemId, "Inventory item equipped.");

    [HttpPost("inventory/items/{itemId:guid}/unequip")]
    public async Task<ActionResult> UnequipInventoryItem(Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.UnequipInventoryItemAsync(AccountId(), gameStateId, characterId, itemId, cancellationToken), itemId, "Inventory item unequipped.");

    [HttpPost("inventory/items/{itemId:guid}/use")]
    public async Task<ActionResult> UseInventoryItem(Guid gameStateId, Guid characterId, Guid itemId, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.UseInventoryItemAsync(AccountId(), gameStateId, characterId, itemId, cancellationToken), itemId, "Inventory item used.");

    [HttpGet("equipment")]
    public async Task<ActionResult> GetEquipment(Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => ToActionResult(await _domain.GetEquipmentAsync(AccountId(), gameStateId, characterId, cancellationToken));

    [HttpPut("equipment")]
    public async Task<ActionResult> UpdateEquipment(Guid gameStateId, Guid characterId, [FromBody] EquipmentRequest request, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.UpdateEquipmentAsync(AccountId(), gameStateId, characterId, request, cancellationToken), characterId, "Equipment updated.");

    [HttpGet("attacks")]
    public async Task<ActionResult> GetAttacks(Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => ToActionResult(await _domain.GetAttacksAsync(AccountId(), gameStateId, characterId, cancellationToken));

    [HttpPost("attacks")]
    public async Task<ActionResult> CreateAttack(Guid gameStateId, Guid characterId, [FromBody] AttackRequest request, CancellationToken cancellationToken)
        => ToCreatedResult(await _domain.CreateAttackAsync(AccountId(), gameStateId, characterId, request, cancellationToken), "Attack created.");

    [HttpPut("attacks/{attackId:guid}")]
    public async Task<ActionResult> UpdateAttack(Guid gameStateId, Guid characterId, Guid attackId, [FromBody] AttackRequest request, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.UpdateAttackAsync(AccountId(), gameStateId, characterId, attackId, request, cancellationToken), attackId, "Attack updated.");

    [HttpDelete("attacks/{attackId:guid}")]
    public async Task<ActionResult> DeleteAttack(Guid gameStateId, Guid characterId, Guid attackId, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.DeleteAttackAsync(AccountId(), gameStateId, characterId, attackId, cancellationToken), attackId, "Attack deleted.");

    [HttpGet("needs")]
    public async Task<ActionResult> GetNeeds(Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
        => ToActionResult(await _domain.GetNeedsAsync(AccountId(), gameStateId, characterId, cancellationToken));

    [HttpPut("needs")]
    public async Task<ActionResult> UpdateNeeds(Guid gameStateId, Guid characterId, [FromBody] NeedsRequest request, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.UpdateNeedsAsync(AccountId(), gameStateId, characterId, request, cancellationToken), characterId, "Needs updated.");

    [HttpPut("progression")]
    public async Task<ActionResult> UpdateProgression(Guid gameStateId, Guid characterId, [FromBody] CharacterProgressionRequest request, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.UpdateProgressionAsync(AccountId(), gameStateId, characterId, request, cancellationToken), characterId, "Progression updated.");

    [HttpPut("resources")]
    public async Task<ActionResult> UpdateResources(Guid gameStateId, Guid characterId, [FromBody] CharacterResourcesRequest request, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.UpdateResourcesAsync(AccountId(), gameStateId, characterId, request, cancellationToken), characterId, "Resources updated.");

    [HttpPut("attributes")]
    public async Task<ActionResult> UpdateAttributes(Guid gameStateId, Guid characterId, [FromBody] CharacterAttributesRequest request, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.UpdateAttributesAsync(AccountId(), gameStateId, characterId, request, cancellationToken), characterId, "Attributes updated.");

    [HttpPut("wealth")]
    public async Task<ActionResult> UpdateWealth(Guid gameStateId, Guid characterId, [FromBody] CharacterWealthRequest request, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.UpdateWealthAsync(AccountId(), gameStateId, characterId, request, cancellationToken), characterId, "Wealth updated.");

    [HttpPut("combat-stats")]
    public async Task<ActionResult> UpdateCombatStats(Guid gameStateId, Guid characterId, [FromBody] CharacterCombatStatsRequest request, CancellationToken cancellationToken)
        => ToOperationResult(await _domain.UpdateCombatStatsAsync(AccountId(), gameStateId, characterId, request, cancellationToken), characterId, "Combat stats updated.");

    private Guid AccountId() => _currentUser.GetRequiredUser().AccountId;

    private ActionResult ToActionResult<T>(RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(result.Value),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Bad request." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Not found." }),
            RpgResultStatus.Conflict => Conflict(new MessageResponse { Message = result.Message ?? "Conflict." }),
            RpgResultStatus.ServiceUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse { Message = result.Message ?? "Service unavailable." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };

    private ActionResult ToCreatedResult(RpgResult<Guid> result, string message)
        => result.Status switch
        {
            RpgResultStatus.Ok => StatusCode(StatusCodes.Status201Created, new OperationResponse { Id = result.Value, Message = message }),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Bad request." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Not found." }),
            RpgResultStatus.Conflict => Conflict(new MessageResponse { Message = result.Message ?? "Conflict." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };

    private ActionResult ToOperationResult(RpgResult<bool> result, Guid id, string message)
        => result.Status switch
        {
            RpgResultStatus.Ok => Ok(new OperationResponse { Id = id, Message = message }),
            RpgResultStatus.BadRequest => BadRequest(new MessageResponse { Message = result.Message ?? "Bad request." }),
            RpgResultStatus.NotFound => NotFound(new MessageResponse { Message = result.Message ?? "Not found." }),
            RpgResultStatus.Conflict => Conflict(new MessageResponse { Message = result.Message ?? "Conflict." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
}
