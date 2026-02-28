-- ============================================================
-- Table: password_reset_tokens
-- Description: Short-lived tokens for password reset.
-- After use, is_used = TRUE. Old refresh tokens are also revoked.
-- ============================================================

CREATE TABLE IF NOT EXISTS password_reset_tokens (
    reset_id    UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID        NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    token_hash  TEXT        NOT NULL,
    expires_at  TIMESTAMPTZ NOT NULL,
    is_used     BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_password_reset_token_hash ON password_reset_tokens (token_hash);
CREATE INDEX IF NOT EXISTS ix_password_reset_user_id    ON password_reset_tokens (user_id);
