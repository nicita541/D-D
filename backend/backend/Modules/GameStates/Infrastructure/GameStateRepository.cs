using System.Text.Json;
using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Modules.GameStates.Infrastructure;

public sealed class GameStateRepository : IGameStateRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public GameStateRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<JsonElement>> GetGameStatesAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT data::text
            FROM game.game_state_documents
            WHERE account_id = @accountId
            ORDER BY game_state_id;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<JsonElement>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(RpgDbJson.ParseElement(reader.GetString(0)));
        }

        return result;
    }

    public async Task<JsonElement?> GetGameStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT data::text
            FROM game.game_state_documents
            WHERE account_id = @accountId
              AND game_state_id = @gameStateId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<Guid?> CreateGameStateAsync(Guid accountId, string? name, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string accountSql = "SELECT EXISTS (SELECT 1 FROM auth.accounts WHERE id = @accountId);";
            await using (var accountCommand = new NpgsqlCommand(accountSql, connection, transaction))
            {
                accountCommand.Parameters.AddWithValue("accountId", accountId);
                var accountExists = (bool)(await accountCommand.ExecuteScalarAsync(cancellationToken) ?? false);
                if (!accountExists)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }
            }

            const string sql = "SELECT game.create_new_game(@accountId, @name);";
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("accountId", accountId);
            command.Parameters.AddWithValue("name", string.IsNullOrWhiteSpace(name) ? "Новая игра" : name.Trim());

            var gameStateId = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException("Game state id was not returned."));

            const string memorySql = """
                INSERT INTO game.campaign_memories (game_state_id)
                VALUES (@gameStateId)
                ON CONFLICT (game_state_id) DO NOTHING;
            """;

            await using var memoryCommand = new NpgsqlCommand(memorySql, connection, transaction);
            memoryCommand.Parameters.AddWithValue("gameStateId", gameStateId);
            await memoryCommand.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return gameStateId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> DeleteGameStateAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            DELETE FROM game.game_states
            WHERE id = @gameStateId
              AND account_id = @accountId;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
}
