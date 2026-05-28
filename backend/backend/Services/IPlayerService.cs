using backend.Models;

namespace backend.Services;

public interface IPlayerService
{
    Task<List<Player>> GetPlayersAsync(CancellationToken cancellationToken);

    Task<Player?> GetPlayerByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Guid> CreatePlayerAsync(Player player, CancellationToken cancellationToken);
}
