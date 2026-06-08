using System.Text.Json;
using System.Text.Json.Nodes;
using backend.Infrastructure.Database;
using backend.Modules.Snapshots.Contracts;
using backend.Shared.Kernel;
using Npgsql;
using NpgsqlTypes;

namespace backend.Modules.Snapshots.Infrastructure;

public sealed class SnapshotRepository : ISnapshotRepository
{
    private static readonly string[] SnapshotTables =
    [
        "story_states",
        "campaign_memories",
        "game_time",
        "locations",
        "factions",
        "quests",
        "monsters",
        "players",
        "player_progression",
        "player_resources",
        "conditions",
        "limited_resources",
        "player_attributes",
        "player_proficiencies",
        "abilities",
        "wealth",
        "player_needs",
        "item_instances",
        "item_weapon_stats",
        "item_armor_stats",
        "item_consumable_stats",
        "equipped_gear",
        "combat_stats",
        "attacks",
        "location_exits",
        "world_objects",
        "world_containers",
        "npcs",
        "quest_steps",
        "quest_reward_items",
        "game_log_entries",
        "game_turns",
        "game_changes",
        "combat_states",
        "combat_participants",
        "dice_rolls",
        "skill_checks",
        "mechanic_requests",
        "loot_containers",
        "loot_items",
        "quest_rewards"
    ];

    private readonly IPostgresConnectionFactory _connectionFactory;

    public SnapshotRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RpgResult<IReadOnlyList<SnapshotDto>>> ListSnapshotsAsync(Guid ownerAccountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT s.id, s.game_state_id, s.reason, s.created_by_account_id, s.created_at, s.restored_by_account_id, s.restored_at
            FROM game.snapshots s
            JOIN game.game_states gs ON gs.id = s.game_state_id
            WHERE gs.account_id = @ownerAccountId
              AND s.game_state_id = @gameStateId
            ORDER BY s.created_at DESC, s.id DESC;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("ownerAccountId", ownerAccountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var snapshots = new List<SnapshotDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            snapshots.Add(ReadSnapshot(reader));
        }

        return RpgResult<IReadOnlyList<SnapshotDto>>.Ok(snapshots);
    }

