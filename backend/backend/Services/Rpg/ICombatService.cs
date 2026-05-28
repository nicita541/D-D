using System.Text.Json;
using backend.Contracts.Rpg.Combat;

namespace backend.Services.Rpg;

public interface ICombatService
{
    Task<JsonElement?> GetCombatStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
    Task<Guid?> StartCombatAsync(Guid accountId, Guid gameStateId, StartCombatRequest request, CancellationToken cancellationToken);
    Task<bool> EndCombatAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
    Task<Guid?> AddParticipantAsync(Guid accountId, Guid gameStateId, AddCombatParticipantRequest request, CancellationToken cancellationToken);
}
