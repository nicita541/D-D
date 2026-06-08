using backend.Modules.GameSessions.Contracts;
using backend.Shared.Kernel;

namespace backend.Modules.GameSessions.Infrastructure;

public interface IGameSessionRepository
{
    Task<RpgResult<StartGameSessionResponse>> StartSoloSessionAsync(Guid accountId, StartGameSessionRequest request, CancellationToken cancellationToken);
}
