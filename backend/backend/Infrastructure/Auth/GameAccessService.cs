using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Infrastructure.Auth;

public sealed class GameAccessService : IGameAccessService
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public GameAccessService(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<GameAccess?> GetAccessAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                gs.account_id,
                pm.id,
                pm.character_id,
                pm.role
            FROM game.game_states gs
            LEFT JOIN LATERAL (
                SELECT id, character_id, role
                FROM game.party_members
                WHERE game_state_id = gs.id
                  AND account_id = @accountId
                  AND status = 'active'
                ORDER BY
                    CASE role
                        WHEN 'host' THEN 0
                        WHEN 'gm' THEN 1
                        WHEN 'player' THEN 2
                        WHEN 'observer' THEN 3
                        ELSE 4
                    END,
                    created_at,
                    id
                LIMIT 1
            ) pm ON true
            WHERE gs.id = @gameStateId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var ownerAccountId = reader.GetGuid(0);
        var partyMemberId = reader.IsDBNull(1) ? (Guid?)null : reader.GetGuid(1);
        var characterId = reader.IsDBNull(2) ? (Guid?)null : reader.GetGuid(2);
        var role = ownerAccountId == accountId
            ? GameAccessRoles.Host
            : partyMemberId.HasValue
                ? reader.GetString(3)
                : GameAccessRoles.None;

        return new GameAccess(
            accountId,
            gameStateId,
            ownerAccountId,
            partyMemberId,
            characterId,
            role);
    }
}
