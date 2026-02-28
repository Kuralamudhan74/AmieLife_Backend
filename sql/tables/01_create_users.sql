-- ============================================================
-- Table: users
-- Description: Core user table. Stores both registered and guest users.
-- Guest users: password_hash = NULL, is_guest = TRUE
-- ============================================================

CREATE TABLE IF NOT EXISTS users (
    user_id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email               VARCHAR(255) NOT NULL,
    phone_number        VARCHAR(20)  NULL,
    password_hash       TEXT         NULL,
    first_name          VARCHAR(100) NULL,
    last_name           VARCHAR(100) NULL,
    role                VARCHAR(20)  NOT NULL DEFAULT 'Customer',
    is_email_verified   BOOLEAN      NOT NULL DEFAULT FALSE,
    is_guest            BOOLEAN      NOT NULL DEFAULT FALSE,
    failed_login_attempts INT        NOT NULL DEFAULT 0,
    lockout_end_time    TIMESTAMPTZ  NULL,
    status              VARCHAR(20)  NOT NULL DEFAULT 'Active',
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_users_role   CHECK (role   IN ('Customer', 'Admin')),
    CONSTRAINT chk_users_status CHECK (status IN ('Active', 'Suspended', 'Deleted'))
);

-- Unique email constraint
CREATE UNIQUE INDEX IF NOT EXISTS ix_users_email ON users (LOWER(email));
