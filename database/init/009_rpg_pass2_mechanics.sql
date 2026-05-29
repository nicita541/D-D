-- PASS 2: mechanic requests, combat turn foundation, and combat AC snapshot.
-- Safe to run multiple times.

CREATE TABLE IF NOT EXISTS game.mechanic_requests (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    account_id uuid NOT NULL REFERENCES auth.accounts(id) ON DELETE CASCADE,
    game_turn_id uuid NULL REFERENCES game.game_turns(id) ON DELETE SET NULL,
    game_change_id uuid NULL REFERENCES game.game_changes(id) ON DELETE SET NULL,
    request_type text NOT NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    status text NOT NULL DEFAULT 'pending',
    result jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    resolved_at timestamptz NULL,
    CONSTRAINT mechanic_requests_status_check
        CHECK (status IN ('pending', 'resolved', 'cancelled', 'failed'))
);

COMMENT ON TABLE game.mechanic_requests IS 'Pending and resolved game mechanic requests proposed by AI changes, such as ability checks.';
COMMENT ON COLUMN game.mechanic_requests.payload IS 'Normalized request payload. Plain secrets and credentials must not be stored here.';
COMMENT ON COLUMN game.mechanic_requests.result IS 'Structured result produced by backend when the mechanic request is resolved.';

CREATE INDEX IF NOT EXISTS mechanic_requests_game_state_id_idx
    ON game.mechanic_requests(game_state_id);

CREATE INDEX IF NOT EXISTS mechanic_requests_account_id_idx
    ON game.mechanic_requests(account_id);

CREATE INDEX IF NOT EXISTS mechanic_requests_status_idx
    ON game.mechanic_requests(status);

CREATE INDEX IF NOT EXISTS mechanic_requests_created_at_idx
    ON game.mechanic_requests(created_at);

CREATE INDEX IF NOT EXISTS mechanic_requests_game_turn_id_idx
    ON game.mechanic_requests(game_turn_id);

CREATE INDEX IF NOT EXISTS mechanic_requests_game_change_id_idx
    ON game.mechanic_requests(game_change_id);

ALTER TABLE IF EXISTS game.combat_states
ADD COLUMN IF NOT EXISTS round_number integer NOT NULL DEFAULT 1 CHECK (round_number >= 1);

ALTER TABLE IF EXISTS game.combat_states
ADD COLUMN IF NOT EXISTS current_turn_participant_id uuid NULL;

ALTER TABLE IF EXISTS game.combat_participants
ADD COLUMN IF NOT EXISTS has_acted boolean NOT NULL DEFAULT false;

ALTER TABLE IF EXISTS game.combat_participants
ADD COLUMN IF NOT EXISTS armor_class integer NOT NULL DEFAULT 10 CHECK (armor_class >= 0);
