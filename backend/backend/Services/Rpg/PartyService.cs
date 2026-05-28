using System.Text.Json;
using backend.Contracts.Rpg.Parties;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

public sealed class PartyService : IPartyService
{
    private readonly IPartyRepository _repository;

    public PartyService(IPartyRepository repository)
    {
        _repository = repository;
    }

    public Task<JsonElement?> GetPartyAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => _repository.GetPartyAsync(accountId, gameStateId, cancellationToken);

    public Task<Guid?> CreatePartyAsync(Guid accountId, Guid gameStateId, CreatePartyRequest request, CancellationToken cancellationToken)
        => _repository.CreatePartyAsync(accountId, gameStateId, request, cancellationToken);

    public Task<Guid?> AddPartyMemberAsync(Guid accountId, Guid gameStateId, AddPartyMemberRequest request, CancellationToken cancellationToken)
        => _repository.AddPartyMemberAsync(accountId, gameStateId, request, cancellationToken);

    public Task<bool> RemovePartyMemberAsync(Guid accountId, Guid gameStateId, Guid memberId, CancellationToken cancellationToken)
        => _repository.RemovePartyMemberAsync(accountId, gameStateId, memberId, cancellationToken);
}
