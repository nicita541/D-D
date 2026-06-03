using System.Text.Json;
using backend.Modules.Characters.Contracts;
using backend.Modules.Characters.Infrastructure;
using backend.Shared.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.Characters.Application;

public sealed class CharacterProgressionService : ICharacterProgressionService
{
    private readonly ICharacterProgressionRepository _repository;

    public CharacterProgressionService(ICharacterProgressionRepository repository)
    {
        _repository = repository;
    }

    public async Task<RpgResult<JsonElement>> GetProgressionAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        var result = await _repository.GetProgressionAsync(accountId, gameStateId, characterId, cancellationToken);
        return result.HasValue
            ? RpgResult<JsonElement>.Ok(result.Value)
            : RpgResult<JsonElement>.NotFound("GameState или персонаж не найден.");
    }

    public async Task<RpgResult<JsonElement>> AddExperienceAsync(Guid accountId, Guid gameStateId, Guid characterId, AddExperienceRequest request, CancellationToken cancellationToken)
    {
        var experience = request.ResolvedExperience;
        if (!experience.HasValue || experience.Value <= 0)
        {
            return RpgResult<JsonElement>.BadRequest("Опыт должен быть больше 0.");
        }

        var result = await _repository.AddExperienceAsync(accountId, gameStateId, characterId, experience.Value, request.ResolvedReason, cancellationToken);
        return result.HasValue
            ? RpgResult<JsonElement>.Ok(result.Value)
            : RpgResult<JsonElement>.NotFound("GameState или персонаж не найден.");
    }

    public async Task<RpgResult<JsonElement>> LevelUpAsync(Guid accountId, Guid gameStateId, Guid characterId, LevelUpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.ResolvedHpMaxAdd is < 0 or > 50)
            {
                return RpgResult<JsonElement>.BadRequest("Прирост максимального HP должен быть от 0 до 50.");
            }

            var result = await _repository.LevelUpAsync(
                accountId,
                gameStateId,
                characterId,
                request.ResolvedNewLevel,
                request.ResolvedHpMaxAdd,
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
}
