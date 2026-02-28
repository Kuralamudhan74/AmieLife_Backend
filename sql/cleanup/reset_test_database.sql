-- ============================================================
-- AmieLife — Test Database Cleanup Script
-- PURPOSE : Wipe all data and return DB to a clean initial state
-- USE WHEN: Testing phase — clearing junk data between test runs
-- WARNING : DO NOT RUN IN PRODUCTION — all user data will be lost
-- ============================================================

-- Single TRUNCATE with CASCADE handles all FK-dependent tables
-- (refresh_tokens, addresses, email_verification_tokens, password_reset_tokens)
-- because they all have ON DELETE CASCADE → users

TRUNCATE TABLE users CASCADE;

-- Verify all tables are empty after cleanup
SELECT
  'users'                      AS table_name, COUNT(*) AS row_count FROM users
UNION ALL
SELECT 'refresh_tokens',                       COUNT(*) FROM refresh_tokens
UNION ALL
SELECT 'addresses',                            COUNT(*) FROM addresses
UNION ALL
SELECT 'email_verification_tokens',            COUNT(*) FROM email_verification_tokens
UNION ALL
SELECT 'password_reset_tokens',                COUNT(*) FROM password_reset_tokens;

-- ============================================================
-- OPTIONAL: Re-seed a test admin user after cleanup
-- Generate a real BCrypt hash using: https://bcrypt-generator.com (work factor 12)
-- Or run this in C#: BCrypt.Net.BCrypt.HashPassword("Admin@TestOnly123", 12)
-- ============================================================
-- INSERT INTO users (
--     email, password_hash, first_name, last_name,
--     role, is_email_verified, is_guest, status
-- ) VALUES (
--     'admin@amielife.dev',
--     '$2a$12$YOUR_BCRYPT_HASH_HERE',
--     'Admin', 'User',
--     'Admin', TRUE, FALSE, 'Active'
-- );
