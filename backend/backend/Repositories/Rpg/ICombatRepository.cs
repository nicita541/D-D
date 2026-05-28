using System.Text.Json;
using backend.Contracts.Rpg.Combat;

namespace backend.Repositories.Rpg;

public interface ICombatRepository
{
    Task<JsonElement?> GetCombatStateAsync(Guid gameStateId, CancellationToken cancellationToken);
    Task<Guid> StartCombatAsync(Guid gameStateId, StartCombatRequest request, CancellationToken cancellationToken);
    Task<bool> EndCombatAsync(Guid gameStateId, CancellationToken cancellationToken);
    Task<Guid> AddParticipantAsync(Guid gameStateId, AddCombatParticipantRequest request, CancellationToken cancellationToken);
}
