using System.Text.Json;
using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Modules.Time.Infrastructure;

public sealed class TimeRepository : ITimeRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public TimeRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> GetTimeAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await EnsureTimeRowAsync(connection, null, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        return await SelectTimeAsync(connection, null, gameStateId, 0, string.Empty, 0, cancellationToken);
    }

    public async Task<JsonElement?> AdvanceTimeAsync(Guid accountId, Guid gameStateId, int minutes, string reason, bool tickConditions, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!await EnsureTimeRowAsync(connection, transaction, accountId, gameStateId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var newTotal = await AdvanceTimeInternalAsync(connection, transaction, gameStateId, minutes, cancellationToken);
            var expired = tickConditions
                ? await ExpireConditionsAsync(connection, transaction, gameStateId, newTotal, cancellationToken)
                : 0;

            var result = await SelectTimeAsync(connection, transaction, gameStateId, minutes, reason, expired, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    internal static async Task<bool> EnsureTimeRowAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.game_time (game_state_id)
            SELECT @gameStateId
            WHERE EXISTS (
                SELECT 1
                FROM game.game_states
                WHERE id = @gameStateId
                  AND account_id = @accountId
            )
            ON CONFLICT (game_state_id) DO NOTHING;

            SELECT EXISTS (
                SELECT 1
                FROM game.game_states
                WHERE id = @gameStateId
                  AND account_id = @accountId
            );
        """;

        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    internal static async Task<int> AdvanceTimeInternalAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, int minutes, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE game.game_time
            SET total_minutes = total_minutes + @minutes,
                current_day = ((total_minutes + @minutes) / 1440) + 1,
                current_hour = ((total_minutes + @minutes) % 1440) / 60,
                current_minute = (total_minutes + @minutes) % 60,
                updated_at = now()
            WHERE game_state_id = @gameStateId
            RETURNING total_minutes;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("minutes", minutes);
        return (int)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Game time row was not updated."));
    }

    internal static async Task<int> ExpireConditionsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, int totalMinutes, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE game.conditions
            SET is_active = false
            WHERE game_state_id = @gameStateId
              AND is_active = true
              AND expires_at_total_minutes IS NOT NULL
              AND expires_at_total_minutes <= @totalMinutes;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("totalMinutes", totalMinutes);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal static async Task<JsonElement?> SelectTimeAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid gameStateId, int advancedMinutes, string reason, int expiredConditions, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT jsonb_build_object(
                'gameStateId', game_state_id,
                'currentDay', current_day,
                'currentHour', current_hour,
                'currentMinute', current_minute,
                'totalMinutes', total_minutes,
                'advancedMinutes', @advancedMinutes,
                'reason', @reason,
                'expiredConditions', @expiredConditions,
                'updatedAt', updated_at
            )::text
            FROM game.game_time
            WHERE game_state_id = @gameStateId
            LIMIT 1;
        """;

        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("advancedMinutes", advancedMinutes);
        command.Parameters.AddWithValue("reason", string.IsNullOrWhiteSpace(reason) ? string.Empty : reason);
        command.Parameters.AddWithValue("expiredConditions", expiredConditions);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }
}
