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

    public Task<JsonElement?> GetCombatStateAsync(Guid gameStateId, CancellationToken cancellationToken)
        => _repository.GetCombatStateAsync(gameStateId, cancellationToken);

    public Task<Guid> StartCombatAsync(Guid gameStateId, StartCombatRequest request, CancellationToken cancellationToken)
        => _repository.StartCombatAsync(gameStateId, request, cancellationToken);

    public Task<bool> EndCombatAsync(Guid gameStateId, CancellationToken cancellationToken)
        => _repository.EndCombatAsync(gameStateId, cancellationToken);

    public Task<Guid> AddParticipantAsync(Guid gameStateId, AddCombatParticipantRequest request, CancellationToken cancellationToken)
        => _repository.AddParticipantAsync(gameStateId, request, cancellationToken);
}
