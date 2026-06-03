using System.Text.Json;
using backend.Modules.Combat.Application;
using backend.Modules.Combat.Contracts;
using backend.Modules.Combat.Domain;
using backend.Modules.Combat.Infrastructure;

namespace backend.Modules.Combat.Application;

public interface ICombatService
{
    Task<JsonElement?> GetCombatStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
    Task<Guid?> StartCombatAsync(Guid accountId, Guid gameStateId, StartCombatRequest request, CancellationToken cancellationToken);
    Task<bool> EndCombatAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
    Task<Guid?> AddParticipantAsync(Guid accountId, Guid gameStateId, AddCombatParticipantRequest request, CancellationToken cancellationToken);
    Task<JsonElement?> NextTurnAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
    Task<JsonElement?> ApplyDamageAsync(Guid accountId, Guid gameStateId, ApplyCombatDamageRequest request, CancellationToken cancellationToken);
    Task<JsonElement?> HealParticipantAsync(Guid accountId, Guid gameStateId, Guid participantId, HealCombatParticipantRequest request, CancellationToken cancellationToken);
    Task<JsonElement?> AttackAsync(Guid accountId, Guid gameStateId, CombatAttackRequest request, CancellationToken cancellationToken);
}
