using System.Text.Json;
using backend.Contracts.Rpg.Memory;
using backend.Infrastructure.Database;
using Npgsql;
using NpgsqlTypes;

namespace backend.Repositories.Rpg;

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

    private static async Task<bool> GameStateExistsAsync(NpgsqlConnection connection, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM game.game_states WHERE id = @gameStateId AND account_id = @accountId);";
        await using var command = new NpgsqlCommand(sql, connection);
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
