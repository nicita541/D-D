using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

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
