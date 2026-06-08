using backend.Modules.GameSessions.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.GameSessions.Application;

public interface IGameSessionService
{
    Task<RpgResult<StartGameSessionResponse>> StartAsync(Guid accountId, StartGameSessionRequest request, CancellationToken cancellationToken);
}
