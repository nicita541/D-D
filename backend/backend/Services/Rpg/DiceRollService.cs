using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Mechanics;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

public sealed class DiceRollService : IDiceRollService
{
    private readonly IDiceRollRepository _repository;
    private readonly IDiceRoller _diceRoller;

    public DiceRollService(IDiceRollRepository repository, IDiceRoller diceRoller)
    {
        _repository = repository;
        _diceRoller = diceRoller;
    }

    public async Task<RpgResult<JsonElement>> RollAsync(Guid accountId, Guid gameStateId, RollDiceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var roll = _diceRoller.Roll(request.ResolvedFormula);
            var result = await _repository.CreateRollAsync(
                accountId,
                gameStateId,
                request.ResolvedCharacterId,
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

    public async Task<RpgResult<IReadOnlyList<JsonElement>>> GetRollsAsync(Guid accountId, Guid gameStateId, int limit, CancellationToken cancellationToken)
    {
        var rolls = await _repository.GetRollsAsync(accountId, gameStateId, Math.Clamp(limit, 1, 100), cancellationToken);
        return rolls is null
            ? RpgResult<IReadOnlyList<JsonElement>>.NotFound("GameState не найден.")
            : RpgResult<IReadOnlyList<JsonElement>>.Ok(rolls);
    }
}
