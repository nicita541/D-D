CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE OR REPLACE FUNCTION game.set_updated_at()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$;

ALTER TABLE game.game_turns
ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'completed';

ALTER TABLE game.game_turns
ADD COLUMN IF NOT EXISTS ai_model text;

ALTER TABLE game.game_turns
ADD COLUMN IF NOT EXISTS error_message text;

ALTER TABLE game.game_turns
ADD COLUMN IF NOT EXISTS completed_at timestamptz;

DO $$
BEGIN
    ALTER TABLE game.game_turns
    ADD CONSTRAINT game_turns_status_check
    CHECK (status IN ('pending', 'completed', 'failed'));
EXCEPTION
    WHEN duplicate_object THEN
        NULL;
END;
$$;

COMMENT ON COLUMN game.game_turns.status IS 'AI turn processing status: pending, completed, or failed.';
COMMENT ON COLUMN game.game_turns.ai_model IS 'Optional model name used for this AI-master response.';
COMMENT ON COLUMN game.game_turns.error_message IS 'Processing error text when the turn failed.';
COMMENT ON COLUMN game.game_turns.completed_at IS 'Timestamp when backend finished processing the turn.';

ALTER TABLE game.game_changes
ADD COLUMN IF NOT EXISTS processed_at timestamptz;

ALTER TABLE game.game_changes
ADD COLUMN IF NOT EXISTS processed_by_account_id uuid;

ALTER TABLE game.game_changes
ADD COLUMN IF NOT EXISTS error_message text;

ALTER TABLE game.game_changes
ADD COLUMN IF NOT EXISTS apply_result jsonb NOT NULL DEFAULT '{}'::jsonb;

DO $$
BEGIN
    ALTER TABLE game.game_changes
    ADD CONSTRAINT game_changes_processed_by_account_fkey
    FOREIGN KEY (processed_by_account_id)
    REFERENCES auth.accounts(id)
    ON DELETE SET NULL;
EXCEPTION
    WHEN duplicate_object THEN
        NULL;
END;
$$;

COMMENT ON COLUMN game.game_changes.processed_at IS 'Timestamp when backend applied or rejected this proposed change.';
COMMENT ON COLUMN game.game_changes.processed_by_account_id IS 'Account that processed this change, when processing is user/admin initiated.';
COMMENT ON COLUMN game.game_changes.error_message IS 'Processing error text when apply/reject fails.';
COMMENT ON COLUMN game.game_changes.apply_result IS 'Structured backend result from applying or rejecting the change.';

CREATE TABLE IF NOT EXISTS game.monsters (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    location_id uuid REFERENCES game.locations(id) ON DELETE SET NULL,
    name text NOT NULL,
    monster_type text,
    description text,
    hp_current integer NOT NULL DEFAULT 1 CHECK (hp_current >= 0),
    hp_max integer NOT NULL DEFAULT 1 CHECK (hp_max >= 0),
    armor_class integer NOT NULL DEFAULT 10 CHECK (armor_class >= 0),
    initiative_bonus integer NOT NULL DEFAULT 0,
    is_alive boolean NOT NULL DEFAULT true,
    stats jsonb NOT NULL DEFAULT '{}'::jsonb,
    abilities jsonb NOT NULL DEFAULT '[]'::jsonb,
    loot jsonb NOT NULL DEFAULT '[]'::jsonb,
    tags jsonb NOT NULL DEFAULT '[]'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT monsters_id_game_state_id_unique UNIQUE (id, game_state_id)
);

COMMENT ON TABLE game.monsters IS 'Monster and enemy actors that can appear in world locations and combat participants.';
COMMENT ON COLUMN game.monsters.game_state_id IS 'Save-game scope. Monster rows are isolated per game_state.';
COMMENT ON COLUMN game.monsters.location_id IS 'Optional current location. Backend must keep it in the same game_state as the monster.';
COMMENT ON COLUMN game.monsters.stats IS 'Flexible monster stats used by combat and AI context.';
COMMENT ON COLUMN game.monsters.abilities IS 'Flexible monster abilities list used by combat and AI context.';
COMMENT ON COLUMN game.monsters.loot IS 'Flexible loot descriptor. Concrete item instances should still use game.item_instances.';
COMMENT ON COLUMN game.monsters.tags IS 'Flexible tags for filtering, AI context, and encounter rules.';

