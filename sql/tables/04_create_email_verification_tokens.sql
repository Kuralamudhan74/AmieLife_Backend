-- ============================================================
-- Table: email_verification_tokens
-- Description: Tokens sent to users to verify their email address.
-- Tokens are single-use and expire after 24 hours.
-- ============================================================

CREATE TABLE IF NOT EXISTS email_verification_tokens (
    token_id    UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID        NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    token_hash  TEXT        NOT NULL,
    expires_at  TIMESTAMPTZ NOT NULL,
    is_used     BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_email_verification_token_hash ON email_verification_tokens (token_hash);
CREATE INDEX IF NOT EXISTS ix_email_verification_user_id    ON email_verification_tokens (user_id);
