-- ============================================================
-- Additional indexes (beyond those in table creation scripts)
-- Run after table creation if not already present.
-- ============================================================

-- Composite index for lockout queries
CREATE INDEX IF NOT EXISTS ix_users_lockout
    ON users (lockout_end_time)
    WHERE lockout_end_time IS NOT NULL;

-- Index for filtering active / non-revoked tokens (partial index)
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_active
    ON refresh_tokens (user_id, expires_at)
    WHERE is_revoked = FALSE;

-- Index for active password reset tokens
CREATE INDEX IF NOT EXISTS ix_password_reset_active
    ON password_reset_tokens (user_id)
    WHERE is_used = FALSE;

-- Index for unused email verification tokens
CREATE INDEX IF NOT EXISTS ix_email_verification_active
    ON email_verification_tokens (user_id)
    WHERE is_used = FALSE;
