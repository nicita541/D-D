CREATE TABLE IF NOT EXISTS game.dice_rolls (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    account_id uuid NOT NULL REFERENCES auth.accounts(id) ON DELETE CASCADE,
    character_id uuid REFERENCES game.players(id) ON DELETE SET NULL,
    formula text NOT NULL,
    reason text NOT NULL DEFAULT '',
    dice_count integer NOT NULL CHECK (dice_count BETWEEN 1 AND 100),
    dice_sides integer NOT NULL CHECK (dice_sides BETWEEN 1 AND 1000),
    modifier integer NOT NULL DEFAULT 0 CHECK (modifier BETWEEN -10000 AND 10000),
    rolls jsonb NOT NULL,
    total integer NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS dice_rolls_game_state_id_idx
    ON game.dice_rolls(game_state_id);

CREATE INDEX IF NOT EXISTS dice_rolls_account_id_idx
    ON game.dice_rolls(account_id);

CREATE INDEX IF NOT EXISTS dice_rolls_character_id_idx
    ON game.dice_rolls(character_id);

CREATE INDEX IF NOT EXISTS dice_rolls_created_at_idx
    ON game.dice_rolls(created_at);

CREATE TABLE IF NOT EXISTS game.skill_checks (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    account_id uuid NOT NULL REFERENCES auth.accounts(id) ON DELETE CASCADE,
    character_id uuid NOT NULL REFERENCES game.players(id) ON DELETE CASCADE,
    roll_id uuid NOT NULL REFERENCES game.dice_rolls(id) ON DELETE CASCADE,
    ability text NOT NULL,
    difficulty_class integer NOT NULL,
    total integer NOT NULL,
    success boolean NOT NULL,
    reason text NOT NULL DEFAULT '',
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT skill_checks_ability_check CHECK (ability IN ('сила', 'ловкость', 'телосложение', 'интеллект', 'мудрость', 'харизма'))
);

CREATE INDEX IF NOT EXISTS skill_checks_game_state_id_idx
    ON game.skill_checks(game_state_id);

CREATE INDEX IF NOT EXISTS skill_checks_account_id_idx
    ON game.skill_checks(account_id);

CREATE INDEX IF NOT EXISTS skill_checks_character_id_idx
    ON game.skill_checks(character_id);

CREATE INDEX IF NOT EXISTS skill_checks_roll_id_idx
    ON game.skill_checks(roll_id);

CREATE INDEX IF NOT EXISTS skill_checks_created_at_idx
    ON game.skill_checks(created_at);

CREATE TABLE IF NOT EXISTS game.campaign_memories (
    game_state_id uuid PRIMARY KEY REFERENCES game.game_states(id) ON DELETE CASCADE,
    summary text NOT NULL DEFAULT '',
    current_scene jsonb NOT NULL DEFAULT '{}'::jsonb,
    important_facts jsonb NOT NULL DEFAULT '[]'::jsonb,
    open_threads jsonb NOT NULL DEFAULT '[]'::jsonb,
    resolved_threads jsonb NOT NULL DEFAULT '[]'::jsonb,
    known_npcs jsonb NOT NULL DEFAULT '[]'::jsonb,
    known_locations jsonb NOT NULL DEFAULT '[]'::jsonb,
    master_secrets jsonb NOT NULL DEFAULT '[]'::jsonb,
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS campaign_memories_updated_at_idx
    ON game.campaign_memories(updated_at);

DROP TRIGGER IF EXISTS campaign_memories_set_updated_at ON game.campaign_memories;
CREATE TRIGGER campaign_memories_set_updated_at
BEFORE UPDATE ON game.campaign_memories
FOR EACH ROW
EXECUTE FUNCTION game.set_updated_at();

COMMENT ON TABLE game.dice_rolls IS 'Persistent dice roll history scoped to one save game and account.';
COMMENT ON TABLE game.skill_checks IS 'Ability checks built on top of dice_rolls.';
COMMENT ON TABLE game.campaign_memories IS 'Long-term campaign memory used by backend and AI context.';
COMMENT ON COLUMN game.campaign_memories.master_secrets IS 'GM-only memory. Frontend player UI must not show this unless a GM mode exists.';
