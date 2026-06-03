\set ON_ERROR_STOP on

BEGIN;

CREATE TEMP TABLE smoke_context (
    account_id uuid,
    game_state_id uuid,
    player_id uuid,
    refresh_token_id uuid,
    location_id uuid,
    sword_id uuid,
    well_id uuid,
    campaign_template_id uuid,
    story_state_id uuid,
    party_id uuid,
    party_member_id uuid,
    combat_state_id uuid,
    combat_participant_id uuid,
    monster_id uuid,
    monster_participant_id uuid,
    game_turn_id uuid,
    game_change_id uuid
) ON COMMIT DROP;

WITH inserted_account AS (
    INSERT INTO auth.accounts (email, username, password_hash, display_name)
    VALUES ('smoke@example.com', 'smoke_user', 'fake_hash_for_smoke_test', 'Smoke Test')
    RETURNING id
)
INSERT INTO smoke_context (account_id)
SELECT id
FROM inserted_account;

SELECT 'OK 01: account created' AS check, account_id
FROM smoke_context;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN auth.accounts a ON a.id = ctx.account_id
        WHERE a.role = 'user'
    ) THEN
        RAISE EXCEPTION 'FAIL 02: account default role is not user';
    END IF;
END;
$$;

SELECT 'OK 02: account default role is user' AS check, a.role
FROM smoke_context ctx
JOIN auth.accounts a ON a.id = ctx.account_id;

WITH inserted_refresh_token AS (
    INSERT INTO auth.refresh_tokens (
        account_id,
        token_hash,
        expires_at,
        created_by_ip,
        user_agent
    )
    SELECT account_id,
           'smoke_refresh_token_hash',
           now() + interval '7 days',
           '127.0.0.1',
           'smoke-test'
    FROM smoke_context
    RETURNING id
)
UPDATE smoke_context
SET refresh_token_id = inserted_refresh_token.id
FROM inserted_refresh_token;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN auth.refresh_tokens rt ON rt.id = ctx.refresh_token_id
        WHERE rt.token_hash = 'smoke_refresh_token_hash'
          AND rt.created_by_ip = '127.0.0.1'
          AND rt.user_agent = 'smoke-test'
          AND rt.replaced_by_token_hash IS NULL
    ) THEN
        RAISE EXCEPTION 'FAIL 03: refresh token was not created with expected hash metadata';
    END IF;
END;
$$;

SELECT 'OK 03: refresh token created with token_hash metadata' AS check, refresh_token_id
FROM smoke_context;

DO $$
DECLARE
    v_account_id uuid;
BEGIN
    SELECT account_id INTO v_account_id
    FROM smoke_context
    LIMIT 1;

    BEGIN
        INSERT INTO auth.refresh_tokens (account_id, token_hash, expires_at)
        VALUES (v_account_id, 'smoke_refresh_token_hash', now() + interval '7 days');

        RAISE EXCEPTION 'FAIL 04: duplicate refresh token hash was accepted';
    EXCEPTION
        WHEN unique_violation THEN
            NULL;
    END;
END;
$$;

SELECT 'OK 04: duplicate refresh token hash rejected' AS check;

UPDATE auth.refresh_tokens rt
SET revoked_at = now(),
    revoked_by_ip = '127.0.0.1',
    replaced_by_token_hash = 'smoke_replacement_refresh_token_hash'
FROM smoke_context ctx
WHERE rt.id = ctx.refresh_token_id;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN auth.refresh_tokens rt ON rt.id = ctx.refresh_token_id
        WHERE rt.revoked_at IS NOT NULL
          AND rt.revoked_by_ip = '127.0.0.1'
          AND rt.replaced_by_token_hash = 'smoke_replacement_refresh_token_hash'
    ) THEN
        RAISE EXCEPTION 'FAIL 05: refresh token revoke metadata was not saved';
    END IF;
END;
$$;

