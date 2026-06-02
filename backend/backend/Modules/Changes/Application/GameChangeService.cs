using System.Text.Json;
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

namespace backend.Modules.Changes.Application;

public sealed class GameChangeService : IGameChangeService
{
    private readonly IGameChangeRepository _repository;

    public GameChangeService(IGameChangeRepository repository)
    {
        _repository = repository;
    }

    public async Task<RpgResult<IReadOnlyList<JsonElement>>> GetChangesAsync(Guid accountId, Guid gameStateId, string? status, CancellationToken cancellationToken)
    {
        try
        {
            var changes = await _repository.GetChangesAsync(accountId, gameStateId, status, cancellationToken);
            return changes is null
                ? RpgResult<IReadOnlyList<JsonElement>>.NotFound("GameState was not found.")
                : RpgResult<IReadOnlyList<JsonElement>>.Ok(changes);
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<IReadOnlyList<JsonElement>>.BadRequest(ex.Message);
        }
    }

    public async Task<RpgResult<JsonElement>> GetChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, CancellationToken cancellationToken)
    {
        var change = await _repository.GetChangeAsync(accountId, gameStateId, changeId, cancellationToken);
        return change.HasValue
            ? RpgResult<JsonElement>.Ok(change.Value)
            : RpgResult<JsonElement>.NotFound("Change was not found.");
    }

    public async Task<RpgResult<JsonElement>> ApplyChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, CancellationToken cancellationToken)
    {
        try
        {
            var change = await _repository.ApplyChangeAsync(accountId, gameStateId, changeId, cancellationToken);
            return change.HasValue
                ? RpgResult<JsonElement>.Ok(change.Value)
                : RpgResult<JsonElement>.NotFound("Pending change was not found.");
        }
        catch (RpgValidationException ex)
        {
            return RpgResult<JsonElement>.BadRequest(ex.Message);
        }
    }

    public async Task<RpgResult<JsonElement>> RejectChangeAsync(Guid accountId, Guid gameStateId, Guid changeId, string reason, CancellationToken cancellationToken)
    {
        var change = await _repository.RejectChangeAsync(accountId, gameStateId, changeId, reason, cancellationToken);
        return change.HasValue
            ? RpgResult<JsonElement>.Ok(change.Value)
            : RpgResult<JsonElement>.NotFound("Pending change was not found.");
    }
}
