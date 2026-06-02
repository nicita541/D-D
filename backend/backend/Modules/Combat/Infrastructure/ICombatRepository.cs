using System.Text.Json;
using backend.Modules.Combat.Application;
using backend.Modules.Combat.Contracts;
using backend.Modules.Combat.Domain;
using backend.Modules.Combat.Infrastructure;
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

namespace backend.Modules.Combat.Infrastructure;

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
