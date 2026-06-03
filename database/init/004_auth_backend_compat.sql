ALTER TABLE auth.accounts
ADD COLUMN IF NOT EXISTS role text NOT NULL DEFAULT 'user';

ALTER TABLE auth.accounts
DROP CONSTRAINT IF EXISTS accounts_role_check;

ALTER TABLE auth.accounts
ADD CONSTRAINT accounts_role_check
CHECK (role IN ('user', 'admin'));

ALTER TABLE auth.refresh_tokens
ADD COLUMN IF NOT EXISTS created_by_ip text;

ALTER TABLE auth.refresh_tokens
ADD COLUMN IF NOT EXISTS revoked_by_ip text;

ALTER TABLE auth.refresh_tokens
ADD COLUMN IF NOT EXISTS user_agent text;

ALTER TABLE auth.refresh_tokens
ADD COLUMN IF NOT EXISTS replaced_by_token_hash text;

CREATE UNIQUE INDEX IF NOT EXISTS ux_refresh_tokens_token_hash
ON auth.refresh_tokens(token_hash);

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_account_id
ON auth.refresh_tokens(account_id);

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_expires_at
ON auth.refresh_tokens(expires_at);

CREATE UNIQUE INDEX IF NOT EXISTS ux_accounts_email_lower
ON auth.accounts (lower(email));

CREATE UNIQUE INDEX IF NOT EXISTS ux_accounts_username_lower
ON auth.accounts (lower(username));
