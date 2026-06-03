using System.Text.Json;
using backend.Infrastructure.Database;
using backend.Modules.Time.Infrastructure;
using Npgsql;

namespace backend.Modules.Rest.Infrastructure;

public sealed class RestRepository : IRestRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public RestRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> RestAsync(Guid accountId, Guid gameStateId, Guid? characterId, bool isLongRest, int minutes, string reason, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!await TimeRepository.EnsureTimeRowAsync(connection, transaction, accountId, gameStateId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            if (characterId.HasValue && !await CharacterExistsAsync(connection, transaction, accountId, gameStateId, characterId.Value, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var affectedCharacters = isLongRest
                ? await ApplyLongRestAsync(connection, transaction, accountId, gameStateId, characterId, cancellationToken)
                : await ApplyShortRestAsync(connection, transaction, accountId, gameStateId, characterId, cancellationToken);

            var restoredResources = await RestoreLimitedResourcesAsync(connection, transaction, accountId, gameStateId, characterId, isLongRest, cancellationToken);
            var newTotal = await TimeRepository.AdvanceTimeInternalAsync(connection, transaction, gameStateId, minutes, cancellationToken);
            var expiredConditions = await TimeRepository.ExpireConditionsAsync(connection, transaction, gameStateId, newTotal, cancellationToken);
            var time = await TimeRepository.SelectTimeAsync(connection, transaction, gameStateId, minutes, reason, expiredConditions, cancellationToken);

            var result = JsonSerializer.SerializeToElement(new
            {
                type = isLongRest ? "long_rest" : "short_rest",
                gameStateId,
                characterId,
                affectedCharacters,
                restoredResources,
                expiredConditions,
                minutes,
                reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason,
                time
            });

            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<int> ApplyShortRestAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid accountId, Guid gameStateId, Guid? characterId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE game.player_resources pr
            SET hp_current = CASE
                    WHEN pr.dead THEN pr.hp_current
                    ELSE LEAST(pr.hp_max, pr.hp_current + GREATEST(1, CEIL(pr.hp_max * 0.25)::int))
                END,
                action_points_current = action_points_max,
                unconscious = CASE
                    WHEN pr.dead THEN pr.unconscious
                    ELSE false
                END
            FROM game.players p
            JOIN game.game_states gs ON gs.id = p.game_state_id
            WHERE pr.player_id = p.id
              AND pr.game_state_id = p.game_state_id
              AND gs.account_id = @accountId
              AND p.game_state_id = @gameStateId
              AND (@characterId::uuid IS NULL OR p.id = @characterId);
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId.HasValue ? characterId.Value : DBNull.Value);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> ApplyLongRestAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid accountId, Guid gameStateId, Guid? characterId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE game.player_resources pr
            SET hp_current = CASE WHEN pr.dead THEN pr.hp_current ELSE pr.hp_max END,
                mana_current = mana_max,
                action_points_current = action_points_max,
                unconscious = CASE WHEN pr.dead THEN pr.unconscious ELSE false END,
                death_saves_successes = 0,
                death_saves_failures = 0
            FROM game.players p
            JOIN game.game_states gs ON gs.id = p.game_state_id
            WHERE pr.player_id = p.id
              AND pr.game_state_id = p.game_state_id
              AND gs.account_id = @accountId
              AND p.game_state_id = @gameStateId
              AND (@characterId::uuid IS NULL OR p.id = @characterId);
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId.HasValue ? characterId.Value : DBNull.Value);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> RestoreLimitedResourcesAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid accountId, Guid gameStateId, Guid? characterId, bool isLongRest, CancellationToken cancellationToken)
    {
        var sql = isLongRest
            ? """
                UPDATE game.limited_resources lr
                SET current_value = max_value
                FROM game.players p
                JOIN game.game_states gs ON gs.id = p.game_state_id
                WHERE lr.player_id = p.id
                  AND lr.game_state_id = p.game_state_id
                  AND gs.account_id = @accountId
                  AND p.game_state_id = @gameStateId
                  AND (@characterId::uuid IS NULL OR p.id = @characterId);
            """
            : """
                UPDATE game.limited_resources lr
                SET current_value = max_value
                FROM game.players p
                JOIN game.game_states gs ON gs.id = p.game_state_id
                WHERE lr.player_id = p.id
                  AND lr.game_state_id = p.game_state_id
                  AND gs.account_id = @accountId
                  AND p.game_state_id = @gameStateId
                  AND (@characterId::uuid IS NULL OR p.id = @characterId)
                  AND lower(lr.recovery) IN ('short_rest', 'short', 'rest');
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId.HasValue ? characterId.Value : DBNull.Value);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> CharacterExistsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM game.players p
                JOIN game.game_states gs ON gs.id = p.game_state_id
                WHERE gs.account_id = @accountId
                  AND p.game_state_id = @gameStateId
                  AND p.id = @characterId
            );
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }
}