SELECT 'OK 05: refresh token revoked_at and replacement hash saved' AS check;

UPDATE smoke_context
SET game_state_id = game.create_new_game(account_id, 'Smoke Test Game');

SELECT 'OK 06: game.create_new_game returned game_state_id' AS check, game_state_id
FROM smoke_context;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.game_states gs ON gs.id = ctx.game_state_id
        WHERE gs.account_id = ctx.account_id
    ) THEN
        RAISE EXCEPTION 'FAIL 06: game_state is not linked to the account_id';
    END IF;
END;
$$;

SELECT 'OK 07: game_state is linked to account_id' AS check, gs.account_id, gs.id AS game_state_id
FROM smoke_context ctx
JOIN game.game_states gs ON gs.id = ctx.game_state_id;

UPDATE smoke_context ctx
SET location_id = gs.current_location_id
FROM game.game_states gs
WHERE gs.id = ctx.game_state_id;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.locations l ON l.id = ctx.location_id
        WHERE l.game_state_id = ctx.game_state_id
    ) THEN
        RAISE EXCEPTION 'FAIL 07: create_new_game did not create the starter location';
    END IF;
END;
$$;

SELECT 'OK 08: starter location exists' AS check, location_id
FROM smoke_context;

WITH inserted_player AS (
    INSERT INTO game.players (
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
    SELECT game_state_id,
           account_id,
           'Smoke Hero',
           'Smoke test character.',
           'human',
           'fighter',
           NULL,
           'Temporary smoke test character.',
           'neutral'
    FROM smoke_context
    RETURNING id
)
UPDATE smoke_context
SET player_id = inserted_player.id
FROM inserted_player;

INSERT INTO game.player_progression (player_id, game_state_id, level, experience, experience_to_next_level)
SELECT player_id, game_state_id, 1, 0, 300
FROM smoke_context;

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
SELECT player_id, game_state_id, 12, 12, 0, 0, 1, 1
FROM smoke_context;

INSERT INTO game.player_attributes (
    player_id,
    game_state_id,
    strength,
    dexterity,
    constitution,
    intelligence,
    wisdom,
    charisma
)
SELECT player_id, game_state_id, 16, 13, 15, 10, 12, 8
FROM smoke_context;

INSERT INTO game.wealth (player_id, game_state_id, gold)
SELECT player_id, game_state_id, 25
FROM smoke_context;

INSERT INTO game.player_needs (player_id, game_state_id, carry_capacity_max, carry_unit)
SELECT player_id, game_state_id, 50, 'kg'
FROM smoke_context;

INSERT INTO game.equipped_gear (player_id, game_state_id)
SELECT player_id, game_state_id
FROM smoke_context;

INSERT INTO game.combat_stats (player_id, game_state_id, armor_class, proficiency_bonus)
SELECT player_id, game_state_id, 16, 2
FROM smoke_context;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.players p ON p.id = ctx.player_id
        JOIN game.player_progression pp ON pp.player_id = p.id
        JOIN game.player_resources pr ON pr.player_id = p.id
        JOIN game.player_attributes pa ON pa.player_id = p.id
        JOIN game.wealth w ON w.player_id = p.id
        JOIN game.player_needs pn ON pn.player_id = p.id
        JOIN game.equipped_gear eg ON eg.player_id = p.id
        JOIN game.combat_stats cs ON cs.player_id = p.id
        WHERE p.game_state_id = ctx.game_state_id
    ) THEN
        RAISE EXCEPTION 'FAIL 08: test character domain rows were not created';
    END IF;
END;
$$;

SELECT 'OK 09: test character and character-domain rows exist' AS check, player_id
FROM smoke_context;

