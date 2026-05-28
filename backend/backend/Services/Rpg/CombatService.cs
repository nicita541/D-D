using System.Text.Json;
using backend.Contracts.Rpg.Combat;
using backend.Repositories.Rpg;

namespace backend.Services.Rpg;

public sealed class CombatService : ICombatService
{
    private readonly ICombatRepository _repository;

    public CombatService(ICombatRepository repository)
    {
        _repository = repository;
    }

    public Task<JsonElement?> GetCombatStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => _repository.GetCombatStateAsync(accountId, gameStateId, cancellationToken);

    public Task<Guid?> StartCombatAsync(Guid accountId, Guid gameStateId, StartCombatRequest request, CancellationToken cancellationToken)
        => _repository.StartCombatAsync(accountId, gameStateId, request, cancellationToken);

    public Task<bool> EndCombatAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
        => _repository.EndCombatAsync(accountId, gameStateId, cancellationToken);

    public Task<Guid?> AddParticipantAsync(Guid accountId, Guid gameStateId, AddCombatParticipantRequest request, CancellationToken cancellationToken)
        => _repository.AddParticipantAsync(accountId, gameStateId, request, cancellationToken);
}
