using System.Text.Json;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.World.Contracts;
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

namespace backend.Modules.World.Application;

public sealed class WorldService : IWorldService
{
    private readonly IWorldRepository _repository;

    public WorldService(IWorldRepository repository)
    {
        _repository = repository;
    }

    public async Task<RpgResult<IReadOnlyList<JsonElement>>> ListAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid? parentId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _repository.ListAsync(accountId, gameStateId, kind, parentId, cancellationToken);
            return result is null
                ? RpgResult<IReadOnlyList<JsonElement>>.NotFound("GameState or parent entity was not found.")
                : RpgResult<IReadOnlyList<JsonElement>>.Ok(result);
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<IReadOnlyList<JsonElement>>.BadRequest(ex.Message);
        }
    }

    public async Task<RpgResult<JsonElement>> GetAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _repository.GetAsync(accountId, gameStateId, kind, entityId, parentId, cancellationToken);
            return result.HasValue
                ? RpgResult<JsonElement>.Ok(result.Value)
                : RpgResult<JsonElement>.NotFound("World entity was not found.");
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<JsonElement>.BadRequest(ex.Message);
        }
    }

    public async Task<RpgResult<Guid>> CreateAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid? parentId, JsonElement payload, CancellationToken cancellationToken)
    {
        try
        {
            var id = await _repository.CreateAsync(accountId, gameStateId, kind, parentId, payload, cancellationToken);
            return id.HasValue
                ? RpgResult<Guid>.Ok(id.Value)
                : RpgResult<Guid>.NotFound("GameState or parent entity was not found.");
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<Guid>.BadRequest(ex.Message);
        }
    }

    public async Task<RpgResult<bool>> UpdateAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, JsonElement payload, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _repository.UpdateAsync(accountId, gameStateId, kind, entityId, parentId, payload, cancellationToken);
            return updated switch
            {
                null => RpgResult<bool>.NotFound("GameState or parent entity was not found."),
                true => RpgResult<bool>.Ok(true),
                false => RpgResult<bool>.NotFound("World entity was not found.")
            };
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<bool>.BadRequest(ex.Message);
        }
    }

    public async Task<RpgResult<bool>> DeleteAsync(Guid accountId, Guid gameStateId, WorldEntityKind kind, Guid entityId, Guid? parentId, CancellationToken cancellationToken)
    {
        var deleted = await _repository.DeleteAsync(accountId, gameStateId, kind, entityId, parentId, cancellationToken);
        return deleted switch
        {
            null => RpgResult<bool>.NotFound("GameState or parent entity was not found."),
            true => RpgResult<bool>.Ok(true),
            false => RpgResult<bool>.NotFound("World entity was not found.")
        };
    }
}
