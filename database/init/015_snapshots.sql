CREATE TABLE IF NOT EXISTS game.snapshots (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    created_by_account_id uuid REFERENCES auth.accounts(id) ON DELETE SET NULL,
    restored_by_account_id uuid REFERENCES auth.accounts(id) ON DELETE SET NULL,
    reason text NOT NULL DEFAULT '',
    payload jsonb NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    restored_at timestamptz
);

CREATE INDEX IF NOT EXISTS ix_snapshots_game_state_id_created_at
    ON game.snapshots(game_state_id, created_at DESC);

CREATE INDEX IF NOT EXISTS ix_snapshots_created_by_account_id
    ON game.snapshots(created_by_account_id);
