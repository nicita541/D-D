using backend.Modules.GameSessions.Contracts;
using backend.Modules.GameSessions.Infrastructure;
using backend.Shared.Kernel;

namespace backend.Modules.GameSessions.Application;

public sealed class GameSessionService : IGameSessionService
{
    private readonly IGameSessionRepository _repository;

    public GameSessionService(IGameSessionRepository repository)
    {
        _repository = repository;
    }

    public Task<RpgResult<StartGameSessionResponse>> StartAsync(Guid accountId, StartGameSessionRequest request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.ResolvedMode, "solo", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(RpgResult<StartGameSessionResponse>.BadRequest("На этом экране доступен только Solo. Party останется отдельным co-op потоком."));
        }

        if (request.AccountCharacterId == Guid.Empty)
        {
            return Task.FromResult(RpgResult<StartGameSessionResponse>.BadRequest("Нужно выбрать героя."));
        }

        if (request.CampaignTemplateId == Guid.Empty)
        {
            return Task.FromResult(RpgResult<StartGameSessionResponse>.BadRequest("Нужно выбрать историю."));
        }

        return _repository.StartSoloSessionAsync(accountId, request, cancellationToken);
    }
}