CREATE INDEX IF NOT EXISTS game_turns_game_state_status_idx
    ON game.game_turns(game_state_id, status);

CREATE INDEX IF NOT EXISTS game_turns_created_at_idx
    ON game.game_turns(created_at);

CREATE INDEX IF NOT EXISTS game_changes_game_state_status_idx
    ON game.game_changes(game_state_id, status);

CREATE INDEX IF NOT EXISTS game_changes_game_turn_id_idx
    ON game.game_changes(game_turn_id);

CREATE INDEX IF NOT EXISTS game_changes_created_at_idx
    ON game.game_changes(created_at);

CREATE INDEX IF NOT EXISTS game_changes_processed_by_account_id_idx
    ON game.game_changes(processed_by_account_id);

CREATE INDEX IF NOT EXISTS abilities_player_id_idx
    ON game.abilities(player_id);

CREATE INDEX IF NOT EXISTS abilities_cost_resource_id_idx
    ON game.abilities(cost_resource_id);

CREATE INDEX IF NOT EXISTS attacks_item_id_idx
    ON game.attacks(item_id);

CREATE INDEX IF NOT EXISTS quest_reward_items_item_id_idx
    ON game.quest_reward_items(item_id);

CREATE INDEX IF NOT EXISTS location_exits_target_location_id_idx
    ON game.location_exits(target_location_id);

CREATE INDEX IF NOT EXISTS world_containers_key_item_id_idx
    ON game.world_containers(key_item_id);

CREATE INDEX IF NOT EXISTS monsters_game_state_id_idx
    ON game.monsters(game_state_id);

CREATE INDEX IF NOT EXISTS monsters_location_id_idx
    ON game.monsters(location_id);

CREATE INDEX IF NOT EXISTS monsters_name_idx
    ON game.monsters(name);

DROP TRIGGER IF EXISTS monsters_set_updated_at ON game.monsters;
CREATE TRIGGER monsters_set_updated_at
BEFORE UPDATE ON game.monsters
FOR EACH ROW
EXECUTE FUNCTION game.set_updated_at();

DO $$
BEGIN
    IF to_regclass('game.game_state_documents_v5_base') IS NULL THEN
        ALTER VIEW game.game_state_documents RENAME TO game_state_documents_v5_base;
    END IF;
END;
$$;

COMMENT ON VIEW game.game_state_documents_v5_base IS 'Internal base document view created by init/005 before monster support is added by init/007.';

CREATE OR REPLACE VIEW game.game_state_documents AS
SELECT
    base_doc.game_state_id,
    base_doc.account_id,
    jsonb_set(
        base_doc.data,
        ARRAY['мир', 'монстры'],
        COALESCE((
            SELECT jsonb_object_agg(
                m.id::text,
                jsonb_build_object(
                    'id', m.id,
                    'locationId', m.location_id,
                    'имя', m.name,
                    'тип', m.monster_type,
                    'описание', m.description,
                    'hp', jsonb_build_object(
                        'текущее', m.hp_current,
                        'максимум', m.hp_max
                    ),
                    'armorClass', m.armor_class,
                    'initiativeBonus', m.initiative_bonus,
                    'alive', m.is_alive,
                    'stats', m.stats,
                    'abilities', m.abilities,
                    'loot', m.loot,
                    'tags', m.tags
                )
                ORDER BY m.name, m.id
            )
            FROM game.monsters m
            WHERE m.game_state_id = base_doc.game_state_id
        ), '{}'::jsonb),
        true
    ) AS data
FROM game.game_state_documents_v5_base base_doc;
