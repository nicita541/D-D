using System.Text.Json;
using backend.Modules.Characters.Domain;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using Npgsql;

namespace backend.Modules.Characters.Infrastructure;

internal static class CharacterProgressionSql
{
    public static async Task<CharacterProgressionState?> SelectAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid gameStateId,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT pp.player_id,
                   pp.level,
                   pp.experience,
                   pp.experience_to_next_level,
                   COALESCE(pp.level_up_available, false),
                   COALESCE(pp.proficiency_bonus, 2),
                   COALESCE(pr.hp_current, 0),
                   COALESCE(pr.hp_max, 0)
            FROM game.player_progression pp
            LEFT JOIN game.player_resources pr ON pr.player_id = pp.player_id AND pr.game_state_id = pp.game_state_id
            WHERE pp.game_state_id = @gameStateId
              AND pp.player_id = @characterId
            LIMIT 1;
        """;

        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadState(reader)
            : null;
    }

    public static async Task<CharacterProgressionState?> AddExperienceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid characterId,
        int amount,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            throw new RpgValidationException("Опыт должен быть больше 0.");
        }

        var current = await SelectForUpdateAsync(connection, transaction, gameStateId, characterId, cancellationToken);
        if (current is null)
        {
            return null;
        }

        var totalExperience = checked(current.Experience + amount);
        var nextThreshold = ProgressionRules.GetNextLevelThreshold(current.Level);
        var proficiencyBonus = ProgressionRules.GetProficiencyBonus(current.Level);
        var levelUpAvailable = nextThreshold.HasValue && totalExperience >= nextThreshold.Value;

        const string sql = """
            UPDATE game.player_progression
            SET experience = @experience,
                experience_to_next_level = @experienceToNextLevel,
                level_up_available = @levelUpAvailable,
                proficiency_bonus = @proficiencyBonus,
                updated_at = now()
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("experience", totalExperience);
        command.Parameters.AddWithValue("experienceToNextLevel", nextThreshold ?? 0);
        command.Parameters.AddWithValue("levelUpAvailable", levelUpAvailable);
        command.Parameters.AddWithValue("proficiencyBonus", proficiencyBonus);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return current with
        {
            Experience = totalExperience,
            ExperienceToNextLevel = nextThreshold ?? 0,
            LevelUpAvailable = levelUpAvailable,
            ProficiencyBonus = proficiencyBonus
        };
    }

    public static async Task<LevelUpResult?> LevelUpAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid characterId,
        int? requestedNewLevel,
        int? requestedHpIncrease,
        CancellationToken cancellationToken)
    {
        var current = await SelectForUpdateAsync(connection, transaction, gameStateId, characterId, cancellationToken);
        if (current is null)
        {
            return null;
        }

        var newLevel = current.Level + 1;
        if (requestedNewLevel.HasValue && requestedNewLevel.Value != newLevel)
        {
            throw new RpgValidationException("Запрошенный уровень должен быть равен текущему уровню + 1.");
        }

        if (newLevel > ProgressionRules.MaxMvpLevel)
        {
            throw new RpgValidationException("MVP поддерживает повышение только до 5 уровня.");
        }

        var requiredExperience = ProgressionRules.GetLevelThreshold(newLevel);
        if (current.Experience < requiredExperience)
        {
            throw new RpgValidationException("Недостаточно опыта для повышения уровня.");
        }

        var hpIncrease = ProgressionRules.GetHpIncrease(requestedHpIncrease);
        if (hpIncrease is < 0 or > 50)
        {
            throw new RpgValidationException("Прирост максимального HP должен быть от 0 до 50.");
        }

        var newNextThreshold = ProgressionRules.GetNextLevelThreshold(newLevel);
        var levelUpAvailable = newNextThreshold.HasValue && current.Experience >= newNextThreshold.Value;
        var proficiencyBonus = ProgressionRules.GetProficiencyBonus(newLevel);

        const string updateProgressionSql = """
            UPDATE game.player_progression
            SET level = @newLevel,
                experience_to_next_level = @experienceToNextLevel,
                level_up_available = @levelUpAvailable,
                proficiency_bonus = @proficiencyBonus,
                updated_at = now()
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId;
        """;

        await using (var updateProgression = new NpgsqlCommand(updateProgressionSql, connection, transaction))
        {
            updateProgression.Parameters.AddWithValue("gameStateId", gameStateId);
            updateProgression.Parameters.AddWithValue("characterId", characterId);
            updateProgression.Parameters.AddWithValue("newLevel", newLevel);
            updateProgression.Parameters.AddWithValue("experienceToNextLevel", newNextThreshold ?? 0);
            updateProgression.Parameters.AddWithValue("levelUpAvailable", levelUpAvailable);
            updateProgression.Parameters.AddWithValue("proficiencyBonus", proficiencyBonus);
            await updateProgression.ExecuteNonQueryAsync(cancellationToken);
        }

        const string updateResourcesSql = """
            UPDATE game.player_resources
            SET hp_max = hp_max + @hpIncrease,
                hp_current = hp_current + @hpIncrease
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId
            RETURNING hp_current, hp_max;
        """;

        var hpCurrent = current.HpCurrent + hpIncrease;
        var hpMax = current.HpMax + hpIncrease;
        await using (var updateResources = new NpgsqlCommand(updateResourcesSql, connection, transaction))
        {
            updateResources.Parameters.AddWithValue("gameStateId", gameStateId);
            updateResources.Parameters.AddWithValue("characterId", characterId);
            updateResources.Parameters.AddWithValue("hpIncrease", hpIncrease);
            await using var reader = await updateResources.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                hpCurrent = reader.GetInt32(0);
                hpMax = reader.GetInt32(1);
            }
        }

        const string updateCombatStatsSql = """
            UPDATE game.combat_stats
            SET proficiency_bonus = @proficiencyBonus
            WHERE game_state_id = @gameStateId
              AND player_id = @characterId;
        """;

        await using (var updateCombatStats = new NpgsqlCommand(updateCombatStatsSql, connection, transaction))
        {
            updateCombatStats.Parameters.AddWithValue("gameStateId", gameStateId);
            updateCombatStats.Parameters.AddWithValue("characterId", characterId);
            updateCombatStats.Parameters.AddWithValue("proficiencyBonus", proficiencyBonus);
            await updateCombatStats.ExecuteNonQueryAsync(cancellationToken);
        }

        var newState = current with
        {
            Level = newLevel,
            ExperienceToNextLevel = newNextThreshold ?? 0,
            LevelUpAvailable = levelUpAvailable,
            ProficiencyBonus = proficiencyBonus,
            HpCurrent = hpCurrent,
            HpMax = hpMax
        };

        return new LevelUpResult(current.Level, newState, hpIncrease);
    }

    public static JsonElement ToProgressionJson(CharacterProgressionState state)
        => JsonSerializer.SerializeToElement(new
        {
            characterId = state.CharacterId,
            level = state.Level,
            experience = state.Experience,
            experienceToNextLevel = state.ExperienceToNextLevel,
            levelUpAvailable = state.LevelUpAvailable,
            canLevelUp = state.LevelUpAvailable,
            proficiencyBonus = state.ProficiencyBonus,
            nextLevelThreshold = state.NextLevelThreshold,
            hpMax = state.HpMax,
            hpCurrent = state.HpCurrent
        });

    public static JsonElement ToAddExperienceJson(CharacterProgressionState state, int addedExperience, string reason)
        => JsonSerializer.SerializeToElement(new
        {
            characterId = state.CharacterId,
            addedExperience,
            experience = state.Experience,
            totalExperience = state.Experience,
            level = state.Level,
            experienceToNextLevel = state.ExperienceToNextLevel,
            levelUpAvailable = state.LevelUpAvailable,
            canLevelUp = state.LevelUpAvailable,
            proficiencyBonus = state.ProficiencyBonus,
            nextLevelThreshold = state.NextLevelThreshold,
            hpMax = state.HpMax,
            hpCurrent = state.HpCurrent,
            reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim()
        });

    public static JsonElement ToLevelUpJson(LevelUpResult result)
        => JsonSerializer.SerializeToElement(new
        {
            characterId = result.State.CharacterId,
            oldLevel = result.OldLevel,
            newLevel = result.State.Level,
            level = result.State.Level,
            experience = result.State.Experience,
            experienceToNextLevel = result.State.ExperienceToNextLevel,
            levelUpAvailable = result.State.LevelUpAvailable,
            proficiencyBonus = result.State.ProficiencyBonus,
            nextLevelThreshold = result.State.NextLevelThreshold,
            hpIncrease = result.HpIncrease,
            hpMax = result.State.HpMax,
            hpCurrent = result.State.HpCurrent
        });

    private static async Task<CharacterProgressionState?> SelectForUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT pp.player_id,
                   pp.level,
                   pp.experience,
                   pp.experience_to_next_level,
                   COALESCE(pp.level_up_available, false),
                   COALESCE(pp.proficiency_bonus, 2),
                   COALESCE(pr.hp_current, 0),
                   COALESCE(pr.hp_max, 0)
            FROM game.player_progression pp
            LEFT JOIN game.player_resources pr ON pr.player_id = pp.player_id AND pr.game_state_id = pp.game_state_id
            WHERE pp.game_state_id = @gameStateId
              AND pp.player_id = @characterId
            FOR UPDATE OF pp;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadState(reader)
            : null;
    }

    private static CharacterProgressionState ReadState(NpgsqlDataReader reader)
        => new(
            reader.GetGuid(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetInt32(3),
            reader.GetBoolean(4),
            reader.GetInt32(5),
            reader.GetInt32(6),
            reader.GetInt32(7));
}

internal sealed record CharacterProgressionState(
    Guid CharacterId,
    int Level,
    int Experience,
    int ExperienceToNextLevel,
    bool LevelUpAvailable,
    int ProficiencyBonus,
    int HpCurrent,
    int HpMax)
{
    public int? NextLevelThreshold => ProgressionRules.GetNextLevelThreshold(Level);
}

internal sealed record LevelUpResult(int OldLevel, CharacterProgressionState State, int HpIncrease);
