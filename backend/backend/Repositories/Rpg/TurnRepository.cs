using System.Text.Json;
using backend.Contracts.Rpg.Turns;
using backend.Infrastructure.Database;
using Npgsql;
using NpgsqlTypes;

namespace backend.Repositories.Rpg;

public sealed class TurnRepository : ITurnRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public TurnRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PendingTurnCreationResult> CreatePendingTurnAsync(Guid accountId, Guid gameStateId, string playerMessage, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string lockGameStateSql = """
                SELECT turn_number
                FROM game.game_states
                WHERE id = @gameStateId
                  AND account_id = @accountId
                FOR UPDATE;
            """;

            await using var lockGameStateCommand = new NpgsqlCommand(lockGameStateSql, connection, transaction);
            lockGameStateCommand.Parameters.AddWithValue("accountId", accountId);
            lockGameStateCommand.Parameters.AddWithValue("gameStateId", gameStateId);
            var currentTurnValue = await lockGameStateCommand.ExecuteScalarAsync(cancellationToken);
            if (currentTurnValue is null or DBNull)
            {
                await transaction.RollbackAsync(cancellationToken);
                return PendingTurnCreationResult.NotFound();
            }

            const string activeTurnSql = """
                SELECT EXISTS (
                    SELECT 1
                    FROM game.game_turns
                    WHERE game_state_id = @gameStateId
                      AND account_id = @accountId
                      AND status IN ('pending', 'processing', 'running')
                );
            """;

            await using (var activeTurnCommand = new NpgsqlCommand(activeTurnSql, connection, transaction))
            {
                activeTurnCommand.Parameters.AddWithValue("accountId", accountId);
                activeTurnCommand.Parameters.AddWithValue("gameStateId", gameStateId);
                if (await activeTurnCommand.ExecuteScalarAsync(cancellationToken) is true)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return PendingTurnCreationResult.Conflict();
                }
            }

            const string nextTurnSql = """
                SELECT GREATEST(@currentTurnNumber, COALESCE(MAX(turn_number), 0)) + 1
                FROM game.game_turns
                WHERE game_state_id = @gameStateId;
            """;

            await using var nextTurnCommand = new NpgsqlCommand(nextTurnSql, connection, transaction);
            nextTurnCommand.Parameters.AddWithValue("currentTurnNumber", Convert.ToInt32(currentTurnValue));
            nextTurnCommand.Parameters.AddWithValue("gameStateId", gameStateId);
            var nextTurnValue = await nextTurnCommand.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException("Next turn number was not returned.");
            var turnNumber = Convert.ToInt32(nextTurnValue);

            const string insertSql = """
                INSERT INTO game.game_turns
                (
                    id,
                    game_state_id,
                    account_id,
                    turn_number,
                    player_message,
                    status
                )
                VALUES
                (
                    gen_random_uuid(),
                    @gameStateId,
                    @accountId,
                    @turnNumber,
                    @playerMessage,
                    'pending'
                )
                RETURNING id;
            """;

            await using var insertCommand = new NpgsqlCommand(insertSql, connection, transaction);
            insertCommand.Parameters.AddWithValue("gameStateId", gameStateId);
            insertCommand.Parameters.AddWithValue("accountId", accountId);
            insertCommand.Parameters.AddWithValue("turnNumber", turnNumber);
            insertCommand.Parameters.AddWithValue("playerMessage", playerMessage);

            var turnId = (Guid)(await insertCommand.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException("Turn id was not returned."));

            await transaction.CommitAsync(cancellationToken);
            return PendingTurnCreationResult.Created(new PendingTurn(turnId, gameStateId, accountId, turnNumber, playerMessage));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<JsonElement?> CompleteTurnAsync(
        PendingTurn turn,
        string masterAnswer,
        string rawAiResponse,
        string aiModel,
        IReadOnlyList<GameChangeProposal> changes,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string updateTurnSql = """
                UPDATE game.game_turns
                SET status = 'completed',
                    master_answer = @masterAnswer,
                    raw_ai_response = @rawAiResponse,
                    ai_model = @aiModel,
                    error_message = NULL,
                    completed_at = now()
                WHERE id = @turnId
                  AND game_state_id = @gameStateId
                  AND account_id = @accountId;
            """;

            await using (var command = new NpgsqlCommand(updateTurnSql, connection, transaction))
            {
                AddTurnIdentity(command, turn);
                command.Parameters.AddWithValue("masterAnswer", masterAnswer);
                command.Parameters.AddWithValue("aiModel", aiModel);
                AddJsonbParameter(command, "rawAiResponse", rawAiResponse);

                if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }
            }

            foreach (var change in changes)
            {
                await InsertChangeAsync(connection, transaction, turn, change, cancellationToken);
            }

            await UpdateGameStateTurnNumberAsync(connection, transaction, turn, cancellationToken);
            await InsertLogEntryAsync(connection, transaction, turn.GameStateId, turn.TurnNumber, "player", turn.PlayerMessage, cancellationToken);
            await InsertLogEntryAsync(connection, transaction, turn.GameStateId, turn.TurnNumber, "master", masterAnswer, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return await GetTurnAsync(turn.AccountId, turn.GameStateId, turn.Id, cancellationToken);
    }

    public async Task<JsonElement?> FailTurnAsync(
        PendingTurn turn,
        string errorMessage,
        string? rawAiResponse,
        string? aiModel,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string sql = """
                UPDATE game.game_turns
                SET status = 'failed',
                    error_message = @errorMessage,
                    raw_ai_response = @rawAiResponse,
                    ai_model = @aiModel,
                    completed_at = now()
                WHERE id = @turnId
                  AND game_state_id = @gameStateId
                  AND account_id = @accountId;
            """;

            await using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                AddTurnIdentity(command, turn);
                command.Parameters.AddWithValue("errorMessage", errorMessage);
                command.Parameters.AddWithValue("aiModel", string.IsNullOrWhiteSpace(aiModel) ? DBNull.Value : aiModel);
                AddJsonbParameter(command, "rawAiResponse", rawAiResponse);

                if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return await GetTurnAsync(turn.AccountId, turn.GameStateId, turn.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<JsonElement>?> GetTurnsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await GameStateExistsAsync(connection, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        const string sql = """
            SELECT jsonb_build_object(
                'id', t.id,
                'gameStateId', t.game_state_id,
                'turnNumber', t.turn_number,
                'status', t.status,
                'playerMessage', t.player_message,
                'masterAnswer', t.master_answer,
                'aiModel', t.ai_model,
                'errorMessage', t.error_message,
                'createdAt', t.created_at,
                'completedAt', t.completed_at
            )::text
            FROM game.game_turns t
            WHERE t.game_state_id = @gameStateId
              AND t.account_id = @accountId
            ORDER BY t.turn_number DESC, t.created_at DESC;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<JsonElement>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(RpgDbJson.ParseElement(reader.GetString(0)));
        }

        return result;
    }

    public async Task<JsonElement?> GetTurnAsync(Guid accountId, Guid gameStateId, Guid turnId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT jsonb_build_object(
                'id', t.id,
                'gameStateId', t.game_state_id,
                'turnNumber', t.turn_number,
                'status', t.status,
                'playerMessage', t.player_message,
                'masterAnswer', t.master_answer,
                'rawAiResponse', t.raw_ai_response,
                'aiModel', t.ai_model,
                'errorMessage', t.error_message,
                'createdAt', t.created_at,
                'completedAt', t.completed_at,
                'changes', COALESCE(
                    (
                        SELECT jsonb_agg(
                            jsonb_build_object(
                                'id', c.id,
                                'operation', c.operation,
                                'payload', c.payload,
                                'status', c.status,
                                'rejectReason', c.reject_reason,
                                'errorMessage', c.error_message,
                                'applyResult', c.apply_result,
                                'createdAt', c.created_at,
                                'processedAt', c.processed_at,
                                'processedByAccountId', c.processed_by_account_id
                            ) ORDER BY c.created_at, c.id
                        )
                        FROM game.game_changes c
                        WHERE c.game_turn_id = t.id
                          AND c.game_state_id = t.game_state_id
                    ),
                    '[]'::jsonb
                )
            )::text
            FROM game.game_turns t
            JOIN game.game_states gs ON gs.id = t.game_state_id
            WHERE t.id = @turnId
              AND t.game_state_id = @gameStateId
              AND gs.account_id = @accountId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("turnId", turnId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    private static async Task InsertChangeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        PendingTurn turn,
        GameChangeProposal change,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.game_changes
            (
                game_state_id,
                game_turn_id,
                operation,
                payload,
                status
            )
            VALUES
            (
                @gameStateId,
                @turnId,
                @operation,
                @payload,
                'pending'
            );
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", turn.GameStateId);
        command.Parameters.AddWithValue("turnId", turn.Id);
        command.Parameters.AddWithValue("operation", change.Operation);
        AddJsonbParameter(command, "payload", change.Payload.GetRawText());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertLogEntryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        int turnNumber,
        string type,
        string text,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.game_log_entries
            (
                game_state_id,
                turn_number,
                type,
                text,
                important
            )
            VALUES
            (
                @gameStateId,
                @turnNumber,
                @type,
                @text,
                false
            );
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("turnNumber", turnNumber);
        command.Parameters.AddWithValue("type", type);
        command.Parameters.AddWithValue("text", text);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpdateGameStateTurnNumberAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        PendingTurn turn,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE game.game_states
            SET turn_number = GREATEST(turn_number, @turnNumber),
                updated_at = now()
            WHERE id = @gameStateId
              AND account_id = @accountId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", turn.AccountId);
        command.Parameters.AddWithValue("gameStateId", turn.GameStateId);
        command.Parameters.AddWithValue("turnNumber", turn.TurnNumber);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> GameStateExistsAsync(NpgsqlConnection connection, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM game.game_states
                WHERE id = @gameStateId
                  AND account_id = @accountId
            );
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static void AddTurnIdentity(NpgsqlCommand command, PendingTurn turn)
    {
        command.Parameters.AddWithValue("accountId", turn.AccountId);
        command.Parameters.AddWithValue("gameStateId", turn.GameStateId);
        command.Parameters.AddWithValue("turnId", turn.Id);
    }

    private static void AddJsonbParameter(NpgsqlCommand command, string name, string? json)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Jsonb);
        parameter.Value = string.IsNullOrWhiteSpace(json) ? DBNull.Value : json;
    }
}