    public async Task<RpgResult<SnapshotDto>> CreateSnapshotAsync(
        Guid ownerAccountId,
        Guid gameStateId,
        Guid createdByAccountId,
        string reason,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!await GameExistsAsync(connection, transaction, ownerAccountId, gameStateId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return RpgResult<SnapshotDto>.NotFound("GameState не найден.");
            }

            var payload = await BuildPayloadAsync(connection, transaction, gameStateId, cancellationToken);
            const string insert = """
                INSERT INTO game.snapshots (game_state_id, created_by_account_id, reason, payload)
                VALUES (@gameStateId, @createdByAccountId, @reason, @payload::jsonb)
                RETURNING id, game_state_id, reason, created_by_account_id, created_at, restored_by_account_id, restored_at;
            """;

            SnapshotDto snapshot;
            await using (var command = new NpgsqlCommand(insert, connection, transaction))
            {
                command.Parameters.AddWithValue("gameStateId", gameStateId);
                command.Parameters.AddWithValue("createdByAccountId", createdByAccountId);
                command.Parameters.AddWithValue("reason", reason);
                command.Parameters.AddWithValue("payload", payload);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                return RpgResult<SnapshotDto>.ServiceUnavailable(null, "Snapshot не создан.");
                }

                snapshot = ReadSnapshot(reader);
            }
            await transaction.CommitAsync(cancellationToken);
            return RpgResult<SnapshotDto>.Ok(snapshot);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RpgResult<SnapshotRestoreResponse>> RestoreSnapshotAsync(
        Guid ownerAccountId,
        Guid gameStateId,
        Guid snapshotId,
        Guid restoredByAccountId,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var payload = await GetSnapshotPayloadAsync(connection, transaction, ownerAccountId, gameStateId, snapshotId, cancellationToken);
            if (payload is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return RpgResult<SnapshotRestoreResponse>.NotFound("Snapshot не найден.");
            }

            await ClearCurrentLocationAsync(connection, transaction, gameStateId, cancellationToken);
            foreach (var table in SnapshotTables.Reverse())
            {
                await DeleteTableRowsAsync(connection, transaction, table, gameStateId, cancellationToken);
            }

            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            var tables = root.TryGetProperty("tables", out var tablesElement)
                ? tablesElement
                : default;

            foreach (var table in SnapshotTables)
            {
                if (tables.ValueKind != JsonValueKind.Object
                    || !tables.TryGetProperty(table, out var rows)
                    || rows.ValueKind != JsonValueKind.Array
                    || rows.GetArrayLength() == 0)
                {
                    continue;
                }

                await InsertRowsAsync(connection, transaction, table, rows.GetRawText(), cancellationToken);
            }

            await RestoreGameStateAsync(connection, transaction, gameStateId, root, cancellationToken);
            await RestoreCombatCurrentTurnAsync(connection, transaction, tables, cancellationToken);
            await MarkRestoredAsync(connection, transaction, snapshotId, restoredByAccountId, cancellationToken);
            await InsertRestoreLogAsync(connection, transaction, gameStateId, snapshotId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return RpgResult<SnapshotRestoreResponse>.Ok(new SnapshotRestoreResponse(snapshotId, gameStateId, "Snapshot восстановлен."));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<bool> GameExistsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid ownerAccountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM game.game_states WHERE id = @gameStateId AND account_id = @ownerAccountId);";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("ownerAccountId", ownerAccountId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task<string?> GetSnapshotPayloadAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid ownerAccountId,
        Guid gameStateId,
        Guid snapshotId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.payload::text
            FROM game.snapshots s
            JOIN game.game_states gs ON gs.id = s.game_state_id
            WHERE gs.account_id = @ownerAccountId
              AND s.game_state_id = @gameStateId
              AND s.id = @snapshotId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("ownerAccountId", ownerAccountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("snapshotId", snapshotId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : value.ToString();
    }

    private static async Task<string> BuildPayloadAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, CancellationToken cancellationToken)
    {
        var payload = new JsonObject
        {
            ["gameState"] = JsonNode.Parse(await ReadGameStateAsync(connection, transaction, gameStateId, cancellationToken))
        };
        var tables = new JsonObject();
        foreach (var table in SnapshotTables)
        {
            tables[table] = JsonNode.Parse(await ReadRowsAsync(connection, transaction, table, gameStateId, cancellationToken));
        }

        payload["tables"] = tables;
        return payload.ToJsonString();
    }

    private static async Task<string> ReadGameStateAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT to_jsonb(gs)::text FROM game.game_states gs WHERE gs.id = @gameStateId;";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return (await command.ExecuteScalarAsync(cancellationToken))?.ToString() ?? "{}";
    }

