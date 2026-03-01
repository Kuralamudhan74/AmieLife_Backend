# API Testing Guide — Authentication Module

> Use Swagger UI (http://localhost:5000) or any REST client (Postman, Insomnia, curl)
> Base URL: http://localhost:5000/api/v1/auth

---

## Email Delivery Mode

The API supports two email delivery modes, controlled by `Smtp:Enabled` in configuration:

| Mode | Config | Where tokens appear |
|---|---|---|
| **Real SMTP** (default for dev) | `Smtp:Enabled = true` | Check your **actual email inbox** (via Brevo) |
| **Console stub** (fallback) | `Smtp:Enabled = false` | Check the **API console/terminal logs** |

> To switch modes, change `Smtp:Enabled` in `appsettings.Development.json` and restart.

## Complete Flow Order

```
1. /signup          → Create account
2. /verify-email    → Confirm email (token arrives via email or console, see above)
3. /login           → Get access + refresh tokens
4. /refresh         → Renew tokens silently (client-side, when access token expires)
5. /logout          → End session
6. /forgot-password → Request password reset
7. /reset-password  → Set new password (token arrives via email or console)
8. /guest           → (Independent) Guest checkout flow
```

---

## Step-by-Step Workflow

---

### STEP 1 — Signup

**POST** `/api/v1/auth/signup`

```json
{
  "email": "testuser@example.com",
  "password": "Test@12345",
  "confirmPassword": "Test@12345",
  "firstName": "Test",
  "lastName": "User",
  "phoneNumber": null
}
```

**What happens:**
- User is created in DB with `is_email_verified = false`
- A verification token is generated (hashed, stored in DB)
- A verification email is sent (via SMTP or logged to console, depending on config)

**If `Smtp:Enabled = true` (real email via Brevo):**
Check the email inbox for `testuser@example.com` — you will receive a professional HTML email with a "Verify Email Address" button. The verification token is embedded in the button URL.

**If `Smtp:Enabled = false` (console fallback):**
Check the API console log — you will see:
```
[EMAIL STUB] Verification email to testuser@example.com.
URL: http://localhost:3000/verify-email?token=RAW_TOKEN_HERE
```

**Copy the token value** from the `?token=` parameter (in the email link or console URL). You need it in Step 2.

**Expected response (200):**
```json
{ "message": "Registration successful. Please check your email to verify..." }
```

---

### STEP 2 — Verify Email

**POST** `/api/v1/auth/verify-email`

Take the token from Step 1 (from the email link or console log):
```json
{
  "token": "PASTE_THE_TOKEN_FROM_CONSOLE_LOG_HERE"
}
```

**What happens:**
- `email_verification_tokens` row marked `is_used = true`
- `users.is_email_verified` set to `true`

**Expected response (200):**
```json
{ "message": "Email verified successfully. You may now log in." }
```

---

### STEP 3 — Login

**POST** `/api/v1/auth/login`

```json
{
  "email": "testuser@example.com",
  "password": "Test@12345"
}
```

**What happens:**
- Validates password with BCrypt
- Generates JWT access token (15 min) + refresh token (14 days)
- Refresh token stored as SHA-256 hash in DB

**Expected response (200):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "raw_refresh_token_value",
  "expiresInSeconds": 900,
  "tokenType": "Bearer"
}
```

**Save both tokens.** Use `accessToken` as `Authorization: Bearer <token>` for protected routes.

---

### STEP 4 — Refresh Token (when access token expires)

**POST** `/api/v1/auth/refresh`

```json
{
  "refreshToken": "PASTE_REFRESH_TOKEN_FROM_STEP_3"
}
```

**What happens:**
- Old refresh token revoked
- New access token + new refresh token issued (rotation)

**Expected response (200):** Same as login response with new tokens.

---

### STEP 5 — Logout

**POST** `/api/v1/auth/logout`

```json
{
  "refreshToken": "PASTE_CURRENT_REFRESH_TOKEN"
}
```

**What happens:**
- Refresh token marked `is_revoked = true` in DB
- Access token expires naturally (15 min)

**Expected response (200):**
```json
{ "message": "Logged out successfully." }
```

---

### STEP 6 — Forgot Password

**POST** `/api/v1/auth/forgot-password`

```json
{
  "email": "testuser@example.com"
}
```

**If `Smtp:Enabled = true` (real email via Brevo):**
Check the email inbox — you will receive a "Reset Password" email with a button link.

**If `Smtp:Enabled = false` (console fallback):**
Check the API console log:
```
[EMAIL STUB] Password reset email to testuser@example.com.
URL: http://localhost:3000/reset-password?token=RAW_RESET_TOKEN_HERE
```

**Copy the token value** from `?token=` (email link or console URL) — needed in Step 7.

**Expected response (200):**
```json
{ "message": "If an account with that email exists, a reset link has been sent." }
```

> Always returns 200 even for non-existent emails — this is intentional (prevents email enumeration).

---

### STEP 7 — Reset Password

**POST** `/api/v1/auth/reset-password`

```json
{
  "token": "PASTE_RESET_TOKEN_FROM_STEP_6_CONSOLE",
  "newPassword": "NewPass@99999",
  "confirmNewPassword": "NewPass@99999"
}
```

**What happens:**
- Reset token marked `is_used = true`
- Password updated with new BCrypt hash
- ALL refresh tokens for this user revoked (forces re-login on all devices)

**Expected response (200):**
```json
{ "message": "Password reset successfully. All sessions have been invalidated." }
```

---

### STEP 8 — Guest User (Independent Flow)

**POST** `/api/v1/auth/guest`

```json
{
  "email": "guest@example.com"
}
```

**What happens:**
- User created with `is_guest = true`, `password_hash = null`
- Short-lived JWT issued (15 min), no refresh token

**Expected response (200):**
```json
{
  "userId": "uuid-here",
  "email": "guest@example.com",
  "guestAccessToken": "eyJhbGci..."
}
```

---

## Positive & Negative Test Cases

---

### 1. SIGNUP

| | Request | Expected |
|---|---|---|
| ✅ **Positive** | Valid email + strong password | 200 — "Registration successful, check email" |
| ❌ **Negative** | Same email again | 400 — "An account with this email already exists." |
| ❌ **Negative** | Weak password `abc123` | 400 — validation errors (missing uppercase, special char) |
| ❌ **Negative** | Passwords don't match | 400 — "Passwords do not match." |

---

### 2. VERIFY EMAIL

| | Request | Expected |
|---|---|---|
| ✅ **Positive** | Valid token from console log | 200 — "Email verified successfully" |
| ❌ **Negative** | Wrong/random token | 400 — "The verification link is invalid or has expired." |
| ❌ **Negative** | Same valid token used twice | 400 — "The verification link is invalid or has expired." |

---

### 3. LOGIN

| | Request | Expected |
|---|---|---|
| ✅ **Positive** | Correct email + password (after verification) | 200 — access + refresh tokens |
| ❌ **Negative** | Correct email, wrong password | 400 — "Invalid email or password." |
| ❌ **Negative** | Login before email verification | 400 — "Please verify your email address before logging in." |
| ❌ **Negative** | 5 failed attempts → 6th attempt | 400 — "Account is temporarily locked..." |
| ❌ **Negative** | Non-existent email | 400 — "Invalid email or password." (same generic message) |

---

### 4. REFRESH TOKEN

| | Request | Expected |
|---|---|---|
| ✅ **Positive** | Valid current refresh token | 200 — new token pair issued, old revoked |
| ❌ **Negative** | Already-used refresh token (replay attack) | 401 — "Invalid or expired refresh token." |
| ❌ **Negative** | Random/garbage string | 401 — "Invalid or expired refresh token." |

---

### 5. LOGOUT

| | Request | Expected |
|---|---|---|
| ✅ **Positive** | Valid refresh token | 200 — "Logged out successfully." |
| ✅ **Negative** | Invalid/already-revoked token | 200 — still success (no token probing) |

> Note: Logout always returns 200 — this is intentional security design.

---

### 6. FORGOT PASSWORD

| | Request | Expected |
|---|---|---|
| ✅ **Positive** | Registered email | 200 — check console for reset token |
| ✅ **Negative** | Non-existent email | 200 — same message (anti-enumeration) |

---

### 7. RESET PASSWORD

| | Request | Expected |
|---|---|---|
| ✅ **Positive** | Valid token + strong new password | 200 — password updated, all sessions revoked |
| ❌ **Negative** | Wrong token | 400 — "The password reset link is invalid or has expired." |
| ❌ **Negative** | Passwords don't match | 400 — validation error |
| ❌ **Negative** | Reuse the same token | 400 — "The password reset link is invalid or has expired." |

---

### 8. GUEST USER

| | Request | Expected |
|---|---|---|
| ✅ **Positive** | Email not used by any registered user | 200 — guest user + access token |
| ❌ **Negative** | Email belongs to a registered (non-guest) user | 400 — "An account with this email already exists. Please log in." |

---

## Database Cleanup (Reset for Fresh Test Run)

Run this in Supabase SQL Editor or any PostgreSQL client:

```sql
TRUNCATE TABLE users CASCADE;
```

This single command wipes **all tables** (users, refresh_tokens, addresses,
email_verification_tokens, password_reset_tokens) because all FK tables
have `ON DELETE CASCADE`.

Full script: `sql/cleanup/reset_test_database.sql`

---

## Token Flow Diagram

```
Signup → [check email inbox or console for token] → Verify Email
                                              ↓
                                           Login
                                          ↙     ↘
                               accessToken    refreshToken
                                  (15 min)      (14 days)
                                     ↓              ↓
                               use in headers    /refresh → new pair
                                                     ↓
                                                  /logout → revoke
```
