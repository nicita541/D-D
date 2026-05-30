using System.Text.Json;
using backend.Contracts.Rpg.Common;
using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Repositories.Rpg;

public sealed class CharacterProgressionRepository : ICharacterProgressionRepository
{
    private static readonly IReadOnlyDictionary<int, int> ExperienceThresholds = new Dictionary<int, int>
    {
        [1] = 300,
        [2] = 900,
        [3] = 2700,
        [4] = 6500,
        [5] = 14000,
        [6] = 23000,
        [7] = 34000,
        [8] = 48000,
        [9] = 64000,
        [10] = 85000,
        [11] = 100000,
        [12] = 120000,
        [13] = 140000,
        [14] = 165000,
        [15] = 195000,
        [16] = 225000,
        [17] = 265000,
        [18] = 305000,
        [19] = 355000,
        [20] = 0
    };

    private readonly IPostgresConnectionFactory _connectionFactory;

    public CharacterProgressionRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<JsonElement?> AddExperienceAsync(Guid accountId, Guid gameStateId, Guid characterId, int experience, string reason, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE game.player_progression pp
            SET experience = pp.experience + @experience
            FROM game.players p
            JOIN game.game_states gs ON gs.id = p.game_state_id
            WHERE pp.player_id = p.id
              AND pp.game_state_id = p.game_state_id
              AND gs.account_id = @accountId
              AND p.game_state_id = @gameStateId
              AND p.id = @characterId
            RETURNING pp.level, pp.experience, pp.experience_to_next_level;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("experience", experience);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var level = reader.GetInt32(0);
        var totalExperience = reader.GetInt32(1);
        var experienceToNextLevel = reader.GetInt32(2);
        return JsonSerializer.SerializeToElement(new
        {
            characterId,
            experience = totalExperience,
            addedExperience = experience,
            reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim(),
            level,
            experienceToNextLevel,
            canLevelUp = experienceToNextLevel > 0 && totalExperience >= experienceToNextLevel
        });
    }

    public async Task<JsonElement?> LevelUpAsync(Guid accountId, Guid gameStateId, Guid characterId, int newLevel, int hpMaxAdd, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string selectSql = """
                SELECT pp.level, pp.experience, pp.experience_to_next_level, pr.hp_current, pr.hp_max
                FROM game.players p
                JOIN game.game_states gs ON gs.id = p.game_state_id
                JOIN game.player_progression pp ON pp.player_id = p.id AND pp.game_state_id = p.game_state_id
                JOIN game.player_resources pr ON pr.player_id = p.id AND pr.game_state_id = p.game_state_id
                WHERE gs.account_id = @accountId
                  AND p.game_state_id = @gameStateId
                  AND p.id = @characterId
                FOR UPDATE OF pp, pr;
            """;

            int currentLevel;
            int experience;
            int currentHp;
            int currentHpMax;
            await using (var select = new NpgsqlCommand(selectSql, connection, transaction))
            {
                select.Parameters.AddWithValue("accountId", accountId);
                select.Parameters.AddWithValue("gameStateId", gameStateId);
                select.Parameters.AddWithValue("characterId", characterId);
                await using var reader = await select.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                currentLevel = reader.GetInt32(0);
                experience = reader.GetInt32(1);
                currentHp = reader.GetInt32(3);
                currentHpMax = reader.GetInt32(4);
            }

            if (newLevel != currentLevel + 1)
            {
                throw new RpgValidationException("новыйУровень должен быть ровно текущий уровень + 1.");
            }

            if (!ExperienceThresholds.TryGetValue(newLevel, out var nextThreshold))
            {
                throw new RpgValidationException("новыйУровень должен быть от 2 до 20.");
            }

            const string updateProgressionSql = """
                UPDATE game.player_progression
                SET level = @newLevel,
                    experience_to_next_level = @nextThreshold
                WHERE game_state_id = @gameStateId
                  AND player_id = @characterId;
            """;

            await using (var updateProgression = new NpgsqlCommand(updateProgressionSql, connection, transaction))
            {
                updateProgression.Parameters.AddWithValue("gameStateId", gameStateId);
                updateProgression.Parameters.AddWithValue("characterId", characterId);
                updateProgression.Parameters.AddWithValue("newLevel", newLevel);
                updateProgression.Parameters.AddWithValue("nextThreshold", nextThreshold);
                await updateProgression.ExecuteNonQueryAsync(cancellationToken);
            }

            const string updateResourcesSql = """
                UPDATE game.player_resources
                SET hp_max = hp_max + @hpMaxAdd,
                    hp_current = hp_current + @hpMaxAdd
                WHERE game_state_id = @gameStateId
                  AND player_id = @characterId;
            """;

            await using (var updateResources = new NpgsqlCommand(updateResourcesSql, connection, transaction))
            {
                updateResources.Parameters.AddWithValue("gameStateId", gameStateId);
                updateResources.Parameters.AddWithValue("characterId", characterId);
                updateResources.Parameters.AddWithValue("hpMaxAdd", hpMaxAdd);
                await updateResources.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            return JsonSerializer.SerializeToElement(new
            {
                characterId,
                level = newLevel,
                experience,
                experienceToNextLevel = nextThreshold,
                hpCurrent = currentHp + hpMaxAdd,
                hpMax = currentHpMax + hpMaxAdd
            });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
