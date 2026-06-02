using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
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
using backend.Modules.Ai.Infrastructure;
using backend.Modules.Campaigns.Infrastructure;
using backend.Modules.Changes.Infrastructure;
using backend.Modules.Characters.Infrastructure;
using backend.Modules.Combat.Infrastructure;
using backend.Modules.GameStates.Infrastructure;
using backend.Modules.Mechanics.Infrastructure;
using backend.Modules.Memory.Infrastructure;
using backend.Modules.Party.Infrastructure;
using backend.Modules.Play.Infrastructure;
using backend.Modules.Story.Infrastructure;
using backend.Modules.Travel.Infrastructure;
using backend.Modules.Turns.Infrastructure;
using backend.Modules.World.Infrastructure;

namespace backend.Modules.Combat.Application;

public sealed class CombatService : ICombatService
{
    private readonly ICombatRepository _repository;
    private readonly IDiceRoller _diceRoller;

    public CombatService(ICombatRepository repository, IDiceRoller diceRoller)
    {
        _repository = repository;
        _diceRoller = diceRoller;
    }

    public Task<JsonElement?> GetCombatStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => _repository.GetCombatStateAsync(accountId, gameStateId, cancellationToken);

    public Task<Guid?> StartCombatAsync(Guid accountId, Guid gameStateId, StartCombatRequest request, CancellationToken cancellationToken)
        => _repository.StartCombatAsync(accountId, gameStateId, request, cancellationToken);

    public Task<bool> EndCombatAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => _repository.EndCombatAsync(accountId, gameStateId, cancellationToken);

    public Task<Guid?> AddParticipantAsync(Guid accountId, Guid gameStateId, AddCombatParticipantRequest request, CancellationToken cancellationToken)
        => _repository.AddParticipantAsync(accountId, gameStateId, request, cancellationToken);

    public Task<JsonElement?> NextTurnAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => _repository.NextTurnAsync(accountId, gameStateId, cancellationToken);

    public Task<JsonElement?> ApplyDamageAsync(Guid accountId, Guid gameStateId, ApplyCombatDamageRequest request, CancellationToken cancellationToken)
    {
        ValidateDamageRequest(request);
        return _repository.ApplyDamageAsync(accountId, gameStateId, request, cancellationToken);
    }

    public Task<JsonElement?> HealParticipantAsync(Guid accountId, Guid gameStateId, Guid participantId, HealCombatParticipantRequest request, CancellationToken cancellationToken)
    {
        if (!request.ResolvedHealing.HasValue || request.ResolvedHealing.Value < 0)
        {
            throw new CombatValidationException("лечение должно быть числом 0 или больше.");
        }

        return _repository.HealParticipantAsync(accountId, gameStateId, participantId, request, cancellationToken);
    }

    public Task<JsonElement?> AttackAsync(Guid accountId, Guid gameStateId, CombatAttackRequest request, CancellationToken cancellationToken)
    {
        if (!request.ResolvedAttackerParticipantId.HasValue)
        {
            throw new CombatValidationException("атакующийУчастникId обязателен.");
        }

        if (!request.ResolvedTargetParticipantId.HasValue)
        {
            throw new CombatValidationException("цельУчастникId обязателен.");
        }

        DiceRollResult attackRoll;
        DiceRollResult damageRoll;
        try
        {
            attackRoll = _diceRoller.Roll(request.ResolvedAttackRoll);
            damageRoll = _diceRoller.Roll(request.ResolvedDamageRoll);
        }
        catch (RpgValidationException ex)
        {
            throw new CombatValidationException(ex.Message);
        }

        return _repository.ResolveAttackAsync(accountId, gameStateId, request, attackRoll, damageRoll, cancellationToken);
    }

    private static void ValidateDamageRequest(ApplyCombatDamageRequest request)
    {
        if (!request.ResolvedTargetParticipantId.HasValue)
        {
            throw new CombatValidationException("цельУчастникId обязателен.");
        }

        if (!request.ResolvedDamage.HasValue || request.ResolvedDamage.Value < 0)
        {
            throw new CombatValidationException("урон должен быть числом 0 или больше.");
        }
    }
}
