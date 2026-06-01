using System.Text.Json;
using backend.Contracts.Rpg.Combat;
using backend.Services.Rpg;

namespace backend.Repositories.Rpg;

public interface ICombatRepository
{
    Task<JsonElement?> GetCombatStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
    Task<Guid?> StartCombatAsync(Guid accountId, Guid gameStateId, StartCombatRequest request, CancellationToken cancellationToken);
    Task<bool> EndCombatAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
    Task<Guid?> AddParticipantAsync(Guid accountId, Guid gameStateId, AddCombatParticipantRequest request, CancellationToken cancellationToken);
    Task<JsonElement?> NextTurnAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
    Task<JsonElement?> ApplyDamageAsync(Guid accountId, Guid gameStateId, ApplyCombatDamageRequest request, CancellationToken cancellationToken);
    Task<JsonElement?> HealParticipantAsync(Guid accountId, Guid gameStateId, Guid participantId, HealCombatParticipantRequest request, CancellationToken cancellationToken);
    Task<JsonElement?> ResolveAttackAsync(Guid accountId, Guid gameStateId, CombatAttackRequest request, DiceRollResult attackRoll, DiceRollResult damageRoll, CancellationToken cancellationToken);
}
