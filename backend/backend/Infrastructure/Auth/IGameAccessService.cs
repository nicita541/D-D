namespace backend.Infrastructure.Auth;

public interface IGameAccessService
{
    Task<GameAccess?> GetAccessAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken);
}
