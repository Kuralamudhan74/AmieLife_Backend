# Database Schema Reference

> Database: PostgreSQL (via Supabase for development)
> ORM: EF Core 8 (Code First — migrations generate the schema)
> SQL reference scripts: `/sql/tables/`

---

## Tables

### `users`
Core identity table. Stores both registered and guest users.

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| `user_id` | UUID | NO | `gen_random_uuid()` | Primary key |
| `email` | VARCHAR(255) | NO | — | Unique (case-insensitive) |
| `phone_number` | VARCHAR(20) | YES | NULL | Optional |
| `password_hash` | TEXT | YES | NULL | BCrypt hash. NULL for guests/OAuth |
| `first_name` | VARCHAR(100) | YES | NULL | |
| `last_name` | VARCHAR(100) | YES | NULL | |
| `role` | VARCHAR(20) | NO | `'Customer'` | Enum: Customer, Admin |
| `is_email_verified` | BOOLEAN | NO | `false` | Must be true to login |
| `is_guest` | BOOLEAN | NO | `false` | Guest checkout users |
| `failed_login_attempts` | INT | NO | `0` | Reset on success |
| `lockout_end_time` | TIMESTAMPTZ | YES | NULL | NULL = not locked |
| `status` | VARCHAR(20) | NO | `'Active'` | Enum: Active, Suspended, Deleted |
| `created_at` | TIMESTAMPTZ | NO | `NOW()` | |
| `updated_at` | TIMESTAMPTZ | NO | `NOW()` | Update on every change |

**Indexes:**
- `ix_users_email` — UNIQUE on `LOWER(email)`
- `ix_users_lockout` — Partial index on `lockout_end_time WHERE NOT NULL`

---

### `refresh_tokens`
One row per active session. A user can have multiple (one per device).

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| `refresh_token_id` | UUID | NO | `gen_random_uuid()` | Primary key |
| `user_id` | UUID | NO | — | FK → users |
| `token_hash` | TEXT | NO | — | SHA-256 hex of the raw token |
| `expires_at` | TIMESTAMPTZ | NO | — | 14 days from creation |
| `is_revoked` | BOOLEAN | NO | `false` | True after use or logout |
| `created_at` | TIMESTAMPTZ | NO | `NOW()` | |

**Indexes:**
- `ix_refresh_tokens_token_hash` — for fast lookup by hash
- `ix_refresh_tokens_user_id` — for revoking all tokens on password change
- `ix_refresh_tokens_active` — Partial index on `(user_id, expires_at) WHERE is_revoked=false`

---

### `addresses`
User shipping and billing addresses. Multiple per user, one can be default.

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| `address_id` | UUID | NO | `gen_random_uuid()` | Primary key |
| `user_id` | UUID | NO | — | FK → users |
| `full_name` | VARCHAR(150) | NO | — | Recipient name |
| `phone_number` | VARCHAR(20) | NO | — | |
| `address_line1` | VARCHAR(255) | NO | — | |
| `address_line2` | VARCHAR(255) | YES | NULL | |
| `city` | VARCHAR(100) | NO | — | |
| `state` | VARCHAR(100) | NO | — | |
| `postal_code` | VARCHAR(20) | NO | — | |
| `country` | VARCHAR(100) | NO | — | |
| `is_default` | BOOLEAN | NO | `false` | |
| `created_at` | TIMESTAMPTZ | NO | `NOW()` | |

---

### `email_verification_tokens`
Single-use tokens for verifying email ownership.

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| `token_id` | UUID | NO | `gen_random_uuid()` | Primary key |
| `user_id` | UUID | NO | — | FK → users |
| `token_hash` | TEXT | NO | — | SHA-256 of raw token |
| `expires_at` | TIMESTAMPTZ | NO | — | 24 hours from creation |
| `is_used` | BOOLEAN | NO | `false` | True after successful verify |
| `created_at` | TIMESTAMPTZ | NO | `NOW()` | |

---

### `password_reset_tokens`
Single-use tokens for password reset flow.

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| `reset_id` | UUID | NO | `gen_random_uuid()` | Primary key |
| `user_id` | UUID | NO | — | FK → users |
| `token_hash` | TEXT | NO | — | SHA-256 of raw token |
| `expires_at` | TIMESTAMPTZ | NO | — | 30 minutes from creation |
| `is_used` | BOOLEAN | NO | `false` | True after password changed |
| `created_at` | TIMESTAMPTZ | NO | `NOW()` | |

---

## Entity Relationship Diagram (Text)

```
users (1) ──────────── (many) refresh_tokens
users (1) ──────────── (many) addresses
users (1) ──────────── (many) email_verification_tokens
users (1) ──────────── (many) password_reset_tokens
```

All foreign keys use `ON DELETE CASCADE` — deleting a user removes all related records.
