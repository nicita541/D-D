using backend.Infrastructure.Database;
using Npgsql;

namespace backend.Modules.AccountCharacters.Infrastructure;

public sealed class AccountCharacterSyncRepository : IAccountCharacterSyncRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public AccountCharacterSyncRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task ImportLatestProfileIntoGameAsync(Guid ownerAccountId, Guid gameStateId, Guid? characterId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var command = new NpgsqlCommand(ImportSql, connection, transaction))
            {
                command.Parameters.AddWithValue("ownerAccountId", ownerAccountId);
                command.Parameters.AddWithValue("gameStateId", gameStateId);
                command.Parameters.AddNullableUuid("characterId", characterId);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ExportGameCharactersAsync(Guid ownerAccountId, Guid gameStateId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(ExportSql, connection);
        command.Parameters.AddWithValue("ownerAccountId", ownerAccountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private const string ImportSql = """
        WITH linked AS (
            SELECT p.id AS player_id,
                   p.game_state_id,
                   ac.*
            FROM game.players p
            JOIN game.game_states gs ON gs.id = p.game_state_id
            JOIN game.account_characters ac ON ac.id = p.account_character_id
            WHERE gs.id = @gameStateId
              AND gs.account_id = @ownerAccountId
              AND (@characterId IS NULL OR p.id = @characterId)
              AND p.account_character_id IS NOT NULL
              AND p.account_character_version IS DISTINCT FROM ac.version
        ),
        updated_players AS (
            UPDATE game.players p
            SET name = linked.name,
                species = linked.species,
                class_name = linked.class_name,
                background = linked.background,
                description = linked.description,
                alignment = linked.alignment,
                account_character_version = linked.version,
                updated_at = now()
            FROM linked
            WHERE p.id = linked.player_id
              AND p.game_state_id = linked.game_state_id
            RETURNING p.id, p.game_state_id
        ),
        upsert_progression AS (
            INSERT INTO game.player_progression (player_id, game_state_id, level, experience, experience_to_next_level)
            SELECT
                player_id,
                game_state_id,
                game.json_int(progression, 'level', 1),
                game.json_int(progression, 'experience', 0),
                game.json_int(progression, 'experienceToNextLevel', 300)
            FROM linked
            ON CONFLICT (player_id)
            DO UPDATE SET
                level = EXCLUDED.level,
                experience = EXCLUDED.experience,
                experience_to_next_level = EXCLUDED.experience_to_next_level
            RETURNING player_id
        ),
        upsert_resources AS (
            INSERT INTO game.player_resources (
                player_id,
                game_state_id,
                hp_max,
                hp_current,
                mana_max,
                mana_current,
                action_points_max,
                action_points_current
            )
            SELECT
                player_id,
                game_state_id,
                game.json_int(resources, 'hpMax', 1),
                game.json_int(resources, 'hpCurrent', game.json_int(resources, 'hpMax', 1)),
                game.json_int(resources, 'manaMax', 0),
                game.json_int(resources, 'manaCurrent', game.json_int(resources, 'manaMax', 0)),
                game.json_int(resources, 'actionPointsMax', 1),
                game.json_int(resources, 'actionPointsCurrent', game.json_int(resources, 'actionPointsMax', 1))
            FROM linked
            ON CONFLICT (player_id)
            DO UPDATE SET
                hp_max = EXCLUDED.hp_max,
                hp_current = LEAST(EXCLUDED.hp_current, EXCLUDED.hp_max),
                mana_max = EXCLUDED.mana_max,
                mana_current = LEAST(EXCLUDED.mana_current, EXCLUDED.mana_max),
                action_points_max = EXCLUDED.action_points_max,
                action_points_current = LEAST(EXCLUDED.action_points_current, EXCLUDED.action_points_max)
            RETURNING player_id
        ),
        upsert_attributes AS (
            INSERT INTO game.player_attributes (
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
            SELECT
                player_id,
                game_state_id,
                game.json_int(attributes, 'strength', 10),
                game.json_int(attributes, 'dexterity', 10),
                game.json_int(attributes, 'constitution', 10),
                game.json_int(attributes, 'intelligence', 10),
                game.json_int(attributes, 'wisdom', 10),
                game.json_int(attributes, 'charisma', 10),
                game.json_int(attributes, 'initiative', 0),
                game.json_int(attributes, 'speed', 9),
                game.json_int(attributes, 'perception', 10)
            FROM linked
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
                perception = EXCLUDED.perception
            RETURNING player_id
        ),
        upsert_wealth AS (
            INSERT INTO game.wealth (player_id, game_state_id, copper, silver, gold, platinum)
            SELECT
                player_id,
                game_state_id,
                game.json_int(wealth, 'copper', 0),
                game.json_int(wealth, 'silver', 0),
                game.json_int(wealth, 'gold', 0),
                game.json_int(wealth, 'platinum', 0)
            FROM linked
            ON CONFLICT (player_id)
            DO UPDATE SET
                copper = EXCLUDED.copper,
                silver = EXCLUDED.silver,
                gold = EXCLUDED.gold,
                platinum = EXCLUDED.platinum
            RETURNING player_id
        ),
        upsert_combat AS (
            INSERT INTO game.combat_stats (player_id, game_state_id, armor_class, proficiency_bonus, in_combat, initiative_roll)
            SELECT
                player_id,
                game_state_id,
                game.json_int(combat, 'armorClass', 10),
                game.json_int(combat, 'proficiencyBonus', 2),
                COALESCE((combat->>'inCombat')::boolean, false),
                game.json_int(combat, 'initiativeRoll', 0)
            FROM linked
            ON CONFLICT (player_id)
            DO UPDATE SET
                armor_class = EXCLUDED.armor_class,
                proficiency_bonus = EXCLUDED.proficiency_bonus,
                in_combat = EXCLUDED.in_combat,
                initiative_roll = EXCLUDED.initiative_roll
            RETURNING player_id
        ),
        deleted_inventory AS (
            DELETE FROM game.item_instances item
            USING linked
            WHERE item.game_state_id = linked.game_state_id
              AND item.owner_kind = 'player_inventory'
              AND item.owner_id = linked.player_id
            RETURNING item.id
        ),
        inserted_inventory AS (
            INSERT INTO game.item_instances (
                game_state_id,
                template_id,
                name,
                item_type,
                subtype,
                description,
                quantity,
                stackable,
                weight_each,
                condition,
                rarity,
                is_magical,
                price_copper,
                price_silver,
                price_gold,
                price_platinum,
                tags,
                owner_kind,
                owner_id
            )
            SELECT
                linked.game_state_id,
                item.value->>'templateId',
                COALESCE(NULLIF(item.value->>'name', ''), 'Предмет'),
                COALESCE(NULLIF(item.value->>'itemType', ''), 'misc'),
                item.value->>'subtype',
                item.value->>'description',
                GREATEST(game.json_int(item.value, 'quantity', 1), 0),
                COALESCE((item.value->>'stackable')::boolean, false),
                GREATEST(game.json_numeric(item.value, 'weightEach', 0), 0),
                COALESCE(NULLIF(item.value->>'condition', ''), 'normal'),
                COALESCE(NULLIF(item.value->>'rarity', ''), 'common'),
                COALESCE((item.value->>'isMagical')::boolean, false),
                GREATEST(game.json_int(item.value, 'priceCopper', 0), 0),
                GREATEST(game.json_int(item.value, 'priceSilver', 0), 0),
                GREATEST(game.json_int(item.value, 'priceGold', 0), 0),
                GREATEST(game.json_int(item.value, 'pricePlatinum', 0), 0),
                CASE WHEN jsonb_typeof(item.value->'tags') = 'array' THEN item.value->'tags' ELSE '[]'::jsonb END,
                'player_inventory',
                linked.player_id
            FROM linked
            CROSS JOIN LATERAL jsonb_array_elements(CASE WHEN jsonb_typeof(linked.inventory) = 'array' THEN linked.inventory ELSE '[]'::jsonb END) AS item(value)
            RETURNING id
        ),
        deleted_attacks AS (
            DELETE FROM game.attacks a
            USING linked
            WHERE a.game_state_id = linked.game_state_id
              AND a.player_id = linked.player_id
            RETURNING a.id
        )
        INSERT INTO game.attacks (game_state_id, player_id, name, roll, damage, damage_type)
        SELECT
            linked.game_state_id,
            linked.player_id,
            COALESCE(NULLIF(attack.value->>'name', ''), 'Удар'),
            COALESCE(NULLIF(attack.value->>'roll', ''), '1d20'),
            COALESCE(NULLIF(attack.value->>'damage', ''), '1'),
            attack.value->>'damageType'
        FROM linked
        CROSS JOIN LATERAL jsonb_array_elements(CASE WHEN jsonb_typeof(linked.attacks) = 'array' THEN linked.attacks ELSE '[]'::jsonb END) AS attack(value);
        """;

    private const string ExportSql = """
        WITH linked AS (
            SELECT
                p.id AS player_id,
                p.game_state_id,
                p.account_character_id,
                p.name,
                p.species,
                p.class_name,
                p.background,
                p.description,
                p.alignment,
                jsonb_build_object(
                    'strength', COALESCE(pa.strength, 10),
                    'dexterity', COALESCE(pa.dexterity, 10),
                    'constitution', COALESCE(pa.constitution, 10),
                    'intelligence', COALESCE(pa.intelligence, 10),
                    'wisdom', COALESCE(pa.wisdom, 10),
                    'charisma', COALESCE(pa.charisma, 10),
                    'initiative', COALESCE(pa.initiative, 0),
                    'speed', COALESCE(pa.speed, 9),
                    'perception', COALESCE(pa.perception, 10)
                ) AS attributes,
                jsonb_build_object(
                    'hpMax', COALESCE(pr.hp_max, 1),
                    'hpCurrent', COALESCE(pr.hp_current, 1),
                    'manaMax', COALESCE(pr.mana_max, 0),
                    'manaCurrent', COALESCE(pr.mana_current, 0),
                    'actionPointsMax', COALESCE(pr.action_points_max, 1),
                    'actionPointsCurrent', COALESCE(pr.action_points_current, 1)
                ) AS resources,
                jsonb_build_object(
                    'level', COALESCE(pp.level, 1),
                    'experience', COALESCE(pp.experience, 0),
                    'experienceToNextLevel', COALESCE(pp.experience_to_next_level, 300)
                ) AS progression,
                jsonb_build_object(
                    'copper', COALESCE(w.copper, 0),
                    'silver', COALESCE(w.silver, 0),
                    'gold', COALESCE(w.gold, 0),
                    'platinum', COALESCE(w.platinum, 0)
                ) AS wealth,
                jsonb_build_object(
                    'armorClass', COALESCE(cs.armor_class, 10),
                    'proficiencyBonus', COALESCE(cs.proficiency_bonus, 2),
                    'inCombat', COALESCE(cs.in_combat, false),
                    'initiativeRoll', COALESCE(cs.initiative_roll, 0)
                ) AS combat,
                COALESCE((
                    SELECT jsonb_agg(
                        jsonb_build_object(
                            'templateId', item.template_id,
                            'name', item.name,
                            'itemType', item.item_type,
                            'subtype', item.subtype,
                            'description', item.description,
                            'quantity', item.quantity,
                            'stackable', item.stackable,
                            'weightEach', item.weight_each,
                            'condition', item.condition,
                            'rarity', item.rarity,
                            'isMagical', item.is_magical,
                            'priceCopper', item.price_copper,
                            'priceSilver', item.price_silver,
                            'priceGold', item.price_gold,
                            'pricePlatinum', item.price_platinum,
                            'tags', item.tags
                        )
                        ORDER BY item.name, item.id
                    )
                    FROM game.item_instances item
                    WHERE item.game_state_id = p.game_state_id
                      AND item.owner_kind = 'player_inventory'
                      AND item.owner_id = p.id
                ), '[]'::jsonb) AS inventory,
                COALESCE((
                    SELECT jsonb_agg(
                        jsonb_build_object(
                            'name', a.name,
                            'roll', a.roll,
                            'damage', a.damage,
                            'damageType', a.damage_type
                        )
                        ORDER BY a.name, a.id
                    )
                    FROM game.attacks a
                    WHERE a.game_state_id = p.game_state_id
                      AND a.player_id = p.id
                ), '[]'::jsonb) AS attacks
            FROM game.players p
            JOIN game.game_states gs ON gs.id = p.game_state_id
            LEFT JOIN game.player_attributes pa ON pa.player_id = p.id
            LEFT JOIN game.player_resources pr ON pr.player_id = p.id
            LEFT JOIN game.player_progression pp ON pp.player_id = p.id
            LEFT JOIN game.wealth w ON w.player_id = p.id
            LEFT JOIN game.combat_stats cs ON cs.player_id = p.id
            WHERE gs.id = @gameStateId
              AND gs.account_id = @ownerAccountId
              AND p.account_character_id IS NOT NULL
        ),
        exported AS (
            UPDATE game.account_characters ac
            SET name = linked.name,
                species = linked.species,
                class_name = linked.class_name,
                background = linked.background,
                description = linked.description,
                alignment = linked.alignment,
                attributes = linked.attributes,
                resources = linked.resources,
                progression = linked.progression,
                wealth = linked.wealth,
                combat = linked.combat,
                inventory = linked.inventory,
                attacks = linked.attacks,
                version = ac.version + 1,
                updated_at = now()
            FROM linked
            WHERE ac.id = linked.account_character_id
            RETURNING ac.id, ac.version
        )
        UPDATE game.players p
        SET account_character_version = exported.version,
            updated_at = now()
        FROM exported
        WHERE p.game_state_id = @gameStateId
          AND p.account_character_id = exported.id;
        """;
}
