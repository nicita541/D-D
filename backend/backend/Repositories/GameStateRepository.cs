using System.Text.Json;
using backend.Contracts.GameStates;
using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Repositories;

public sealed class GameStateRepository : IGameStateRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public GameStateRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<JsonElement>> GetGameStatesAsync(CancellationToken cancellationToken)
    {
        var documents = new List<JsonElement>();

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT data::text
            FROM game.game_state_documents
            ORDER BY data->>'id';
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var json = reader.GetString(0);
            documents.Add(JsonElementParser.Parse(json));
        }

        return documents;
    }

    public async Task<JsonElement?> GetGameStateAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT data::text
            FROM game.game_state_documents
            WHERE game_state_id = @id
            LIMIT 1;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result == null || result == DBNull.Value)
        {
            return null;
        }

        return JsonElementParser.Parse(result.ToString()!);
    }

    public async Task<JsonElement?> GetGameStatePlayerAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT (data->'игрок')::text
            FROM game.game_state_documents
            WHERE game_state_id = @id
            LIMIT 1;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result == null || result == DBNull.Value)
        {
            return null;
        }

        return JsonElementParser.Parse(result.ToString()!);
    }

    public async Task<CreateGameStateResult> CreateGameStateAsync(
        string saveName,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var accountId = await DevelopmentAccountHelper.EnsureDevelopmentAccountAsync(
                connection,
                transaction,
                cancellationToken);
            var gameStateId = await GameStateDatabaseHelper.CreateNewGameAsync(
                connection,
                transaction,
                accountId,
                saveName,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new CreateGameStateResult
            {
                Id = gameStateId,
                AccountId = accountId,
                SaveName = saveName
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> DeleteGameStateAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE game.game_states
            SET is_active = false,
                updated_at = now()
            WHERE id = @id
              AND is_active = true
            RETURNING id;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null && result != DBNull.Value;
    }
}
