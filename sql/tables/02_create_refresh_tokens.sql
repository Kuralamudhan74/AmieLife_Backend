-- ============================================================
-- Table: refresh_tokens
-- Description: Stores hashed refresh tokens per device/session.
-- Raw tokens are NEVER stored — only SHA-256 hashes.
-- ============================================================

CREATE TABLE IF NOT EXISTS refresh_tokens (
    refresh_token_id    UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id             UUID        NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    token_hash          TEXT        NOT NULL,
    expires_at          TIMESTAMPTZ NOT NULL,
    is_revoked          BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_token_hash ON refresh_tokens (token_hash);
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_user_id    ON refresh_tokens (user_id);
