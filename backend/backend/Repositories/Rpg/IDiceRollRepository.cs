using System.Text.Json;
using backend.Services.Rpg;

namespace backend.Repositories.Rpg;

public interface IDiceRollRepository
{
    Task<JsonElement?> CreateRollAsync(
        Guid accountId,
        Guid gameStateId,
        Guid? characterId,
        string reason,
        DiceRollResult roll,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JsonElement>?> GetRollsAsync(
        Guid accountId,
        Guid gameStateId,
        int limit,
        CancellationToken cancellationToken);
}
