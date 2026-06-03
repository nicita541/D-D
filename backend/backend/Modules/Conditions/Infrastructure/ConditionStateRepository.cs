using System.Text.Json;
using backend.Infrastructure.Database;
using backend.Modules.Time.Infrastructure;
using Npgsql;

namespace backend.Modules.Conditions.Infrastructure;

public sealed class ConditionStateRepository : IConditionStateRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public ConditionStateRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> TickConditionsAsync(Guid accountId, Guid gameStateId, Guid characterId, int turns, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!await TimeRepository.EnsureTimeRowAsync(connection, transaction, accountId, gameStateId, cancellationToken)
                || !await CharacterExistsAsync(connection, transaction, accountId, gameStateId, characterId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var totalMinutes = await GetTotalMinutesAsync(connection, transaction, gameStateId, cancellationToken);
            var decremented = await DecrementConditionTurnsAsync(connection, transaction, gameStateId, characterId, turns, cancellationToken);
            var expired = await ExpireCharacterConditionsAsync(connection, transaction, gameStateId, characterId, totalMinutes, cancellationToken);
            var active = await SelectActiveConditionsAsync(connection, transaction, gameStateId, characterId, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return JsonSerializer.SerializeToElement(new
            {
                gameStateId,
                characterId,
                turns,
                decrementedConditions = decremented,
                expiredConditions = expired,
                activeConditions = active
            });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<JsonElement?> KnockoutAsync(Guid accountId, Guid gameStateId, Guid characterId, string reason, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!await CharacterExistsAsync(connection, transaction, accountId, gameStateId, characterId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            const string sql = """
                UPDATE game.player_resources
                SET hp_current = 0,
                    unconscious = true
                WHERE game_state_id = @gameStateId
                  AND player_id = @characterId;
            """;

            await using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("gameStateId", gameStateId);
                command.Parameters.AddWithValue("characterId", characterId);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await EnsureConditionAsync(connection, transaction, gameStateId, characterId, "повержен", "control", string.IsNullOrWhiteSpace(reason) ? "Персонаж выведен из строя." : reason, "knockout", cancellationToken);
            var result = await SelectCharacterStateAsync(connection, transaction, gameStateId, characterId, "knockout", reason, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<JsonElement?> ReviveAsync(Guid accountId, Guid gameStateId, Guid characterId, int hp, bool clearDead, string reason, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!await CharacterExistsAsync(connection, transaction, accountId, gameStateId, characterId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            const string sql = """
                UPDATE game.player_resources
                SET hp_current = CASE
                        WHEN dead AND @clearDead = false THEN hp_current
                        ELSE GREATEST(hp_current, @hp)
                    END,
                    unconscious = CASE
                        WHEN dead AND @clearDead = false THEN unconscious
                        ELSE false
                    END,
                    dead = CASE
                        WHEN @clearDead THEN false
                        ELSE dead
                    END,
                    death_saves_successes = 0,
                    death_saves_failures = 0
                WHERE game_state_id = @gameStateId
                  AND player_id = @characterId;
            """;

            await using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("gameStateId", gameStateId);
                command.Parameters.AddWithValue("characterId", characterId);
                command.Parameters.AddWithValue("hp", hp);
                command.Parameters.AddWithValue("clearDead", clearDead);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await InactivateConditionsByNameAsync(connection, transaction, gameStateId, characterId, ["повержен", "мертв"], cancellationToken);
            var result = await SelectCharacterStateAsync(connection, transaction, gameStateId, characterId, "revive", reason, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<JsonElement>?> GetActiveConditionsAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await GameStateExistsAsync(connection, null, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        return await SelectActiveConditionsAsync(connection, null, gameStateId, null, cancellationToken);
    }

    public async Task<IReadOnlyList<JsonElement>?> GetCharacterStatesAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await GameStateExistsAsync(connection, null, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        const string sql = """
            SELECT jsonb_build_object(
                'characterId', p.id,
                'name', p.name,
                'hpCurrent', COALESCE(pr.hp_current, 0),
                'hpMax', COALESCE(pr.hp_max, 0),
                'manaCurrent', COALESCE(pr.mana_current, 0),
                'manaMax', COALESCE(pr.mana_max, 0),
                'actionPointsCurrent', COALESCE(pr.action_points_current, 0),
                'actionPointsMax', COALESCE(pr.action_points_max, 0),
                'unconscious', COALESCE(pr.unconscious, false),
                'dead', COALESCE(pr.dead, false),
                'deathSavesSuccesses', COALESCE(pr.death_saves_successes, 0),
                'deathSavesFailures', COALESCE(pr.death_saves_failures, 0)
            )::text
            FROM game.players p
            JOIN game.game_states gs ON gs.id = p.game_state_id
            LEFT JOIN game.player_resources pr ON pr.player_id = p.id AND pr.game_state_id = p.game_state_id
            WHERE gs.account_id = @accountId
              AND p.game_state_id = @gameStateId
            ORDER BY p.name, p.id;
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

    private static async Task<int> GetTotalMinutesAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT total_minutes FROM game.game_time WHERE game_state_id = @gameStateId LIMIT 1;";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
    }

    private static async Task<int> DecrementConditionTurnsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid characterId, int turns, CancellationToken cancellationToken)
    {
        if (turns <= 0)
        {
            return 0;
        }

        const string sql = """
            UPDATE game.conditions
            SET remaining_turns = GREATEST(0, remaining_turns - @turns)
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
              AND is_active = true
              AND is_permanent = false
              AND remaining_turns IS NOT NULL;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("turns", turns);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> ExpireCharacterConditionsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid characterId, int totalMinutes, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE game.conditions
            SET is_active = false
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
              AND is_active = true
              AND is_permanent = false
              AND (
                  (remaining_turns IS NOT NULL AND remaining_turns <= 0)
                  OR (expires_at_total_minutes IS NOT NULL AND expires_at_total_minutes <= @totalMinutes)
              );
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("totalMinutes", totalMinutes);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureConditionAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid characterId, string name, string type, string description, string source, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.conditions (game_state_id, player_id, name, type, description, source, is_active)
            SELECT @gameStateId, @characterId, @name, @type, @description, @source, true
            WHERE NOT EXISTS (
                SELECT 1
                FROM game.conditions
                WHERE game_state_id = @gameStateId
                  AND player_id = @characterId
                  AND lower(name) = lower(@name)
                  AND is_active = true
            );
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("type", type);
        command.Parameters.AddWithValue("description", description);
        command.Parameters.AddWithValue("source", source);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InactivateConditionsByNameAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid characterId, IReadOnlyList<string> names, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE game.conditions
            SET is_active = false
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
              AND is_active = true
              AND lower(name) = ANY(@names);
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("names", names.Select(name => name.ToLowerInvariant()).ToArray());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<JsonElement?> SelectCharacterStateAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid characterId, string operation, string reason, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT jsonb_build_object(
                'operation', @operation,
                'reason', @reason,
                'gameStateId', pr.game_state_id,
                'characterId', pr.player_id,
                'hpCurrent', pr.hp_current,
                'hpMax', pr.hp_max,
                'unconscious', pr.unconscious,
                'dead', pr.dead,
                'deathSavesSuccesses', pr.death_saves_successes,
                'deathSavesFailures', pr.death_saves_failures,
                'activeConditions', COALESCE((
                    SELECT jsonb_agg(jsonb_build_object(
                        'id', c.id,
                        'name', c.name,
                        'type', c.type,
                        'remainingTurns', c.remaining_turns,
                        'durationMinutes', c.duration_minutes,
                        'expiresAtTotalMinutes', c.expires_at_total_minutes,
                        'source', c.source
                    ) ORDER BY c.created_at, c.id)
                    FROM game.conditions c
                    WHERE c.game_state_id = pr.game_state_id
                      AND c.player_id = pr.player_id
                      AND c.is_active = true
                ), '[]'::jsonb)
            )::text
            FROM game.player_resources pr
            WHERE pr.game_state_id = @gameStateId
              AND pr.player_id = @characterId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("operation", operation);
        command.Parameters.AddWithValue("reason", string.IsNullOrWhiteSpace(reason) ? string.Empty : reason);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    private static async Task<IReadOnlyList<JsonElement>> SelectActiveConditionsAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid gameStateId, Guid? characterId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT jsonb_build_object(
                'id', c.id,
                'gameStateId', c.game_state_id,
                'characterId', c.player_id,
                'name', c.name,
                'type', c.type,
                'description', c.description,
                'source', c.source,
                'remainingTurns', c.remaining_turns,
                'durationMinutes', c.duration_minutes,
                'expiresAtTotalMinutes', c.expires_at_total_minutes,
                'isActive', c.is_active,
                'effects', c.effects,
                'tags', c.tags,
                'createdAt', c.created_at
            )::text
            FROM game.conditions c
            WHERE c.game_state_id = @gameStateId
              AND c.is_active = true
              AND (@characterId::uuid IS NULL OR c.player_id = @characterId)
            ORDER BY c.created_at, c.id;
        """;

        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId.HasValue ? characterId.Value : DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<JsonElement>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(RpgDbJson.ParseElement(reader.GetString(0)));
        }

        return result;
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

    private static async Task<bool> GameStateExistsAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM game.game_states WHERE account_id = @accountId AND id = @gameStateId);";
        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }
}
