using backend.Models;
using backend.Repositories;

namespace backend.Services;

public sealed class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _players;

    public PlayerService(IPlayerRepository players)
    {
        _players = players;
    }

    public Task<List<Player>> GetPlayersAsync(CancellationToken cancellationToken)
    {
        return _players.GetPlayersAsync(cancellationToken);
    }

    public Task<Player?> GetPlayerByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _players.GetPlayerByIdAsync(id, cancellationToken);
    }

    public Task<Guid> CreatePlayerAsync(Player player, CancellationToken cancellationToken)
    {
        return _players.CreatePlayerAsync(player, cancellationToken);
    }
}