    private static async Task<string> ReadRowsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string table, Guid gameStateId, CancellationToken cancellationToken)
    {
        var sql = $"SELECT COALESCE(jsonb_agg(to_jsonb(t)), '[]'::jsonb)::text FROM (SELECT * FROM game.{table} WHERE game_state_id = @gameStateId) t;";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return (await command.ExecuteScalarAsync(cancellationToken))?.ToString() ?? "[]";
    }

    private static async Task ClearCurrentLocationAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE game.game_states SET current_location_id = NULL WHERE id = @gameStateId;";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DeleteTableRowsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string table, Guid gameStateId, CancellationToken cancellationToken)
    {
        var sql = $"DELETE FROM game.{table} WHERE game_state_id = @gameStateId;";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertRowsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string table, string rowsJson, CancellationToken cancellationToken)
    {
        var rowsExpression = table == "combat_states"
            ? "(SELECT COALESCE(jsonb_agg(value || jsonb_build_object('current_turn_participant_id', NULL)), '[]'::jsonb) FROM jsonb_array_elements(@rows::jsonb))"
            : "@rows::jsonb";
        var sql = $"INSERT INTO game.{table} SELECT * FROM jsonb_populate_recordset(NULL::game.{table}, {rowsExpression});";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add("rows", NpgsqlDbType.Jsonb).Value = rowsJson;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task RestoreGameStateAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        if (!payload.TryGetProperty("gameState", out var gameState) || gameState.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var sql = """
            UPDATE game.game_states
            SET name = COALESCE(@name, name),
                schema_version = COALESCE(@schemaVersion, schema_version),
                turn_number = COALESCE(@turnNumber, turn_number),
                mode = COALESCE(@mode, mode),
                is_active = COALESCE(@isActive, is_active),
                current_location_id = CASE
                    WHEN @currentLocationId IS NULL THEN NULL
                    WHEN EXISTS (SELECT 1 FROM game.locations WHERE id = @currentLocationId AND game_state_id = @gameStateId) THEN @currentLocationId
                    ELSE NULL
                END,
                updated_at = now()
            WHERE id = @gameStateId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("name", DbString(gameState, "name"));
        command.Parameters.AddWithValue("schemaVersion", DbString(gameState, "schema_version"));
        command.Parameters.AddWithValue("mode", DbString(gameState, "mode"));
        command.Parameters.AddWithValue("turnNumber", TryGetInt(gameState, "turn_number") is { } turnNumber ? turnNumber : DBNull.Value);
        command.Parameters.AddWithValue("isActive", TryGetBool(gameState, "is_active") is { } isActive ? isActive : DBNull.Value);
        command.Parameters.AddNullableUuid("currentLocationId", TryGetGuid(gameState, "current_location_id"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task RestoreCombatCurrentTurnAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, JsonElement tables, CancellationToken cancellationToken)
    {
        if (tables.ValueKind != JsonValueKind.Object
            || !tables.TryGetProperty("combat_states", out var combatStates)
            || combatStates.ValueKind != JsonValueKind.Array
            || combatStates.GetArrayLength() == 0)
        {
            return;
        }

        const string sql = """
            UPDATE game.combat_states c
            SET current_turn_participant_id = (row.value ->> 'current_turn_participant_id')::uuid
            FROM jsonb_array_elements(@rows::jsonb) AS row(value)
            WHERE c.id = (row.value ->> 'id')::uuid
              AND row.value ->> 'current_turn_participant_id' IS NOT NULL
              AND EXISTS (
                  SELECT 1
                  FROM game.combat_participants p
                  WHERE p.id = (row.value ->> 'current_turn_participant_id')::uuid
              );
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add("rows", NpgsqlDbType.Jsonb).Value = combatStates.GetRawText();
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkRestoredAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid snapshotId, Guid restoredByAccountId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE game.snapshots
            SET restored_at = now(),
                restored_by_account_id = @restoredByAccountId
            WHERE id = @snapshotId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("snapshotId", snapshotId);
        command.Parameters.AddWithValue("restoredByAccountId", restoredByAccountId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertRestoreLogAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid snapshotId, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.game_log_entries (game_state_id, turn_number, type, text, important)
            SELECT id, turn_number, 'system', @text, true
            FROM game.game_states
            WHERE id = @gameStateId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("text", $"Восстановлен snapshot {snapshotId}.");
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static SnapshotDto ReadSnapshot(NpgsqlDataReader reader)
        => new(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetGuid(3),
            reader.GetFieldValue<DateTimeOffset>(4),
            reader.IsDBNull(5) ? null : reader.GetGuid(5),
            reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6));

    private static object DbString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : DBNull.Value;

    private static int? TryGetInt(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
            ? number
            : null;

    private static bool? TryGetBool(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;

    private static Guid? TryGetGuid(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value)
           && value.ValueKind == JsonValueKind.String
           && Guid.TryParse(value.GetString(), out var parsed)
            ? parsed
            : null;
}
