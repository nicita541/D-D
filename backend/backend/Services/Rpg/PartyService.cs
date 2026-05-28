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

    public Task<JsonElement?> GetPartyAsync(Guid gameStateId, CancellationToken cancellationToken)
        => _repository.GetPartyAsync(gameStateId, cancellationToken);

    public Task<Guid> CreatePartyAsync(Guid gameStateId, CreatePartyRequest request, CancellationToken cancellationToken)
        => _repository.CreatePartyAsync(gameStateId, request, cancellationToken);

    public Task<Guid> AddPartyMemberAsync(Guid gameStateId, AddPartyMemberRequest request, CancellationToken cancellationToken)
        => _repository.AddPartyMemberAsync(gameStateId, request, cancellationToken);

    public Task<bool> RemovePartyMemberAsync(Guid gameStateId, Guid memberId, CancellationToken cancellationToken)
        => _repository.RemovePartyMemberAsync(gameStateId, memberId, cancellationToken);
}