WITH inserted_sword AS (
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
        tags,
        owner_kind,
        owner_id
    )
    SELECT game_state_id,
           'smoke_longsword',
           'Smoke Longsword',
           'weapon',
           'martial_melee',
           'Temporary sword for smoke test.',
           1,
           false,
           1.5,
           '["weapon","sword"]'::jsonb,
           'player_inventory',
           player_id
    FROM smoke_context
    RETURNING id
)
UPDATE smoke_context
SET sword_id = inserted_sword.id
FROM inserted_sword;

INSERT INTO game.item_weapon_stats (game_state_id, item_id, damage_dice, damage_type, attack_bonus, damage_bonus)
SELECT game_state_id, sword_id, '1d8', 'slashing', 5, 3
FROM smoke_context;

SELECT 'OK 10: sword item created in player inventory' AS check, sword_id
FROM smoke_context;

WITH inserted_well AS (
    INSERT INTO game.world_objects (game_state_id, location_id, name, object_type, description, tags)
    SELECT game_state_id,
           location_id,
           'Smoke Well',
           'well',
           'Temporary well for smoke test.',
           '["well"]'::jsonb
    FROM smoke_context
    RETURNING id
)
UPDATE smoke_context
SET well_id = inserted_well.id
FROM inserted_well;

SELECT 'OK 11: world object well created' AS check, well_id
FROM smoke_context;

UPDATE game.equipped_gear eg
SET main_hand_item_id = ctx.sword_id
FROM smoke_context ctx
WHERE eg.player_id = ctx.player_id;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.equipped_gear eg ON eg.player_id = ctx.player_id
        WHERE eg.main_hand_item_id = ctx.sword_id
    ) THEN
        RAISE EXCEPTION 'FAIL 12: sword was not equipped in main_hand_item_id';
    END IF;
END;
$$;

SELECT 'OK 12: sword equipped in main hand' AS check;

UPDATE game.item_instances item
SET owner_kind = 'world_object',
    owner_id = ctx.well_id
FROM smoke_context ctx
WHERE item.id = ctx.sword_id
  AND item.game_state_id = ctx.game_state_id
  AND item.owner_kind = 'player_inventory'
  AND item.owner_id = ctx.player_id;

UPDATE game.equipped_gear eg
SET main_hand_item_id = NULL
FROM smoke_context ctx
WHERE eg.player_id = ctx.player_id
  AND eg.main_hand_item_id = ctx.sword_id;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.item_instances item ON item.id = ctx.sword_id
        WHERE item.owner_kind = 'world_object'
          AND item.owner_id = ctx.well_id
    ) THEN
        RAISE EXCEPTION 'FAIL 13: sword was not moved to the well world_object owner';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.equipped_gear eg ON eg.player_id = ctx.player_id
        WHERE eg.main_hand_item_id = ctx.sword_id
    ) THEN
        RAISE EXCEPTION 'FAIL 14: main_hand_item_id still references moved sword';
    END IF;
END;
$$;

SELECT 'OK 13: sword moved to well and main hand cleared' AS check;

WITH inserted_campaign AS (
    INSERT INTO game.campaign_templates (
        title,
        genre,
        tone,
        summary,
        opening_scene,
        main_goal,
        master_secrets,
        initial_flags
    )
    VALUES (
        'Smoke Campaign',
        'fantasy',
        'adventure',
        'Smoke test campaign template.',
        'The party starts near a test well.',
        'Verify the RPG extension schema.',
        '["secret smoke fact"]'::jsonb,
        '{"smoke": true}'::jsonb
    )
    RETURNING id
)
UPDATE smoke_context
SET campaign_template_id = inserted_campaign.id
FROM inserted_campaign;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.campaign_templates ct ON ct.id = ctx.campaign_template_id
        WHERE ct.title = 'Smoke Campaign'
    ) THEN
        RAISE EXCEPTION 'FAIL 15: campaign_template was not created';
    END IF;
END;
$$;

SELECT 'OK 14: campaign_template created' AS check, campaign_template_id
FROM smoke_context;

