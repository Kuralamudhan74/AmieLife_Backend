-- ============================================================
-- Seed data for development / testing
-- DO NOT run in production
-- ============================================================

-- Admin user (password: Admin@123456 — change immediately)
INSERT INTO users (
    user_id, email, password_hash, first_name, last_name,
    role, is_email_verified, is_guest, status
)
VALUES (
    gen_random_uuid(),
    'admin@amielife.dev',
    -- BCrypt hash of "Admin@123456" — regenerate for real environments
    '$2a$12$PLACEHOLDER_CHANGE_THIS_BEFORE_RUNNING',
    'Admin',
    'User',
    'Admin',
    TRUE,
    FALSE,
    'Active'
)
ON CONFLICT DO NOTHING;
