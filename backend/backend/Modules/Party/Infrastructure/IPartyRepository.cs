using System.Text.Json;
using backend.Modules.Party.Contracts;

namespace backend.Modules.Party.Infrastructure;

public interface IPartyRepository
{
    Task<JsonElement?> GetPartyAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
    Task<Guid?> CreatePartyAsync(Guid accountId, Guid gameStateId, CreatePartyRequest request, CancellationToken cancellationToken);
    Task<Guid?> AddPartyMemberAsync(Guid accountId, Guid gameStateId, AddPartyMemberRequest request, CancellationToken cancellationToken);
    Task<bool> UpdatePartyMemberAsync(Guid accountId, Guid gameStateId, Guid memberId, UpdatePartyMemberRequest request, CancellationToken cancellationToken);
    Task<bool> AssignPartyMemberCharacterAsync(Guid accountId, Guid gameStateId, Guid memberId, Guid? characterId, CancellationToken cancellationToken);
    Task<bool> AssignCharacterToAccountAsync(Guid ownerAccountId, Guid gameStateId, Guid memberAccountId, Guid characterId, CancellationToken cancellationToken);
    Task<bool> RemovePartyMemberAsync(Guid accountId, Guid gameStateId, Guid memberId, CancellationToken cancellationToken);
    Task<bool> LeavePartyAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
}
