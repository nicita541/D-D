using backend.Contracts.Rpg.Common;
using backend.Modules.Play;
using backend.Repositories.Rpg;

namespace backend.Modules.Play;

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