WITH inserted_story AS (
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
    SELECT game_state_id,
           campaign_template_id,
           'Act I',
           'Smoke Scene',
           'Run schema checks',
           2,
           '{"smoke_started": true}'::jsonb,
           '["The party sees a well."]'::jsonb,
           '["The well is only a test fixture."]'::jsonb,
           '["Smoke test initialized."]'::jsonb
    FROM smoke_context
    RETURNING id
)
UPDATE smoke_context
SET story_state_id = inserted_story.id
FROM inserted_story;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.story_states ss ON ss.id = ctx.story_state_id
        WHERE ss.game_state_id = ctx.game_state_id
          AND ss.campaign_template_id = ctx.campaign_template_id
          AND ss.tension_level = 2
    ) THEN
        RAISE EXCEPTION 'FAIL 16: story_state was not created for the game_state';
    END IF;
END;
$$;

SELECT 'OK 15: story_state created for game_state' AS check, story_state_id
FROM smoke_context;

WITH inserted_party AS (
    INSERT INTO game.parties (game_state_id, name)
    SELECT game_state_id, 'Smoke Party'
    FROM smoke_context
    RETURNING id
)
UPDATE smoke_context
SET party_id = inserted_party.id
FROM inserted_party;

WITH inserted_member AS (
    INSERT INTO game.party_members (
        game_state_id,
        party_id,
        account_id,
        character_id,
        role,
        status,
        display_name
    )
    SELECT game_state_id,
           party_id,
           account_id,
           player_id,
           'host',
           'active',
           'Smoke Host'
    FROM smoke_context
    RETURNING id
)
UPDATE smoke_context
SET party_member_id = inserted_member.id
FROM inserted_member;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.parties p ON p.id = ctx.party_id
        JOIN game.party_members pm ON pm.id = ctx.party_member_id
        WHERE p.game_state_id = ctx.game_state_id
          AND pm.game_state_id = ctx.game_state_id
          AND pm.party_id = p.id
          AND pm.account_id = ctx.account_id
          AND pm.character_id = ctx.player_id
          AND pm.role = 'host'
          AND pm.status = 'active'
    ) THEN
        RAISE EXCEPTION 'FAIL 17: party or party_member was not created correctly';
    END IF;
END;
$$;

SELECT 'OK 16: party and party_member created' AS check, party_id, party_member_id
FROM smoke_context;

WITH inserted_combat AS (
    INSERT INTO game.combat_states (game_state_id, is_active, round_number)
    SELECT game_state_id, true, 1
    FROM smoke_context
    RETURNING id
)
UPDATE smoke_context
SET combat_state_id = inserted_combat.id
FROM inserted_combat;

WITH inserted_character_participant AS (
    INSERT INTO game.combat_participants (
        game_state_id,
        combat_state_id,
        actor_type,
        actor_id,
        name,
        initiative,
        hp_current,
        hp_max,
        has_acted,
        conditions
    )
    SELECT game_state_id,
           combat_state_id,
           'character',
           player_id,
           'Smoke Character',
           14,
           12,
           12,
           false,
           '[]'::jsonb
    FROM smoke_context
    RETURNING id
)
UPDATE smoke_context
SET combat_participant_id = inserted_character_participant.id
FROM inserted_character_participant;

UPDATE game.combat_states cs
SET current_turn_participant_id = ctx.combat_participant_id
FROM smoke_context ctx
WHERE cs.id = ctx.combat_state_id;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.combat_states cs ON cs.id = ctx.combat_state_id
        JOIN game.combat_participants cp ON cp.id = ctx.combat_participant_id
        WHERE cs.game_state_id = ctx.game_state_id
          AND cs.is_active
          AND cs.round_number = 1
          AND cs.current_turn_participant_id = cp.id
          AND cp.game_state_id = ctx.game_state_id
          AND cp.combat_state_id = cs.id
          AND cp.actor_type = 'character'
          AND cp.actor_id = ctx.player_id
          AND cp.hp_current = 12
          AND cp.hp_max = 12
    ) THEN
        RAISE EXCEPTION 'FAIL 18: combat_state or character combat_participant was not created correctly';
    END IF;
