using System.Text.Json;
using backend.Modules.Characters.Contracts;
using backend.Modules.Characters.Domain;
using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Modules.Characters.Infrastructure;

public sealed class CharacterRepository : ICharacterRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public CharacterRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<JsonElement>> GetCharactersAsync(Guid accountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var sql = $"""
            SELECT {CharacterJsonExpression}
            FROM game.players p
            JOIN game.game_states gs ON gs.id = p.game_state_id
            LEFT JOIN game.player_progression pp ON pp.player_id = p.id
            LEFT JOIN game.player_resources pr ON pr.player_id = p.id
            LEFT JOIN game.player_attributes pa ON pa.player_id = p.id
            LEFT JOIN game.wealth w ON w.player_id = p.id
            LEFT JOIN game.combat_stats cs ON cs.player_id = p.id
            WHERE p.game_state_id = @gameStateId
              AND gs.account_id = @accountId
            ORDER BY p.created_at, p.id;
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

    public async Task<JsonElement?> GetCharacterAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var sql = $"""
            SELECT {CharacterJsonExpression}
            FROM game.players p
            JOIN game.game_states gs ON gs.id = p.game_state_id
            LEFT JOIN game.player_progression pp ON pp.player_id = p.id
            LEFT JOIN game.player_resources pr ON pr.player_id = p.id
            LEFT JOIN game.player_attributes pa ON pa.player_id = p.id
            LEFT JOIN game.wealth w ON w.player_id = p.id
            LEFT JOIN game.combat_stats cs ON cs.player_id = p.id
            WHERE p.game_state_id = @gameStateId
              AND p.id = @characterId
              AND gs.account_id = @accountId
            LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : RpgDbJson.ParseElement(value.ToString()!);
    }

    public async Task<Guid?> CreateCharacterAsync(Guid accountId, Guid gameStateId, CreateCharacterRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string sql = """
                INSERT INTO game.players
                (
                    game_state_id,
                    account_id,
                    name,
                    background,
                    species,
                    class_name,
                    subclass,
                    description,
                    alignment
                )
                SELECT
                    @gameStateId,
                    @accountId,
                    @name,
                    @background,
                    @species,
                    @className,
                    @subclass,
                    @description,
                    @alignment
                WHERE EXISTS (
                    SELECT 1
                    FROM game.game_states
                    WHERE id = @gameStateId
                      AND account_id = @accountId
                )
                RETURNING id;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("accountId", accountId);
            command.Parameters.AddWithValue("gameStateId", gameStateId);
            AddBaseParameters(command, request.Name, request.Background, request.Species, request.ClassName, request.Subclass, request.Description, request.Alignment);

            var value = await command.ExecuteScalarAsync(cancellationToken);
            if (value is null or DBNull)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var characterId = (Guid)value;
            await UpsertProgressionAsync(connection, transaction, gameStateId, characterId, request.Progression, cancellationToken);
            await UpsertResourcesAsync(connection, transaction, gameStateId, characterId, request.Resources, cancellationToken);
            await UpsertAttributesAsync(connection, transaction, gameStateId, characterId, request.Attributes, cancellationToken);
            await UpsertWealthAsync(connection, transaction, gameStateId, characterId, request.Wealth, cancellationToken);
            await UpsertCombatStatsAsync(connection, transaction, gameStateId, characterId, request.Combat, cancellationToken);
            await EnsureNeedsAsync(connection, transaction, gameStateId, characterId, cancellationToken);
            await EnsureEquippedGearAsync(connection, transaction, gameStateId, characterId, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return characterId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UpdateCharacterAsync(Guid accountId, Guid gameStateId, Guid characterId, UpdateCharacterRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string sql = """
                UPDATE game.players p
                SET name = COALESCE(NULLIF(@name, ''), p.name),
                    background = @background,
                    species = @species,
                    class_name = @className,
                    subclass = @subclass,
                    description = @description,
                    alignment = @alignment,
                    updated_at = now()
                FROM game.game_states gs
                WHERE gs.id = p.game_state_id
                  AND gs.account_id = @accountId
                  AND p.game_state_id = @gameStateId
                  AND p.id = @characterId
                RETURNING p.id;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("accountId", accountId);
            command.Parameters.AddWithValue("gameStateId", gameStateId);
            command.Parameters.AddWithValue("characterId", characterId);
            AddBaseParameters(command, request.Name, request.Background, request.Species, request.ClassName, request.Subclass, request.Description, request.Alignment);

            var value = await command.ExecuteScalarAsync(cancellationToken);
            if (value is null or DBNull)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            if (request.Progression is not null)
            {
                await UpsertProgressionAsync(connection, transaction, gameStateId, characterId, request.Progression, cancellationToken);
            }

            if (request.Resources is not null)
            {
                await UpsertResourcesAsync(connection, transaction, gameStateId, characterId, request.Resources, cancellationToken);
            }

            if (request.Attributes is not null)
            {
                await UpsertAttributesAsync(connection, transaction, gameStateId, characterId, request.Attributes, cancellationToken);
            }

            if (request.Wealth is not null)
            {
                await UpsertWealthAsync(connection, transaction, gameStateId, characterId, request.Wealth, cancellationToken);
            }

            if (request.Combat is not null)
            {
                await UpsertCombatStatsAsync(connection, transaction, gameStateId, characterId, request.Combat, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> DeleteCharacterAsync(Guid accountId, Guid gameStateId, Guid characterId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            DELETE FROM game.players p
            USING game.game_states gs
            WHERE gs.id = p.game_state_id
              AND gs.account_id = @accountId
              AND p.game_state_id = @gameStateId
              AND p.id = @characterId;
        """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private const string CharacterJsonExpression = """
        jsonb_build_object(
            'id', p.id,
            'gameStateId', p.game_state_id,
            'name', p.name,
            'background', p.background,
            'species', p.species,
            'className', p.class_name,
            'subclass', p.subclass,
            'description', p.description,
            'alignment', p.alignment,
            'имя', p.name,
            'предыстория', p.background,
            'вид', p.species,
            'класс', p.class_name,
            'подкласс', p.subclass,
            'описание', p.description,
            'мировоззрение', p.alignment,
            'прогресс', jsonb_build_object(
                'уровень', COALESCE(pp.level, 1),
                'опыт', COALESCE(pp.experience, 0),
                'опытДоСледующегоУровня', COALESCE(pp.experience_to_next_level, 300)
            ),
            'progression', jsonb_build_object(
                'level', COALESCE(pp.level, 1),
                'experience', COALESCE(pp.experience, 0),
                'experienceToNextLevel', COALESCE(pp.experience_to_next_level, 300),
                'levelUpAvailable', COALESCE(pp.level_up_available, false),
                'canLevelUp', COALESCE(pp.level_up_available, false),
                'proficiencyBonus', COALESCE(pp.proficiency_bonus, cs.proficiency_bonus, 2),
                'nextLevelThreshold', NULLIF(COALESCE(pp.experience_to_next_level, 300), 0),
                'hpMax', COALESCE(pr.hp_max, 1),
                'hpCurrent', COALESCE(pr.hp_current, 1)
            ),
            'resources', jsonb_build_object(
                'hpMax', COALESCE(pr.hp_max, 1),
                'hpCurrent', COALESCE(pr.hp_current, 1),
                'manaMax', COALESCE(pr.mana_max, 0),
                'manaCurrent', COALESCE(pr.mana_current, 0),
                'actionPointsMax', COALESCE(pr.action_points_max, 1),
                'actionPointsCurrent', COALESCE(pr.action_points_current, 1)
            ),
            'attributes', jsonb_build_object(
                'strength', COALESCE(pa.strength, 10),
                'dexterity', COALESCE(pa.dexterity, 10),
                'constitution', COALESCE(pa.constitution, 10),
                'intelligence', COALESCE(pa.intelligence, 10),
                'wisdom', COALESCE(pa.wisdom, 10),
                'charisma', COALESCE(pa.charisma, 10),
                'initiative', COALESCE(pa.initiative, 0),
                'speed', COALESCE(pa.speed, 9),
                'perception', COALESCE(pa.perception, 10)
            ),
            'wealth', jsonb_build_object(
                'copper', COALESCE(w.copper, 0),
                'silver', COALESCE(w.silver, 0),
                'gold', COALESCE(w.gold, 0),
                'platinum', COALESCE(w.platinum, 0)
            ),
            'combat', jsonb_build_object(
                'armorClass', COALESCE(cs.armor_class, 10),
                'proficiencyBonus', COALESCE(cs.proficiency_bonus, 2),
                'inCombat', COALESCE(cs.in_combat, false),
                'initiativeRoll', COALESCE(cs.initiative_roll, 0)
            ),
            'ресурсы', jsonb_build_object(
                'хпМаксимум', COALESCE(pr.hp_max, 1),
                'хпТекущее', COALESCE(pr.hp_current, 1),
                'манаМаксимум', COALESCE(pr.mana_max, 0),
                'манаТекущая', COALESCE(pr.mana_current, 0),
                'очкиДействийМаксимум', COALESCE(pr.action_points_max, 1),
                'очкиДействийТекущие', COALESCE(pr.action_points_current, 1)
            ),
            'характеристики', jsonb_build_object(
                'сила', COALESCE(pa.strength, 10),
                'ловкость', COALESCE(pa.dexterity, 10),
                'телосложение', COALESCE(pa.constitution, 10),
                'интеллект', COALESCE(pa.intelligence, 10),
                'мудрость', COALESCE(pa.wisdom, 10),
                'харизма', COALESCE(pa.charisma, 10),
                'инициатива', COALESCE(pa.initiative, 0),
                'скорость', COALESCE(pa.speed, 9),
                'восприятие', COALESCE(pa.perception, 10)
            ),
            'богатство', jsonb_build_object(
                'медные', COALESCE(w.copper, 0),
                'серебряные', COALESCE(w.silver, 0),
                'золотые', COALESCE(w.gold, 0),
                'платиновые', COALESCE(w.platinum, 0)
            ),
            'бой', jsonb_build_object(
                'классДоспеха', COALESCE(cs.armor_class, 10),
                'бонусМастерства', COALESCE(cs.proficiency_bonus, 2),
                'вБою', COALESCE(cs.in_combat, false),
                'бросокИнициативы', COALESCE(cs.initiative_roll, 0)
            )
        )::text
        """;

    private static void AddBaseParameters(
        NpgsqlCommand command,
        string? name,
        string? background,
        string? species,
        string? className,
        string? subclass,
        string? description,
        string? alignment)
    {
        command.Parameters.AddWithValue("name", string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim());
        command.Parameters.AddWithValue("background", RpgDbJson.DbString(background));
        command.Parameters.AddWithValue("species", RpgDbJson.DbString(species));
        command.Parameters.AddWithValue("className", RpgDbJson.DbString(className));
        command.Parameters.AddWithValue("subclass", RpgDbJson.DbString(subclass));
        command.Parameters.AddWithValue("description", RpgDbJson.DbString(description));
        command.Parameters.AddWithValue("alignment", RpgDbJson.DbString(alignment));
    }

    private static async Task UpsertProgressionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid characterId,
        CharacterProgressionRequest progression,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.player_progression
            (
                player_id,
                game_state_id,
                level,
                experience,
                experience_to_next_level,
                level_up_available,
                proficiency_bonus,
                updated_at
            )
            VALUES
            (
                @characterId,
                @gameStateId,
                @level,
                @experience,
                @experienceToNextLevel,
                @levelUpAvailable,
                @proficiencyBonus,
                now()
            )
            ON CONFLICT (player_id)
            DO UPDATE SET
                level = EXCLUDED.level,
                experience = EXCLUDED.experience,
                experience_to_next_level = EXCLUDED.experience_to_next_level,
                level_up_available = EXCLUDED.level_up_available,
                proficiency_bonus = EXCLUDED.proficiency_bonus,
                updated_at = now();
        """;

        var level = Math.Max(1, progression.Level);
        var experience = Math.Max(0, progression.Experience);
        var nextThreshold = ProgressionRules.GetNextLevelThreshold(level);
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("level", level);
        command.Parameters.AddWithValue("experience", experience);
        command.Parameters.AddWithValue("experienceToNextLevel", nextThreshold ?? Math.Max(0, progression.ExperienceToNextLevel));
        command.Parameters.AddWithValue("levelUpAvailable", nextThreshold.HasValue && experience >= nextThreshold.Value);
        command.Parameters.AddWithValue("proficiencyBonus", ProgressionRules.GetProficiencyBonus(level));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertResourcesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid characterId,
        CharacterResourcesRequest resources,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.player_resources
            (
                player_id,
                game_state_id,
                hp_max,
                hp_current,
                mana_max,
                mana_current,
                action_points_max,
                action_points_current
            )
            VALUES
            (
                @characterId,
                @gameStateId,
                @hpMax,
                @hpCurrent,
                @manaMax,
                @manaCurrent,
                @actionPointsMax,
                @actionPointsCurrent
            )
            ON CONFLICT (player_id)
            DO UPDATE SET
                hp_max = EXCLUDED.hp_max,
                hp_current = EXCLUDED.hp_current,
                mana_max = EXCLUDED.mana_max,
                mana_current = EXCLUDED.mana_current,
                action_points_max = EXCLUDED.action_points_max,
                action_points_current = EXCLUDED.action_points_current;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("hpMax", Math.Max(0, resources.HpMax));
        command.Parameters.AddWithValue("hpCurrent", Math.Max(0, resources.HpCurrent));
        command.Parameters.AddWithValue("manaMax", Math.Max(0, resources.ManaMax));
        command.Parameters.AddWithValue("manaCurrent", Math.Max(0, resources.ManaCurrent));
        command.Parameters.AddWithValue("actionPointsMax", Math.Max(0, resources.ActionPointsMax));
        command.Parameters.AddWithValue("actionPointsCurrent", Math.Max(0, resources.ActionPointsCurrent));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertAttributesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid characterId,
        CharacterAttributesRequest attributes,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.player_attributes
            (
                player_id,
                game_state_id,
                strength,
                dexterity,
                constitution,
                intelligence,
                wisdom,
                charisma,
                initiative,
                speed,
                perception
            )
            VALUES
            (
                @characterId,
                @gameStateId,
                @strength,
                @dexterity,
                @constitution,
                @intelligence,
                @wisdom,
                @charisma,
                @initiative,
                @speed,
                @perception
            )
            ON CONFLICT (player_id)
            DO UPDATE SET
                strength = EXCLUDED.strength,
                dexterity = EXCLUDED.dexterity,
                constitution = EXCLUDED.constitution,
                intelligence = EXCLUDED.intelligence,
                wisdom = EXCLUDED.wisdom,
                charisma = EXCLUDED.charisma,
                initiative = EXCLUDED.initiative,
                speed = EXCLUDED.speed,
                perception = EXCLUDED.perception;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("strength", attributes.Strength);
        command.Parameters.AddWithValue("dexterity", attributes.Dexterity);
        command.Parameters.AddWithValue("constitution", attributes.Constitution);
        command.Parameters.AddWithValue("intelligence", attributes.Intelligence);
        command.Parameters.AddWithValue("wisdom", attributes.Wisdom);
        command.Parameters.AddWithValue("charisma", attributes.Charisma);
        command.Parameters.AddWithValue("initiative", attributes.Initiative);
        command.Parameters.AddWithValue("speed", Math.Max(0, attributes.Speed));
        command.Parameters.AddWithValue("perception", attributes.Perception);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertWealthAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid characterId,
        CharacterWealthRequest wealth,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.wealth
            (
                player_id,
                game_state_id,
                copper,
                silver,
                gold,
                platinum
            )
            VALUES
            (
                @characterId,
                @gameStateId,
                @copper,
                @silver,
                @gold,
                @platinum
            )
            ON CONFLICT (player_id)
            DO UPDATE SET
                copper = EXCLUDED.copper,
                silver = EXCLUDED.silver,
                gold = EXCLUDED.gold,
                platinum = EXCLUDED.platinum;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("copper", Math.Max(0, wealth.Copper));
        command.Parameters.AddWithValue("silver", Math.Max(0, wealth.Silver));
        command.Parameters.AddWithValue("gold", Math.Max(0, wealth.Gold));
        command.Parameters.AddWithValue("platinum", Math.Max(0, wealth.Platinum));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertCombatStatsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid characterId,
        CharacterCombatStatsRequest combat,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.combat_stats
            (
                player_id,
                game_state_id,
                armor_class,
                proficiency_bonus,
                in_combat,
                initiative_roll
            )
            VALUES
            (
                @characterId,
                @gameStateId,
                @armorClass,
                @proficiencyBonus,
                @inCombat,
                @initiativeRoll
            )
            ON CONFLICT (player_id)
            DO UPDATE SET
                armor_class = EXCLUDED.armor_class,
                proficiency_bonus = EXCLUDED.proficiency_bonus,
                in_combat = EXCLUDED.in_combat,
                initiative_roll = EXCLUDED.initiative_roll;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("armorClass", Math.Max(0, combat.ArmorClass));
        command.Parameters.AddWithValue("proficiencyBonus", Math.Max(0, combat.ProficiencyBonus));
        command.Parameters.AddWithValue("inCombat", combat.InCombat);
        command.Parameters.AddWithValue("initiativeRoll", combat.InitiativeRoll);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureNeedsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.player_needs (player_id, game_state_id)
            VALUES (@characterId, @gameStateId)
            ON CONFLICT (player_id) DO NOTHING;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureEquippedGearAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid gameStateId,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO game.equipped_gear (player_id, game_state_id)
            VALUES (@characterId, @gameStateId)
            ON CONFLICT (player_id) DO NOTHING;
        """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("characterId", characterId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
