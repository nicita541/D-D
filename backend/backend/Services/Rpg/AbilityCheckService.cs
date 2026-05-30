using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Mechanics;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

public sealed class AbilityCheckService : IAbilityCheckService
{
    private readonly IAbilityCheckRepository _repository;
    private readonly IDiceRoller _diceRoller;

    public AbilityCheckService(IAbilityCheckRepository repository, IDiceRoller diceRoller)
    {
        _repository = repository;
        _diceRoller = diceRoller;
    }

    public async Task<RpgResult<JsonElement>> CreateAbilityCheckAsync(Guid accountId, Guid gameStateId, AbilityCheckRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.ResolvedCharacterId.HasValue)
            {
                return RpgResult<JsonElement>.BadRequest("персонажId обязателен.");
            }

            if (!request.ResolvedDifficultyClass.HasValue)
            {
                return RpgResult<JsonElement>.BadRequest("сложность обязательна.");
            }

            var ability = AbilityRules.NormalizeAbility(request.ResolvedAbility);
            var abilityScore = await _repository.GetAbilityScoreAsync(
                accountId,
                gameStateId,
                request.ResolvedCharacterId.Value,
                ability,
                cancellationToken);

            if (!abilityScore.HasValue)
            {
                return RpgResult<JsonElement>.NotFound("GameState или персонаж не найден.");
            }

            var modifier = AbilityRules.CalculateModifier(abilityScore.Value);
            var formula = modifier == 0 ? "1d20" : $"1d20{(modifier > 0 ? "+" : string.Empty)}{modifier}";
            var roll = _diceRoller.Roll(formula);
            var success = roll.Total >= request.ResolvedDifficultyClass.Value;

            var result = await _repository.CreateAbilityCheckAsync(
                accountId,
                gameStateId,
                request.ResolvedCharacterId.Value,
                ability,
                abilityScore.Value,
                modifier,
                request.ResolvedDifficultyClass.Value,
                success,
                request.ResolvedReason,
                roll,
                cancellationToken);

            return result.HasValue
                ? RpgResult<JsonElement>.Ok(result.Value)
                : RpgResult<JsonElement>.NotFound("GameState или персонаж не найден.");
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<JsonElement>.BadRequest(ex.Message);
        }
    }

    public async Task<RpgResult<IReadOnlyList<JsonElement>>> GetChecksAsync(Guid accountId, Guid gameStateId, int limit, CancellationToken cancellationToken)
    {
        var checks = await _repository.GetChecksAsync(accountId, gameStateId, Math.Clamp(limit, 1, 100), cancellationToken);
        return checks is null
            ? RpgResult<IReadOnlyList<JsonElement>>.NotFound("GameState не найден.")
            : RpgResult<IReadOnlyList<JsonElement>>.Ok(checks);
    }
}
