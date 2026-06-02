using System.Text.Json;
using backend.Modules.Party.Contracts;
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

namespace backend.Modules.Party.Application;

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
