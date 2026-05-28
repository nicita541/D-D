using System.Text.Json;
using backend.Contracts.Rpg.Parties;

namespace backend.Repositories.Rpg;

public interface IPartyRepository
{
    Task<JsonElement?> GetPartyAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
    Task<Guid?> CreatePartyAsync(Guid accountId, Guid gameStateId, CreatePartyRequest request, CancellationToken cancellationToken);
    Task<Guid?> AddPartyMemberAsync(Guid accountId, Guid gameStateId, AddPartyMemberRequest request, CancellationToken cancellationToken);
    Task<bool> RemovePartyMemberAsync(Guid accountId, Guid gameStateId, Guid memberId, CancellationToken cancellationToken);
}
