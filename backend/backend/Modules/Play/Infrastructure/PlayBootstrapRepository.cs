using System.Text.Json;
using backend.Modules.Play;
using backend.Infrastructure.Database;
using Npgsql;
using NpgsqlTypes;

namespace backend.Modules.Play;

public sealed class PlayBootstrapRepository : IPlayBootstrapRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public PlayBootstrapRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PlayBootstrapRepositoryResult> BootstrapAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!await GameStateExistsAsync(connection, transaction, accountId, gameStateId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new PlayBootstrapRepositoryResult(PlayBootstrapRepositoryStatus.NotFound, null);
            }

            if (!await HasCharactersAsync(connection, transaction, gameStateId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new PlayBootstrapRepositoryResult(PlayBootstrapRepositoryStatus.NoCharacters, null);
            }

            await EnsureMemoryAsync(connection, transaction, gameStateId, cancellationToken);
            if (await IsBootstrappedAsync(connection, transaction, gameStateId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new PlayBootstrapRepositoryResult(PlayBootstrapRepositoryStatus.AlreadyBootstrapped, null);
            }

            var locationId = await GetOrCreateLocationAsync(connection, transaction, gameStateId, cancellationToken);
            var questId = await GetOrCreateQuestAsync(connection, transaction, gameStateId, cancellationToken);
            var questStepId = await GetOrCreateQuestStepAsync(connection, transaction, gameStateId, questId, cancellationToken);
            var npcId = await GetOrCreateNpcAsync(connection, transaction, gameStateId, locationId, cancellationToken);
            var updatedAt = DateTimeOffset.UtcNow;

            var scene = new PlaySceneDto(
                "Начало пути",
                "Персонаж оказывается у старой дороги, где первые следы приключения ведут к заброшенному трактиру.",
                "Осмотреть окрестности и выяснить, кто оставил тревожные знаки у дороги.",
                "В лесу рядом с дорогой слышны чужие шаги.",
                locationId,
                new[] { npcId },
                new[] { questId },
                updatedAt);

            var memoryScene = JsonSerializer.SerializeToElement(new
            {
                bootstrap = true,
                title = scene.Title,
                summary = scene.Summary,
                currentObjective = scene.CurrentObjective,
                currentThreat = scene.CurrentThreat,
                locationId = scene.LocationId,
                activeNpcIds = scene.ActiveNpcIds,
                activeQuestIds = scene.ActiveQuestIds,
                updatedAt = scene.UpdatedAt
            });

            await UpdateSceneAsync(connection, transaction, gameStateId, memoryScene, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new PlayBootstrapRepositoryResult(
                PlayBootstrapRepositoryStatus.Ok,
                new PlayBootstrapResponse(true, locationId, questId, questStepId, npcId, scene));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<bool> GameStateExistsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM game.game_states WHERE id = @gameStateId AND account_id = @accountId);";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task<bool> HasCharactersAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM game.players WHERE game_state_id = @gameStateId);";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task EnsureMemoryAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.campaign_memories (game_state_id)
            VALUES (@gameStateId)
            ON CONFLICT (game_state_id) DO NOTHING;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> IsBootstrappedAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COALESCE(current_scene->>'bootstrap', 'false') = 'true'
            FROM game.campaign_memories
            WHERE game_state_id = @gameStateId
            FOR UPDATE;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task<Guid> GetOrCreateLocationAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string selectSql = "SELECT id FROM game.locations WHERE game_state_id = @gameStateId ORDER BY created_at, id LIMIT 1;";
        await using (var select = new NpgsqlCommand(selectSql, connection, transaction))
        {
            select.Parameters.AddWithValue("gameStateId", gameStateId);
            var existing = await select.ExecuteScalarAsync(cancellationToken);
            if (existing is Guid id)
            {
                return id;
            }
        }

        const string insertSql = """
            INSERT INTO game.locations (game_state_id, name, description)
            VALUES (@gameStateId, @name, @description)
            RETURNING id;
        """;

        await using var insert = new NpgsqlCommand(insertSql, connection, transaction);
        insert.Parameters.AddWithValue("gameStateId", gameStateId);
        insert.Parameters.AddWithValue("name", "Старая дорога");
        insert.Parameters.AddWithValue("description", "Пыльная дорога у кромки леса. Рядом стоит покосившийся указатель и видны свежие следы.");
        return (Guid)(await insert.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Bootstrap location id was not returned."));
    }

    private static async Task<Guid> GetOrCreateQuestAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, CancellationToken cancellationToken)
    {
        const string selectSql = "SELECT id FROM game.quests WHERE game_state_id = @gameStateId AND title = @title LIMIT 1;";
        await using (var select = new NpgsqlCommand(selectSql, connection, transaction))
        {
            select.Parameters.AddWithValue("gameStateId", gameStateId);
            select.Parameters.AddWithValue("title", "Первый след");
            var existing = await select.ExecuteScalarAsync(cancellationToken);
            if (existing is Guid id)
            {
                return id;
            }
        }

        const string insertSql = """
            INSERT INTO game.quests (game_state_id, title, description, status)
            VALUES (@gameStateId, @title, @description, 'active')
            RETURNING id;
        """;

        await using var insert = new NpgsqlCommand(insertSql, connection, transaction);
        insert.Parameters.AddWithValue("gameStateId", gameStateId);
        insert.Parameters.AddWithValue("title", "Первый след");
        insert.Parameters.AddWithValue("description", "Разобраться, что случилось у старой дороги, и найти источник тревожных следов.");
        return (Guid)(await insert.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Bootstrap quest id was not returned."));
    }

    private static async Task<Guid> GetOrCreateQuestStepAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid questId, CancellationToken cancellationToken)
    {
        const string selectSql = "SELECT id FROM game.quest_steps WHERE game_state_id = @gameStateId AND quest_id = @questId ORDER BY sort_order, id LIMIT 1;";
        await using (var select = new NpgsqlCommand(selectSql, connection, transaction))
        {
            select.Parameters.AddWithValue("gameStateId", gameStateId);
            select.Parameters.AddWithValue("questId", questId);
            var existing = await select.ExecuteScalarAsync(cancellationToken);
            if (existing is Guid id)
            {
                return id;
            }
        }

        const string insertSql = """
            INSERT INTO game.quest_steps (game_state_id, quest_id, description, sort_order)
            VALUES (@gameStateId, @questId, @description, 0)
            RETURNING id;
        """;

        await using var insert = new NpgsqlCommand(insertSql, connection, transaction);
        insert.Parameters.AddWithValue("gameStateId", gameStateId);
        insert.Parameters.AddWithValue("questId", questId);
        insert.Parameters.AddWithValue("description", "Осмотреть дорогу, следы и поговорить с первым встреченным NPC.");
        return (Guid)(await insert.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Bootstrap quest step id was not returned."));
    }

    private static async Task<Guid> GetOrCreateNpcAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, Guid locationId, CancellationToken cancellationToken)
    {
        const string selectSql = "SELECT id FROM game.npcs WHERE game_state_id = @gameStateId AND name = @name LIMIT 1;";
        await using (var select = new NpgsqlCommand(selectSql, connection, transaction))
        {
            select.Parameters.AddWithValue("gameStateId", gameStateId);
            select.Parameters.AddWithValue("name", "Проводник Мирон");
            var existing = await select.ExecuteScalarAsync(cancellationToken);
            if (existing is Guid id)
            {
                return id;
            }
        }

        const string insertSql = """
            INSERT INTO game.npcs (game_state_id, location_id, name, role, attitude, description, is_alive)
            VALUES (@gameStateId, @locationId, @name, @role, 'neutral', @description, true)
            RETURNING id;
        """;

        await using var insert = new NpgsqlCommand(insertSql, connection, transaction);
        insert.Parameters.AddWithValue("gameStateId", gameStateId);
        insert.Parameters.AddWithValue("locationId", locationId);
        insert.Parameters.AddWithValue("name", "Проводник Мирон");
        insert.Parameters.AddWithValue("role", "проводник");
        insert.Parameters.AddWithValue("description", "Усталый проводник, который знает слухи о дороге и боится заходить в лес.");
        return (Guid)(await insert.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Bootstrap npc id was not returned."));
    }

    private static async Task UpdateSceneAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid gameStateId, JsonElement scene, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE game.campaign_memories
            SET current_scene = @scene,
                updated_at = now()
            WHERE game_state_id = @gameStateId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        var sceneParameter = command.Parameters.Add("scene", NpgsqlDbType.Jsonb);
        sceneParameter.Value = scene.GetRawText();
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
