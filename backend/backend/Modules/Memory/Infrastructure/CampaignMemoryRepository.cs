using System.Text.Json;
using backend.Modules.Memory.Contracts;
using backend.Infrastructure.Database;
using backend.Modules.Ai.Application;
using backend.Modules.Campaigns.Application;
using backend.Modules.Changes.Application;
using backend.Modules.Characters.Application;
using backend.Modules.Combat.Application;
using backend.Modules.GameStates.Application;
using backend.Modules.Mechanics.Application;
using backend.Modules.Memory.Application;
using backend.Modules.Party.Application;
using backend.Modules.Play.Application;
using backend.Modules.Story.Application;
using backend.Modules.Travel.Application;
using backend.Modules.Turns.Application;
using backend.Modules.World.Application;
using Npgsql;
using NpgsqlTypes;

namespace backend.Modules.Memory.Infrastructure;

public sealed class CampaignMemoryRepository : ICampaignMemoryRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public CampaignMemoryRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> GetMemoryAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await EnsureMemoryAsync(connection, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        return await SelectMemoryAsync(connection, accountId, gameStateId, cancellationToken);
    }

    public async Task<JsonElement?> UpdateMemoryAsync(Guid accountId, Guid gameStateId, CampaignMemoryRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await EnsureMemoryAsync(connection, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        const string sql = """
            UPDATE game.campaign_memories cm
            SET summary = COALESCE(@summary, summary),
                current_scene = COALESCE(@currentScene, current_scene),
                important_facts = COALESCE(@importantFacts, important_facts),
                open_threads = COALESCE(@openThreads, open_threads),
                resolved_threads = COALESCE(@resolvedThreads, resolved_threads),
                known_npcs = COALESCE(@knownNpcs, known_npcs),
                known_locations = COALESCE(@knownLocations, known_locations),
                master_secrets = COALESCE(@masterSecrets, master_secrets)
            FROM game.game_states gs
            WHERE gs.id = cm.game_state_id
              AND gs.account_id = @accountId
              AND cm.game_state_id = @gameStateId;
        """;

        await using (var command = new NpgsqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("accountId", accountId);
            command.Parameters.AddWithValue("gameStateId", gameStateId);
            var summaryParameter = command.Parameters.Add("summary", NpgsqlDbType.Text);
            summaryParameter.Value = string.IsNullOrWhiteSpace(request.ResolvedSummary) ? DBNull.Value : request.ResolvedSummary.Trim();
            AddJsonb(command, "currentScene", request.ResolvedCurrentScene);
            AddJsonb(command, "importantFacts", request.ResolvedImportantFacts);
            AddJsonb(command, "openThreads", request.ResolvedOpenThreads);
            AddJsonb(command, "resolvedThreads", request.ResolvedResolvedThreads);
            AddJsonb(command, "knownNpcs", request.ResolvedKnownNpcs);
            AddJsonb(command, "knownLocations", request.ResolvedKnownLocations);
            AddJsonb(command, "masterSecrets", request.ResolvedMasterSecrets);

            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                return null;
            }
        }

        return await SelectMemoryAsync(connection, accountId, gameStateId, cancellationToken);
    }

    public async Task<JsonElement?> ApplyMemoryPatchAsync(Guid accountId, Guid gameStateId, JsonElement payload, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!await EnsureMemoryInTransactionAsync(connection, transaction, accountId, gameStateId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var current = await SelectMemoryForUpdateAsync(connection, transaction, accountId, gameStateId, cancellationToken);
            var merged = CampaignMemoryMergeHelper.Merge(current, payload);
            await UpdateMergedMemoryAsync(connection, transaction, gameStateId, merged, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return await SelectMemoryAsync(connection, accountId, gameStateId, cancellationToken);
    }

    public async Task<IReadOnlyList<JsonElement>?> GetRecentLogEntriesAsync(Guid accountId, Guid gameStateId, int limit, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (!await GameStateExistsAsync(connection, accountId, gameStateId, cancellationToken))
        {
            return null;
        }

        const string sql = """
            WITH recent AS (
                SELECT id, game_state_id, turn_number, type, text, important, created_at
                FROM game.game_log_entries
                WHERE game_state_id = @gameStateId
                ORDER BY created_at DESC, id DESC
                LIMIT @limit
            )
            SELECT jsonb_build_object(
                'id', id,
                'gameStateId', game_state_id,
                'turnNumber', turn_number,
                'type', type,
                'text', text,
                'important', important,
                'createdAt', created_at
            )::text
            FROM recent
            ORDER BY created_at ASC, id ASC;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("limit", Math.Clamp(limit, 1, 100));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<JsonElement>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(RpgDbJson.ParseElement(reader.GetString(0)));
        }

        return result;
    }

    public async Task EnsureMemoryAsync(Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO game.campaign_memories (game_state_id)
            SELECT id
            FROM game.game_states
            WHERE id = @gameStateId
            ON CONFLICT (game_state_id) DO NOTHING;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> EnsureMemoryAsync(NpgsqlConnection connection, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.campaign_memories (game_state_id)
            SELECT id
            FROM game.game_states
            WHERE id = @gameStateId
              AND account_id = @accountId
            ON CONFLICT (game_state_id) DO NOTHING;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var inserted = await command.ExecuteNonQueryAsync(cancellationToken);
        return inserted > 0 || await GameStateExistsAsync(connection, accountId, gameStateId, cancellationToken);
    }

    private static async Task<bool> EnsureMemoryInTransactionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid accountId,
        Guid gameStateId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.campaign_memories (game_state_id)
            SELECT id
            FROM game.game_states
            WHERE id = @gameStateId
              AND account_id = @accountId
            ON CONFLICT (game_state_id) DO NOTHING;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var inserted = await command.ExecuteNonQueryAsync(cancellationToken);
        return inserted > 0 || await GameStateExistsInTransactionAsync(connection, transaction, accountId, gameStateId, cancellationToken);
    }

    private static async Task<JsonElement?> SelectMemoryAsync(NpgsqlConnection connection, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT jsonb_build_object(
                'gameStateId', cm.game_state_id,
                'резюме', cm.summary,
                'текущаяСцена', cm.current_scene,
                'важныеФакты', cm.important_facts,
                'открытыеЛинии', cm.open_threads,
                'закрытыеЛинии', cm.resolved_threads,
                'известныеNpc', cm.known_npcs,
                'известныеЛокации', cm.known_locations,
                'секретыМастера', cm.master_secrets,
                'updatedAt', cm.updated_at
            )::text
            FROM game.campaign_memories cm
            JOIN game.game_states gs ON gs.id = cm.game_state_id
            WHERE gs.account_id = @accountId
              AND cm.game_state_id = @gameStateId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    private static async Task<JsonElement> SelectMemoryForUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid accountId,
        Guid gameStateId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT jsonb_build_object(
                'gameStateId', cm.game_state_id,
                'резюме', cm.summary,
                'текущаяСцена', cm.current_scene,
                'важныеФакты', cm.important_facts,
                'открытыеЛинии', cm.open_threads,
                'закрытыеЛинии', cm.resolved_threads,
                'известныеNpc', cm.known_npcs,
                'известныеЛокации', cm.known_locations,
                'секретыМастера', cm.master_secrets,
                'updatedAt', cm.updated_at
            )::text
            FROM game.campaign_memories cm
            JOIN game.game_states gs ON gs.id = cm.game_state_id
            WHERE gs.account_id = @accountId
              AND cm.game_state_id = @gameStateId
            FOR UPDATE OF cm;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is null or DBNull)
        {
            throw new InvalidOperationException("Campaign memory row was expected after ensure.");
        }

        return RpgDbJson.ParseElement(value.ToString()!);
    }

    private static async Task UpdateMergedMemoryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        CampaignMemoryMergedPatch merged,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE game.campaign_memories
            SET summary = @summary,
                current_scene = @currentScene,
                important_facts = @importantFacts,
                open_threads = @openThreads,
                resolved_threads = @resolvedThreads,
                known_npcs = @knownNpcs,
                known_locations = @knownLocations,
                master_secrets = @masterSecrets,
                updated_at = now()
            WHERE game_state_id = @gameStateId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("summary", merged.Summary);
        AddJsonb(command, "currentScene", merged.CurrentScene);
        AddJsonb(command, "importantFacts", merged.ImportantFacts);
        AddJsonb(command, "openThreads", merged.OpenThreads);
        AddJsonb(command, "resolvedThreads", merged.ResolvedThreads);
        AddJsonb(command, "knownNpcs", merged.KnownNpcs);
        AddJsonb(command, "knownLocations", merged.KnownLocations);
        AddJsonb(command, "masterSecrets", merged.MasterSecrets);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> GameStateExistsAsync(NpgsqlConnection connection, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM game.game_states WHERE id = @gameStateId AND account_id = @accountId);";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task<bool> GameStateExistsInTransactionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid accountId,
        Guid gameStateId,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM game.game_states WHERE id = @gameStateId AND account_id = @accountId);";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static void AddJsonb(NpgsqlCommand command, string name, JsonElement? value)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Jsonb);
        parameter.Value = value.HasValue && value.Value.ValueKind != JsonValueKind.Null
            ? value.Value.GetRawText()
            : DBNull.Value;
    }
}
