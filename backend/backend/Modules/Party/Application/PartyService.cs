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

    public Task<bool> UpdatePartyMemberAsync(Guid accountId, Guid gameStateId, Guid memberId, UpdatePartyMemberRequest request, CancellationToken cancellationToken)
        => _repository.UpdatePartyMemberAsync(accountId, gameStateId, memberId, request, cancellationToken);

    public Task<bool> AssignPartyMemberCharacterAsync(Guid accountId, Guid gameStateId, Guid memberId, Guid? characterId, CancellationToken cancellationToken)
        => _repository.AssignPartyMemberCharacterAsync(accountId, gameStateId, memberId, characterId, cancellationToken);

    public Task<bool> AssignCharacterToAccountAsync(Guid ownerAccountId, Guid gameStateId, Guid memberAccountId, Guid characterId, CancellationToken cancellationToken)
        => _repository.AssignCharacterToAccountAsync(ownerAccountId, gameStateId, memberAccountId, characterId, cancellationToken);

    public Task<bool> RemovePartyMemberAsync(Guid accountId, Guid gameStateId, Guid memberId, CancellationToken cancellationToken)
        => _repository.RemovePartyMemberAsync(accountId, gameStateId, memberId, cancellationToken);

    public Task<bool> LeavePartyAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => _repository.LeavePartyAsync(accountId, gameStateId, cancellationToken);
}
