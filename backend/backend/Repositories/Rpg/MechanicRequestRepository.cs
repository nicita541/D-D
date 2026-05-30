using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Contracts.Rpg.Mechanics;
using backend.Infrastructure.Database;
using Npgsql;
using NpgsqlTypes;

namespace backend.Repositories.Rpg;

public sealed class MechanicRequestRepository : IMechanicRequestRepository
{
    private static readonly HashSet<string> SupportedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "pending",
        "resolved",
        "cancelled",
        "failed"
    };

    private readonly IPostgresConnectionFactory _connectionFactory;

    public MechanicRequestRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<JsonElement>?> GetRequestsAsync(Guid accountId, Guid gameStateId, string? status, CancellationToken cancellationToken)
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();
        if (normalizedStatus is not null && !SupportedStatuses.Contains(normalizedStatus))
        {
            throw new RpgValidationException("Некорректный статус запроса механики.");
        }

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await GameStateExistsAsync(connection, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        const string sql = """
            SELECT jsonb_build_object(
                'id', mr.id,
                'gameStateId', mr.game_state_id,
                'turnId', mr.game_turn_id,
                'changeId', mr.game_change_id,
                'requestType', mr.request_type,
                'payload', mr.payload,
                'status', mr.status,
                'result', mr.result,
                'createdAt', mr.created_at,
                'resolvedAt', mr.resolved_at
            )::text
            FROM game.mechanic_requests mr
            WHERE mr.account_id = @accountId
              AND mr.game_state_id = @gameStateId
              AND (@status IS NULL OR mr.status = @status)
            ORDER BY mr.created_at DESC, mr.id DESC;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);

        var statusParameter = command.Parameters.Add("status", NpgsqlDbType.Text);
        statusParameter.Value = normalizedStatus is null ? DBNull.Value : normalizedStatus;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<JsonElement>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(RpgDbJson.ParseElement(reader.GetString(0)));
        }

        return result;
    }

    public async Task<MechanicRequestRecord?> GetPendingAbilityCheckRequestAsync(Guid accountId, Guid gameStateId, Guid requestId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT
                mr.id,
                mr.game_state_id,
                mr.game_turn_id,
                mr.game_change_id,
                mr.request_type,
                mr.payload::text,
                mr.status,
                mr.result::text,
                mr.created_at,
                mr.resolved_at
            FROM game.mechanic_requests mr
            JOIN game.game_states gs ON gs.id = mr.game_state_id
            WHERE gs.account_id = @accountId
              AND mr.account_id = @accountId
              AND mr.game_state_id = @gameStateId
              AND mr.id = @requestId
              AND mr.status = 'pending'
              AND mr.request_type = 'ability_check'
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("requestId", requestId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new MechanicRequestRecord(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.IsDBNull(2) ? null : reader.GetGuid(2),
            reader.IsDBNull(3) ? null : reader.GetGuid(3),
            reader.GetString(4),
            RpgDbJson.ParseElement(reader.GetString(5)),
            reader.GetString(6),
            RpgDbJson.ParseElement(reader.GetString(7)),
            reader.GetFieldValue<DateTimeOffset>(8),
            reader.IsDBNull(9) ? null : reader.GetFieldValue<DateTimeOffset>(9));
    }

    public async Task<JsonElement?> ResolveAsync(Guid accountId, Guid gameStateId, Guid requestId, JsonElement result, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE game.mechanic_requests mr
            SET status = 'resolved',
                result = @result,
                resolved_at = now()
            FROM game.game_states gs
            WHERE gs.id = mr.game_state_id
              AND gs.account_id = @accountId
              AND mr.account_id = @accountId
              AND mr.game_state_id = @gameStateId
              AND mr.id = @requestId
              AND mr.status = 'pending'
            RETURNING jsonb_build_object(
                'id', mr.id,
                'gameStateId', mr.game_state_id,
                'turnId', mr.game_turn_id,
                'changeId', mr.game_change_id,
                'requestType', mr.request_type,
                'payload', mr.payload,
                'status', mr.status,
                'result', mr.result,
                'createdAt', mr.created_at,
                'resolvedAt', mr.resolved_at
            )::text;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("requestId", requestId);
        var parameter = command.Parameters.Add("result", NpgsqlDbType.Jsonb);
        parameter.Value = result.GetRawText();
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    private static async Task<bool> GameStateExistsAsync(NpgsqlConnection connection, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM game.game_states WHERE id = @gameStateId AND account_id = @accountId);";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }
}
