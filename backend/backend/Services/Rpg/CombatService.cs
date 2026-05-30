using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Combat;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

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
