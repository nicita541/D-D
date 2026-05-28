using System.Text.Json;
using backend.Contracts.Rpg.Parties;

namespace backend.Repositories.Rpg;

public interface IPartyRepository
{
    Task<JsonElement?> GetPartyAsync(Guid gameStateId, CancellationToken cancellationToken);
    Task<Guid> CreatePartyAsync(Guid gameStateId, CreatePartyRequest request, CancellationToken cancellationToken);
    Task<Guid> AddPartyMemberAsync(Guid gameStateId, AddPartyMemberRequest request, CancellationToken cancellationToken);
    Task<bool> RemovePartyMemberAsync(Guid gameStateId, Guid memberId, CancellationToken cancellationToken);
}