END;
$$;

SELECT 'OK 17: combat_state and character participant created' AS check, combat_state_id, combat_participant_id
FROM smoke_context;

WITH inserted_monster AS (
    INSERT INTO game.monsters (
        game_state_id,
        location_id,
        name,
        monster_type,
        description,
        hp_current,
        hp_max,
        armor_class,
        initiative_bonus,
        stats,
        abilities,
        loot,
        tags
    )
    SELECT game_state_id,
           location_id,
           'Smoke Goblin',
           'goblin',
           'Temporary monster for smoke test.',
           7,
           7,
           13,
           2,
           '{"strength": 8, "dexterity": 14}'::jsonb,
           '[{"name": "scimitar"}]'::jsonb,
           '[{"templateId": "coin_copper", "quantity": 3}]'::jsonb,
           '["smoke","enemy"]'::jsonb
    FROM smoke_context
    RETURNING id
)
UPDATE smoke_context
SET monster_id = inserted_monster.id
FROM inserted_monster;

WITH inserted_monster_participant AS (
    INSERT INTO game.combat_participants (
        game_state_id,
        combat_state_id,
        actor_type,
        actor_id,
        name,
        initiative,
        hp_current,
        hp_max,
        has_acted,
        conditions
    )
    SELECT game_state_id,
           combat_state_id,
           'monster',
           monster_id,
           'Smoke Goblin',
           12,
           7,
           7,
           false,
           '[]'::jsonb
    FROM smoke_context
    RETURNING id
)
UPDATE smoke_context
SET monster_participant_id = inserted_monster_participant.id
FROM inserted_monster_participant;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.monsters m ON m.id = ctx.monster_id
        JOIN game.combat_participants cp ON cp.id = ctx.monster_participant_id
        WHERE m.game_state_id = ctx.game_state_id
          AND m.location_id = ctx.location_id
          AND cp.game_state_id = ctx.game_state_id
          AND cp.combat_state_id = ctx.combat_state_id
          AND cp.actor_type = 'monster'
          AND cp.actor_id = m.id
    ) THEN
        RAISE EXCEPTION 'FAIL 19: monster or monster combat_participant was not created correctly';
    END IF;
END;
$$;

SELECT 'OK 18: monster and monster combat participant created' AS check, monster_id, monster_participant_id
FROM smoke_context;

WITH inserted_turn AS (
    INSERT INTO game.game_turns (
        game_state_id,
        account_id,
        turn_number,
        player_message,
        status,
        ai_model
    )
    SELECT game_state_id,
           account_id,
           1,
           'Smoke turn message.',
           'pending',
           'smoke-model'
    FROM smoke_context
    RETURNING id
)
UPDATE smoke_context
SET game_turn_id = inserted_turn.id
FROM inserted_turn;

UPDATE game.game_turns gt
SET status = 'completed',
    master_answer = 'Smoke master answer.',
    raw_ai_response = '{"answer": "ok"}'::jsonb,
    completed_at = now()
FROM smoke_context ctx
WHERE gt.id = ctx.game_turn_id;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.game_turns gt ON gt.id = ctx.game_turn_id
        WHERE gt.game_state_id = ctx.game_state_id
          AND gt.account_id = ctx.account_id
          AND gt.status = 'completed'
          AND gt.ai_model = 'smoke-model'
          AND gt.completed_at IS NOT NULL
    ) THEN
        RAISE EXCEPTION 'FAIL 20: game_turn processing fields were not saved correctly';
    END IF;
END;
$$;

SELECT 'OK 19: game_turn AI-loop fields work' AS check, game_turn_id
FROM smoke_context;

