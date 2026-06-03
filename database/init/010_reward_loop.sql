-- Gameplay tranche 2: reward loop foundation.
-- Additive only: no existing tables or columns are removed.

ALTER TABLE IF EXISTS game.monsters
ADD COLUMN IF NOT EXISTS xp_reward integer NOT NULL DEFAULT 0 CHECK (xp_reward >= 0);

ALTER TABLE IF EXISTS game.monsters
ADD COLUMN IF NOT EXISTS currency_reward integer NOT NULL DEFAULT 0 CHECK (currency_reward >= 0);

ALTER TABLE IF EXISTS game.monsters
ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'alive';

DO $$
BEGIN
    ALTER TABLE game.monsters
    ADD CONSTRAINT monsters_status_check
    CHECK (status IN ('alive', 'dead', 'fled'));
EXCEPTION
    WHEN duplicate_object THEN
        NULL;
END;
$$;

UPDATE game.monsters
SET status = CASE WHEN is_alive THEN 'alive' ELSE 'dead' END
WHERE status IS NULL OR status = '';

ALTER TABLE IF EXISTS game.combat_participants
ADD COLUMN IF NOT EXISTS is_enemy boolean NOT NULL DEFAULT false;

ALTER TABLE IF EXISTS game.combat_participants
ADD COLUMN IF NOT EXISTS xp_reward integer NOT NULL DEFAULT 0 CHECK (xp_reward >= 0);

ALTER TABLE IF EXISTS game.combat_participants
ADD COLUMN IF NOT EXISTS currency_reward integer NOT NULL DEFAULT 0 CHECK (currency_reward >= 0);

CREATE TABLE IF NOT EXISTS game.loot_containers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    source_type text,
    source_id uuid,
    name text NOT NULL,
    status text NOT NULL DEFAULT 'available',
    currency_amount integer NOT NULL DEFAULT 0 CHECK (currency_amount >= 0),
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    claimed_at timestamptz,
    claimed_by_character_id uuid,
    CONSTRAINT loot_containers_status_check CHECK (status IN ('available', 'claimed', 'discarded')),
    CONSTRAINT loot_containers_character_fkey FOREIGN KEY (claimed_by_character_id, game_state_id)
        REFERENCES game.players(id, game_state_id) ON DELETE SET NULL,
    CONSTRAINT loot_containers_id_game_state_id_unique UNIQUE (id, game_state_id)
);

CREATE INDEX IF NOT EXISTS loot_containers_game_state_id_idx
    ON game.loot_containers(game_state_id);

CREATE INDEX IF NOT EXISTS loot_containers_status_idx
    ON game.loot_containers(status);

CREATE INDEX IF NOT EXISTS loot_containers_source_idx
    ON game.loot_containers(source_type, source_id);

CREATE TABLE IF NOT EXISTS game.loot_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    loot_container_id uuid NOT NULL,
    name text NOT NULL,
    description text,
    quantity integer NOT NULL DEFAULT 1 CHECK (quantity > 0),
    item_type text,
    rarity text,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT loot_items_container_fkey FOREIGN KEY (loot_container_id, game_state_id)
        REFERENCES game.loot_containers(id, game_state_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS loot_items_game_state_id_idx
    ON game.loot_items(game_state_id);

CREATE INDEX IF NOT EXISTS loot_items_container_idx
    ON game.loot_items(loot_container_id);

CREATE TABLE IF NOT EXISTS game.quest_rewards (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    quest_id uuid NOT NULL,
    xp_amount integer NOT NULL DEFAULT 0 CHECK (xp_amount >= 0),
    currency_amount integer NOT NULL DEFAULT 0 CHECK (currency_amount >= 0),
    items jsonb NOT NULL DEFAULT '[]'::jsonb,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    granted_at timestamptz,
    granted_to_character_id uuid,
    CONSTRAINT quest_rewards_quest_fkey FOREIGN KEY (quest_id, game_state_id)
        REFERENCES game.quests(id, game_state_id) ON DELETE CASCADE,
    CONSTRAINT quest_rewards_character_fkey FOREIGN KEY (granted_to_character_id, game_state_id)
        REFERENCES game.players(id, game_state_id) ON DELETE SET NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS quest_rewards_one_grant_per_quest_idx
    ON game.quest_rewards(quest_id)
    WHERE granted_at IS NOT NULL;

CREATE INDEX IF NOT EXISTS quest_rewards_game_state_id_idx
    ON game.quest_rewards(game_state_id);

CREATE INDEX IF NOT EXISTS quest_rewards_quest_id_idx
    ON game.quest_rewards(quest_id);
