CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS game.invites (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    game_state_id uuid NOT NULL REFERENCES game.game_states(id) ON DELETE CASCADE,
    token_hash text NOT NULL UNIQUE,
    role text NOT NULL DEFAULT 'player',
    max_uses integer NOT NULL DEFAULT 1,
    use_count integer NOT NULL DEFAULT 0,
    status text NOT NULL DEFAULT 'active',
    expires_at timestamptz NOT NULL,
    created_by_account_id uuid REFERENCES auth.accounts(id) ON DELETE SET NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    revoked_at timestamptz,
    revoked_by_account_id uuid REFERENCES auth.accounts(id) ON DELETE SET NULL,
    CONSTRAINT invites_role_check CHECK (role IN ('player', 'observer')),
    CONSTRAINT invites_status_check CHECK (status IN ('active', 'revoked')),
    CONSTRAINT invites_max_uses_check CHECK (max_uses > 0),
    CONSTRAINT invites_use_count_check CHECK (use_count >= 0 AND use_count <= max_uses)
);

CREATE TABLE IF NOT EXISTS game.invite_acceptances (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    invite_id uuid NOT NULL REFERENCES game.invites(id) ON DELETE CASCADE,
    account_id uuid NOT NULL REFERENCES auth.accounts(id) ON DELETE CASCADE,
    party_member_id uuid REFERENCES game.party_members(id) ON DELETE SET NULL,
    accepted_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT invite_acceptances_invite_account_unique UNIQUE (invite_id, account_id)
);

CREATE INDEX IF NOT EXISTS ix_invites_game_state_id
    ON game.invites(game_state_id);

CREATE INDEX IF NOT EXISTS ix_invites_token_hash
    ON game.invites(token_hash);

CREATE INDEX IF NOT EXISTS ix_invites_created_by_account_id
    ON game.invites(created_by_account_id);

CREATE INDEX IF NOT EXISTS ix_invite_acceptances_invite_id
    ON game.invite_acceptances(invite_id);

CREATE INDEX IF NOT EXISTS ix_invite_acceptances_account_id
    ON game.invite_acceptances(account_id);

WITH ranked AS (
    SELECT
        id,
        row_number() OVER (
            PARTITION BY game_state_id, account_id
            ORDER BY
                CASE role WHEN 'host' THEN 0 WHEN 'gm' THEN 1 WHEN 'player' THEN 2 ELSE 3 END,
                created_at,
                id
        ) AS duplicate_rank
    FROM game.party_members
    WHERE account_id IS NOT NULL
      AND status = 'active'
)
UPDATE game.party_members pm
SET status = 'inactive',
    updated_at = now()
FROM ranked r
WHERE pm.id = r.id
  AND r.duplicate_rank > 1;

CREATE UNIQUE INDEX IF NOT EXISTS ux_party_members_active_account
    ON game.party_members(game_state_id, account_id)
    WHERE account_id IS NOT NULL AND status = 'active';

CREATE OR REPLACE FUNCTION game.ensure_host_party(
    p_game_state_id uuid,
    p_account_id uuid
)
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    v_party_id uuid;
BEGIN
    INSERT INTO game.parties (game_state_id, name)
    SELECT gs.id, 'Партия'
    FROM game.game_states gs
    WHERE gs.id = p_game_state_id
      AND gs.account_id = p_account_id
    ON CONFLICT (game_state_id)
    DO UPDATE SET updated_at = game.parties.updated_at
    RETURNING id INTO v_party_id;

    IF v_party_id IS NULL THEN
        SELECT id INTO v_party_id
        FROM game.parties
        WHERE game_state_id = p_game_state_id;
    END IF;

    IF v_party_id IS NULL THEN
        RETURN NULL;
    END IF;

    INSERT INTO game.party_members (
        game_state_id,
        party_id,
        account_id,
        role,
        status,
        display_name
    )
    SELECT
        gs.id,
        v_party_id,
        gs.account_id,
        'host',
        'active',
        COALESCE(NULLIF(a.display_name, ''), a.username)
    FROM game.game_states gs
    JOIN auth.accounts a ON a.id = gs.account_id
    WHERE gs.id = p_game_state_id
      AND gs.account_id = p_account_id
      AND NOT EXISTS (
          SELECT 1
          FROM game.party_members pm
          WHERE pm.game_state_id = gs.id
            AND pm.account_id = gs.account_id
            AND pm.status = 'active'
      );

    RETURN v_party_id;
END;
$$;

INSERT INTO game.parties (game_state_id, name)
SELECT gs.id, 'Партия'
FROM game.game_states gs
WHERE NOT EXISTS (
    SELECT 1
    FROM game.parties p
    WHERE p.game_state_id = gs.id
)
ON CONFLICT (game_state_id) DO NOTHING;

INSERT INTO game.party_members (
    game_state_id,
    party_id,
    account_id,
    role,
    status,
    display_name
)
SELECT
    gs.id,
    p.id,
    gs.account_id,
    'host',
    'active',
    COALESCE(NULLIF(a.display_name, ''), a.username)
FROM game.game_states gs
JOIN game.parties p ON p.game_state_id = gs.id
JOIN auth.accounts a ON a.id = gs.account_id
WHERE NOT EXISTS (
    SELECT 1
    FROM game.party_members pm
    WHERE pm.game_state_id = gs.id
      AND pm.account_id = gs.account_id
      AND pm.status = 'active'
);

DROP TRIGGER IF EXISTS trg_invites_updated_at ON game.invites;
CREATE TRIGGER trg_invites_updated_at
BEFORE UPDATE ON game.invites
FOR EACH ROW
EXECUTE FUNCTION game.set_updated_at();

CREATE OR REPLACE FUNCTION game.create_new_game(
    p_account_id uuid,
    p_save_name text DEFAULT 'Новая игра'
)
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    v_game_state_id uuid;
    v_location_id uuid;
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM auth.accounts a
        WHERE a.id = p_account_id
          AND a.is_active
    ) THEN
        RAISE EXCEPTION 'Active account % does not exist', p_account_id
            USING ERRCODE = 'foreign_key_violation';
    END IF;

    INSERT INTO game.game_states (account_id, name)
    VALUES (p_account_id, COALESCE(NULLIF(btrim(p_save_name), ''), 'Новая игра'))
    RETURNING id INTO v_game_state_id;

    PERFORM game.ensure_host_party(v_game_state_id, p_account_id);

    INSERT INTO game.locations (game_state_id, name, description)
    VALUES (v_game_state_id, 'Начальная локация', 'Место, с которого начинается приключение.')
    RETURNING id INTO v_location_id;

    UPDATE game.game_states
    SET current_location_id = v_location_id
    WHERE id = v_game_state_id;

    INSERT INTO game.game_log_entries (game_state_id, turn_number, type, text, important)
    VALUES (v_game_state_id, 0, 'system', 'Создана новая игра.', true);

    RETURN v_game_state_id;
END;
$$;