WITH inserted_change AS (
    INSERT INTO game.game_changes (
        game_state_id,
        game_turn_id,
        operation,
        payload
    )
    SELECT game_state_id,
           game_turn_id,
           'smoke_change',
           '{"value": 1}'::jsonb
    FROM smoke_context
    RETURNING id
)
UPDATE smoke_context
SET game_change_id = inserted_change.id
FROM inserted_change;

UPDATE game.game_changes gc
SET status = 'rejected',
    reject_reason = 'Smoke rejection.',
    processed_at = now(),
    processed_by_account_id = ctx.account_id,
    error_message = 'Smoke handled error.',
    apply_result = '{"checked": true}'::jsonb
FROM smoke_context ctx
WHERE gc.id = ctx.game_change_id;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.game_changes gc ON gc.id = ctx.game_change_id
        WHERE gc.game_state_id = ctx.game_state_id
          AND gc.game_turn_id = ctx.game_turn_id
          AND gc.status = 'rejected'
          AND gc.reject_reason = 'Smoke rejection.'
          AND gc.processed_at IS NOT NULL
          AND gc.processed_by_account_id = ctx.account_id
          AND gc.error_message = 'Smoke handled error.'
          AND gc.apply_result = '{"checked": true}'::jsonb
    ) THEN
        RAISE EXCEPTION 'FAIL 21: game_change audit/apply-result fields were not saved correctly';
    END IF;
END;
$$;

SELECT 'OK 20: game_change apply/reject audit fields work' AS check, game_change_id
FROM smoke_context;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.game_state_documents doc ON doc.game_state_id = ctx.game_state_id
        WHERE doc.data ? 'персонажи'
          AND doc.data ? 'партия'
          AND doc.data ? 'мир'
          AND doc.data ? 'квесты'
          AND doc.data ? 'история'
          AND jsonb_typeof(doc.data->'персонажи') = 'array'
          AND jsonb_array_length(doc.data->'персонажи') >= 1
          AND doc.data->'мир'->>'текущаялокацияId' = ctx.location_id::text
          AND (doc.data->'мир'->'монстры') ? ctx.monster_id::text
    ) THEN
        RAISE EXCEPTION 'FAIL 22: game.game_state_documents did not return expected GameState JSON with monsters';
    END IF;
END;
$$;

SELECT
    'OK 21: game.game_state_documents returns valid GameState JSON with monsters' AS check,
    jsonb_pretty(doc.data) AS document_preview
FROM smoke_context ctx
JOIN game.game_state_documents doc ON doc.game_state_id = ctx.game_state_id;

SELECT
    'OK 22: table SELECT checks passed' AS check,
    (SELECT count(*) FROM game.campaign_templates ct JOIN smoke_context ctx ON ct.id = ctx.campaign_template_id) AS campaign_templates,
    (SELECT count(*) FROM game.story_states ss JOIN smoke_context ctx ON ss.id = ctx.story_state_id) AS story_states,
    (SELECT count(*) FROM game.parties p JOIN smoke_context ctx ON p.id = ctx.party_id) AS parties,
    (SELECT count(*) FROM game.party_members pm JOIN smoke_context ctx ON pm.id = ctx.party_member_id) AS party_members,
    (SELECT count(*) FROM game.combat_states cs JOIN smoke_context ctx ON cs.id = ctx.combat_state_id) AS combat_states,
    (SELECT count(*) FROM game.combat_participants cp JOIN smoke_context ctx ON cp.id = ctx.monster_participant_id) AS monster_combat_participants,
    (SELECT count(*) FROM game.monsters m JOIN smoke_context ctx ON m.id = ctx.monster_id) AS monsters,
    (SELECT count(*) FROM game.game_turns gt JOIN smoke_context ctx ON gt.id = ctx.game_turn_id) AS game_turns,
    (SELECT count(*) FROM game.game_changes gc JOIN smoke_context ctx ON gc.id = ctx.game_change_id) AS game_changes;

ROLLBACK;
