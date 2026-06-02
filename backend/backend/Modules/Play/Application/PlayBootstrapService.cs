using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Modules.Play.Application;
using backend.Modules.Play.Contracts;
using backend.Modules.Play.Infrastructure;
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

namespace backend.Modules.Play.Application;

public sealed class PlayBootstrapService : IPlayBootstrapService
{
    private readonly IPlayBootstrapRepository _repository;

    public PlayBootstrapService(IPlayBootstrapRepository repository)
    {
        _repository = repository;
    }

    public async Task<RpgResult<PlayBootstrapResponse>> BootstrapAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        var result = await _repository.BootstrapAsync(accountId, gameStateId, cancellationToken);
        return result.Status switch
        {
            PlayBootstrapRepositoryStatus.Ok when result.Response is not null => RpgResult<PlayBootstrapResponse>.Ok(result.Response),
            PlayBootstrapRepositoryStatus.NotFound => RpgResult<PlayBootstrapResponse>.NotFound("GameState не найден."),
            PlayBootstrapRepositoryStatus.NoCharacters => RpgResult<PlayBootstrapResponse>.BadRequest("Перед bootstrap нужно создать хотя бы одного персонажа."),
            PlayBootstrapRepositoryStatus.AlreadyBootstrapped => RpgResult<PlayBootstrapResponse>.Conflict("Game already bootstrapped."),
            _ => RpgResult<PlayBootstrapResponse>.BadRequest("Не удалось выполнить bootstrap.")
        };
    }
}
