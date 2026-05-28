\set ON_ERROR_STOP on

BEGIN;

CREATE TEMP TABLE smoke_context (
    account_id uuid,
    game_state_id uuid,
    player_id uuid,
    location_id uuid,
    sword_id uuid,
    well_id uuid
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

UPDATE smoke_context
SET game_state_id = game.create_new_game(account_id, 'Smoke Test Game');

SELECT 'OK 02: game.create_new_game returned game_state_id' AS check, game_state_id
FROM smoke_context;

UPDATE smoke_context ctx
SET player_id = p.id
FROM game.players p
WHERE p.game_state_id = ctx.game_state_id;

UPDATE smoke_context ctx
SET location_id = gs.current_location_id
FROM game.game_states gs
WHERE gs.id = ctx.game_state_id;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.game_states gs ON gs.id = ctx.game_state_id
        JOIN game.players p ON p.id = ctx.player_id
        JOIN game.player_progression pp ON pp.player_id = p.id
        JOIN game.player_resources pr ON pr.player_id = p.id
        JOIN game.player_attributes pa ON pa.player_id = p.id
        JOIN game.wealth w ON w.player_id = p.id
        JOIN game.player_needs pn ON pn.player_id = p.id
        JOIN game.equipped_gear eg ON eg.player_id = p.id
        JOIN game.combat_stats cs ON cs.player_id = p.id
        JOIN game.locations l ON l.id = ctx.location_id
    ) THEN
        RAISE EXCEPTION 'FAIL 03: create_new_game did not create all default rows';
    END IF;
END;
$$;

SELECT 'OK 03: default rows exist' AS check;

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

SELECT 'OK 04: sword item created in player inventory' AS check, sword_id
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

SELECT 'OK 05: world object well created' AS check, well_id
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
        RAISE EXCEPTION 'FAIL 06: sword was not equipped in main_hand_item_id';
    END IF;
END;
$$;

SELECT 'OK 06: sword equipped in main hand' AS check;

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
        RAISE EXCEPTION 'FAIL 07: sword was not moved to the well world_object owner';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.equipped_gear eg ON eg.player_id = ctx.player_id
        WHERE eg.main_hand_item_id = ctx.sword_id
    ) THEN
        RAISE EXCEPTION 'FAIL 08: main_hand_item_id still references moved sword';
    END IF;
END;
$$;

SELECT 'OK 07: sword moved to well and main hand cleared' AS check;

DO $$
DECLARE
    k_player text := convert_from(decode('d0b8d0b3d180d0bed0ba', 'hex'), 'UTF8');
    k_world text := convert_from(decode('d0bcd0b8d180', 'hex'), 'UTF8');
    k_quests text := convert_from(decode('d0bad0b2d0b5d181d182d18b', 'hex'), 'UTF8');
    k_history text := convert_from(decode('d0b8d181d182d0bed180d0b8d18f', 'hex'), 'UTF8');
    k_current_location text := convert_from(decode('d182d0b5d0bad183d189d0b0d18fd0bbd0bed0bad0b0d186d0b8d18f4964', 'hex'), 'UTF8');
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM smoke_context ctx
        JOIN game.game_state_documents doc ON doc.game_state_id = ctx.game_state_id
        WHERE doc.data ? k_player
          AND doc.data ? k_world
          AND doc.data ? k_quests
          AND doc.data ? k_history
          AND doc.data->k_world->>k_current_location = ctx.location_id::text
    ) THEN
        RAISE EXCEPTION 'FAIL 09: game.game_state_documents did not return expected JSON';
    END IF;
END;
$$;

SELECT
    'OK 08: game.game_state_documents returns valid top-level JSON' AS check,
    jsonb_pretty(doc.data) AS document_preview
FROM smoke_context ctx
JOIN game.game_state_documents doc ON doc.game_state_id = ctx.game_state_id;

ROLLBACK;
