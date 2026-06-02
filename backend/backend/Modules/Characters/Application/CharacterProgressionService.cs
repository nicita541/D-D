using System.Text.Json;
using backend.Modules.Characters.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
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

namespace backend.Modules.Characters.Application;

public sealed class CharacterProgressionService : ICharacterProgressionService
{
    private readonly ICharacterProgressionRepository _repository;

    public CharacterProgressionService(ICharacterProgressionRepository repository)
    {
        _repository = repository;
    }

    public async Task<RpgResult<JsonElement>> AddExperienceAsync(Guid accountId, Guid gameStateId, Guid characterId, AddExperienceRequest request, CancellationToken cancellationToken)
    {
        var experience = request.ResolvedExperience;
        if (!experience.HasValue || experience.Value <= 0)
        {
            return RpgResult<JsonElement>.BadRequest("опыт должен быть больше 0.");
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
            if (!request.ResolvedNewLevel.HasValue)
            {
                return RpgResult<JsonElement>.BadRequest("новыйУровень обязателен.");
            }

            if (request.ResolvedHpMaxAdd is < 0 or > 50)
            {
                return RpgResult<JsonElement>.BadRequest("хпМаксимумДобавить должен быть от 0 до 50.");
            }

            var result = await _repository.LevelUpAsync(
                accountId,
                gameStateId,
                characterId,
                request.ResolvedNewLevel.Value,
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
