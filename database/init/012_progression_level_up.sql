-- Stage 4: progression / level-up MVP.
-- Additive only: existing progression data and API contracts stay intact.

ALTER TABLE IF EXISTS game.player_progression
ADD COLUMN IF NOT EXISTS level_up_available boolean NOT NULL DEFAULT false;

ALTER TABLE IF EXISTS game.player_progression
ADD COLUMN IF NOT EXISTS proficiency_bonus integer NOT NULL DEFAULT 2 CHECK (proficiency_bonus >= 0);

ALTER TABLE IF EXISTS game.player_progression
ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now();

CREATE INDEX IF NOT EXISTS player_progression_game_state_idx
    ON game.player_progression(game_state_id);

CREATE INDEX IF NOT EXISTS player_progression_level_up_available_idx
    ON game.player_progression(game_state_id, level_up_available)
    WHERE level_up_available = true;

UPDATE game.player_progression
SET experience_to_next_level = CASE
        WHEN level <= 1 THEN 300
        WHEN level = 2 THEN 900
        WHEN level = 3 THEN 2700
        WHEN level = 4 THEN 6500
        ELSE 0
    END,
    proficiency_bonus = CASE
        WHEN level >= 5 THEN 3
        ELSE 2
    END,
    level_up_available = CASE
        WHEN level <= 1 THEN experience >= 300
        WHEN level = 2 THEN experience >= 900
        WHEN level = 3 THEN experience >= 2700
        WHEN level = 4 THEN experience >= 6500
        ELSE false
    END,
    updated_at = now();
