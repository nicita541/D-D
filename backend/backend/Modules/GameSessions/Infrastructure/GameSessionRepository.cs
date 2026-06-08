using backend.Infrastructure.Database;
using backend.Modules.GameSessions.Contracts;
using backend.Shared.Kernel;
using Npgsql;
using NpgsqlTypes;

namespace backend.Modules.GameSessions.Infrastructure;

public sealed class GameSessionRepository : IGameSessionRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public GameSessionRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RpgResult<StartGameSessionResponse>> StartSoloSessionAsync(Guid accountId, StartGameSessionRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var session = await CreateSessionAsync(connection, transaction, accountId, request, cancellationToken);
            if (session is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return RpgResult<StartGameSessionResponse>.NotFound("Герой или история не найдены.");
            }

            await AssignHostCharacterAsync(connection, transaction, accountId, session.GameStateId, session.CharacterId, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return RpgResult<StartGameSessionResponse>.Ok(session);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<StartGameSessionResponse?> CreateSessionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid accountId,
        StartGameSessionRequest request,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(StartSoloSessionSql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("accountCharacterId", request.AccountCharacterId);
        command.Parameters.AddWithValue("campaignTemplateId", request.CampaignTemplateId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var gameStateId = reader.GetGuid(0);
        var characterId = reader.GetGuid(1);
        return new StartGameSessionResponse(gameStateId, characterId, $"/games/{gameStateId}/play");
    }

    private static async Task AssignHostCharacterAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid accountId,
        Guid gameStateId,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE game.party_members
            SET character_id = @characterId,
                role = 'host',
                status = 'active',
                updated_at = now()
            WHERE game_state_id = @gameStateId
              AND account_id = @accountId
              AND status = 'active';
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("gameStateId", gameStateId);
        command.Parameters.AddWithValue("characterId", characterId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private const string StartSoloSessionSql = """
        WITH selected AS (
            SELECT
                ac.id AS account_character_id,
                ac.account_id,
                ac.name,
                ac.species,
                ac.class_name,
                ac.background,
                ac.description,
                ac.alignment,
                ac.attributes,
                ac.resources,
                ac.progression,
                ac.wealth,
                ac.combat,
                ac.inventory,
                ac.attacks,
                ac.version,
                ct.id AS campaign_template_id,
                ct.title,
                ct.opening_scene,
                ct.main_goal,
                ct.master_secrets,
                ct.initial_flags
            FROM game.account_characters ac
            CROSS JOIN game.campaign_templates ct
            WHERE ac.account_id = @accountId
              AND ac.id = @accountCharacterId
              AND ct.id = @campaignTemplateId
            LIMIT 1
        ),
        created_game AS (
            SELECT game.create_new_game(
                @accountId,
                (SELECT CONCAT(title, ' — ', name) FROM selected)
            ) AS game_state_id
            WHERE EXISTS (SELECT 1 FROM selected)
        ),
        story AS (
            INSERT INTO game.story_states (
                game_state_id,
                campaign_template_id,
                current_act,
                current_scene,
                current_goal,
                tension_level,
                plot_flags,
                known_facts,
                hidden_facts,
                short_memory
            )
            SELECT
                game_state_id,
                selected.campaign_template_id,
                'Акт 1',
                selected.opening_scene,
                selected.main_goal,
                1,
                selected.initial_flags,
                jsonb_build_array(selected.opening_scene),
                selected.master_secrets,
                '[]'::jsonb
            FROM created_game
            CROSS JOIN selected
            ON CONFLICT (game_state_id)
            DO UPDATE SET
                campaign_template_id = EXCLUDED.campaign_template_id,
                current_act = EXCLUDED.current_act,
                current_scene = EXCLUDED.current_scene,
                current_goal = EXCLUDED.current_goal,
                tension_level = EXCLUDED.tension_level,
                plot_flags = EXCLUDED.plot_flags,
                known_facts = EXCLUDED.known_facts,
                hidden_facts = EXCLUDED.hidden_facts,
                short_memory = EXCLUDED.short_memory,
                updated_at = now()
            RETURNING game_state_id
        ),
        player AS (
            INSERT INTO game.players (
                game_state_id,
                account_id,
                name,
                background,
                species,
                class_name,
                description,
                alignment,
                account_character_id,
                account_character_version
            )
            SELECT
                created_game.game_state_id,
                @accountId,
                selected.name,
                selected.background,
                selected.species,
                selected.class_name,
                selected.description,
                selected.alignment,
                selected.account_character_id,
                selected.version
            FROM created_game
            CROSS JOIN selected
            RETURNING id, game_state_id
        ),
        progression AS (
            INSERT INTO game.player_progression (player_id, game_state_id, level, experience, experience_to_next_level)
            SELECT
                player.id,
                player.game_state_id,
                game.json_int(selected.progression, 'level', 1),
                game.json_int(selected.progression, 'experience', 0),
                game.json_int(selected.progression, 'experienceToNextLevel', 300)
            FROM player
            CROSS JOIN selected
            RETURNING player_id
        ),
        resources AS (
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
                player.id,
                player.game_state_id,
                game.json_int(selected.resources, 'hpMax', 1),
                game.json_int(selected.resources, 'hpCurrent', game.json_int(selected.resources, 'hpMax', 1)),
                game.json_int(selected.resources, 'manaMax', 0),
                game.json_int(selected.resources, 'manaCurrent', game.json_int(selected.resources, 'manaMax', 0)),
                game.json_int(selected.resources, 'actionPointsMax', 1),
                game.json_int(selected.resources, 'actionPointsCurrent', game.json_int(selected.resources, 'actionPointsMax', 1))
            FROM player
            CROSS JOIN selected
            RETURNING player_id
        ),
        attributes AS (
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
                player.id,
                player.game_state_id,
                game.json_int(selected.attributes, 'strength', 10),
                game.json_int(selected.attributes, 'dexterity', 10),
                game.json_int(selected.attributes, 'constitution', 10),
                game.json_int(selected.attributes, 'intelligence', 10),
                game.json_int(selected.attributes, 'wisdom', 10),
                game.json_int(selected.attributes, 'charisma', 10),
                game.json_int(selected.attributes, 'initiative', 0),
                game.json_int(selected.attributes, 'speed', 9),
                game.json_int(selected.attributes, 'perception', 10)
            FROM player
            CROSS JOIN selected
            RETURNING player_id
        ),
        wealth AS (
            INSERT INTO game.wealth (player_id, game_state_id, copper, silver, gold, platinum)
            SELECT
                player.id,
                player.game_state_id,
                game.json_int(selected.wealth, 'copper', 0),
                game.json_int(selected.wealth, 'silver', 0),
                game.json_int(selected.wealth, 'gold', 0),
                game.json_int(selected.wealth, 'platinum', 0)
            FROM player
            CROSS JOIN selected
            RETURNING player_id
        ),
        combat AS (
            INSERT INTO game.combat_stats (player_id, game_state_id, armor_class, proficiency_bonus, in_combat, initiative_roll)
            SELECT
                player.id,
                player.game_state_id,
                game.json_int(selected.combat, 'armorClass', 10),
                game.json_int(selected.combat, 'proficiencyBonus', 2),
                false,
                0
            FROM player
            CROSS JOIN selected
            RETURNING player_id
        ),
        needs AS (
            INSERT INTO game.player_needs (player_id, game_state_id)
            SELECT player.id, player.game_state_id
            FROM player
            RETURNING player_id
        ),
        gear AS (
            INSERT INTO game.equipped_gear (player_id, game_state_id)
            SELECT player.id, player.game_state_id
            FROM player
            RETURNING player_id
        ),
        inventory AS (
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
                player.game_state_id,
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
                player.id
            FROM player
            CROSS JOIN selected
            CROSS JOIN LATERAL jsonb_array_elements(CASE WHEN jsonb_typeof(selected.inventory) = 'array' THEN selected.inventory ELSE '[]'::jsonb END) AS item(value)
            RETURNING id
        ),
        attacks AS (
            INSERT INTO game.attacks (game_state_id, player_id, name, roll, damage, damage_type)
            SELECT
                player.game_state_id,
                player.id,
                COALESCE(NULLIF(attack.value->>'name', ''), 'Удар'),
                COALESCE(NULLIF(attack.value->>'roll', ''), '1d20'),
                COALESCE(NULLIF(attack.value->>'damage', ''), '1'),
                attack.value->>'damageType'
            FROM player
            CROSS JOIN selected
            CROSS JOIN LATERAL jsonb_array_elements(CASE WHEN jsonb_typeof(selected.attacks) = 'array' THEN selected.attacks ELSE '[]'::jsonb END) AS attack(value)
            RETURNING id
        ),
        party AS (
            UPDATE game.party_members pm
            SET character_id = player.id,
                role = 'host',
                status = 'active',
                display_name = selected.name,
                updated_at = now()
            FROM player
            CROSS JOIN selected
            WHERE pm.game_state_id = player.game_state_id
              AND pm.account_id = @accountId
              AND pm.status = 'active'
            RETURNING pm.id
        )
        SELECT player.game_state_id, player.id
        FROM player;
        """;
}
