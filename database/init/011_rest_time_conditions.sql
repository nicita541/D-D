-- Stage 3: Rest / Time / Conditions / Death
-- Additive-only migration. Existing data and schema behavior are preserved.

CREATE TABLE IF NOT EXISTS game.game_time (
    game_state_id uuid PRIMARY KEY REFERENCES game.game_states(id) ON DELETE CASCADE,
    current_day integer NOT NULL DEFAULT 1 CHECK (current_day >= 1),
    current_hour integer NOT NULL DEFAULT 8 CHECK (current_hour >= 0 AND current_hour < 24),
    current_minute integer NOT NULL DEFAULT 0 CHECK (current_minute >= 0 AND current_minute < 60),
    total_minutes integer NOT NULL DEFAULT 480 CHECK (total_minutes >= 0),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS game_time_total_minutes_idx ON game.game_time(total_minutes);

ALTER TABLE IF EXISTS game.conditions
    ADD COLUMN IF NOT EXISTS duration_minutes integer NULL CHECK (duration_minutes IS NULL OR duration_minutes >= 0),
    ADD COLUMN IF NOT EXISTS expires_at_total_minutes integer NULL CHECK (expires_at_total_minutes IS NULL OR expires_at_total_minutes >= 0),
    ADD COLUMN IF NOT EXISTS source text NULL,
    ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;

CREATE INDEX IF NOT EXISTS conditions_is_active_idx ON game.conditions(is_active);
CREATE INDEX IF NOT EXISTS conditions_expires_at_total_minutes_idx ON game.conditions(expires_at_total_minutes);

ALTER TABLE IF EXISTS game.player_resources
    ADD COLUMN IF NOT EXISTS unconscious boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS dead boolean NOT NULL DEFAULT false;

CREATE INDEX IF NOT EXISTS player_resources_unconscious_idx ON game.player_resources(unconscious);
CREATE INDEX IF NOT EXISTS player_resources_dead_idx ON game.player_resources(dead);
