CREATE TABLE IF NOT EXISTS game.account_characters (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL REFERENCES auth.accounts(id) ON DELETE CASCADE,
    name text NOT NULL DEFAULT 'Герой',
    species text,
    class_name text,
    background text,
    description text,
    alignment text,
    attributes jsonb NOT NULL DEFAULT '{}'::jsonb,
    resources jsonb NOT NULL DEFAULT '{}'::jsonb,
    progression jsonb NOT NULL DEFAULT '{}'::jsonb,
    wealth jsonb NOT NULL DEFAULT '{}'::jsonb,
    combat jsonb NOT NULL DEFAULT '{}'::jsonb,
    inventory jsonb NOT NULL DEFAULT '[]'::jsonb,
    equipment jsonb NOT NULL DEFAULT '{}'::jsonb,
    attacks jsonb NOT NULL DEFAULT '[]'::jsonb,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    version integer NOT NULL DEFAULT 1 CHECK (version >= 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_account_characters_account_updated
    ON game.account_characters(account_id, updated_at DESC);

CREATE INDEX IF NOT EXISTS ix_account_characters_account_name
    ON game.account_characters(account_id, name);

CREATE OR REPLACE FUNCTION game.json_int(
    p_source jsonb,
    p_key text,
    p_fallback integer
)
RETURNS integer
LANGUAGE sql
IMMUTABLE
AS $$
    SELECT CASE
        WHEN p_source ? p_key AND (p_source ->> p_key) ~ '^-?[0-9]+$'
            THEN (p_source ->> p_key)::integer
        ELSE p_fallback
    END;
$$;

CREATE OR REPLACE FUNCTION game.json_numeric(
    p_source jsonb,
    p_key text,
    p_fallback numeric
)
RETURNS numeric
LANGUAGE sql
IMMUTABLE
AS $$
    SELECT CASE
        WHEN p_source ? p_key AND (p_source ->> p_key) ~ '^-?[0-9]+(\.[0-9]+)?$'
            THEN (p_source ->> p_key)::numeric
        ELSE p_fallback
    END;
$$;

DROP TRIGGER IF EXISTS trg_account_characters_updated_at ON game.account_characters;
CREATE TRIGGER trg_account_characters_updated_at
BEFORE UPDATE ON game.account_characters
FOR EACH ROW
EXECUTE FUNCTION game.set_updated_at();

ALTER TABLE game.players
    ADD COLUMN IF NOT EXISTS account_character_id uuid;

ALTER TABLE game.players
    ADD COLUMN IF NOT EXISTS account_character_version integer;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'players_account_character_fkey'
          AND conrelid = 'game.players'::regclass
    ) THEN
        ALTER TABLE game.players
            ADD CONSTRAINT players_account_character_fkey
            FOREIGN KEY (account_character_id)
            REFERENCES game.account_characters(id)
            ON DELETE SET NULL;
    END IF;
END;
$$;

CREATE INDEX IF NOT EXISTS ix_players_account_character_id
    ON game.players(account_character_id)
    WHERE account_character_id IS NOT NULL;
