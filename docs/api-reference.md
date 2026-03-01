# API Reference — Authentication Module

> Base URL: `https://your-domain.com/api/v1/auth`
> Content-Type: `application/json`
> Auth: `Authorization: Bearer {accessToken}` (for protected routes)

---

## Endpoints

### POST `/signup`

Register a new user account. Email verification required before login.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "Str0ng@Pass",
  "confirmPassword": "Str0ng@Pass",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890"
}
```

**Responses:**
- `200 OK` — Registration successful, check email
- `400 Bad Request` — Validation error or email already exists

---

### POST `/login`

Login with email and password.

**Request:**
```json
{ "email": "user@example.com", "password": "Str0ng@Pass" }
```

**Response (200):**
```json
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "raw_refresh_token_value",
  "expiresInSeconds": 900,
  "tokenType": "Bearer"
}
```

**Errors:**
- `400` — Invalid credentials / locked / unverified email
- `429` — Rate limit exceeded

---

### POST `/refresh`

Exchange a refresh token for a new token pair.

**Request:**
```json
{ "refreshToken": "raw_refresh_token_value" }
```

**Response (200):** Same as login response.
**Errors:** `401` — Invalid, expired, or revoked token.

---

### POST `/logout`

Revoke the current device's refresh token.

**Request:**
```json
{ "refreshToken": "raw_refresh_token_value" }
```

**Response:** `200 OK` always (no token probing).

---

### POST `/verify-email`

Verify email using token from verification email.

**Request:**
```json
{ "token": "raw_verification_token" }
```

**Response:** `200 OK` or `400` if invalid/expired.

---

### POST `/forgot-password`

Request a password reset email.

**Request:**
```json
{ "email": "user@example.com" }
```

**Response:** `200 OK` always (anti-enumeration).

---

### POST `/reset-password`

Set a new password using a reset token.

**Request:**
```json
{
  "token": "raw_reset_token",
  "newPassword": "NewStr0ng@Pass",
  "confirmNewPassword": "NewStr0ng@Pass"
}
```

**Response:** `200 OK` or `400` if invalid.

---

### POST `/guest`

Create a guest user for checkout.

**Request:**
```json
{ "email": "guest@example.com" }
```

**Response (200):**
```json
{
  "userId": "uuid",
  "email": "guest@example.com",
  "guestAccessToken": "eyJhbGci..."
}
```

---

## Error Response Format (RFC 7807)

```json
{
  "status": 400,
  "title": "Login Failed",
  "detail": "Invalid email or password.",
  "instance": "/api/v1/auth/login"
}
```

## Validation Error Format

```json
{
  "errors": {
    "Password": ["Password must contain at least one uppercase letter."]
  }
}
```

---

## Password Rules

- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character (`!@#$%^&*` etc.)

---

## Frontend Integration Guide

> This section is for the **frontend team** to understand how to integrate with the auth API.

### Token Storage

| Token | Where to Store | Why |
|---|---|---|
| **Access Token** | In-memory variable (React state / JS variable) | Short-lived (15 min). Never in localStorage (XSS risk). |
| **Refresh Token** | HttpOnly secure cookie or secure mobile storage | Long-lived (14 days). HttpOnly prevents JS access. |

### Authentication Header

For all protected API calls, include the access token:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### Refresh Token Flow (How to Use)

The refresh token keeps the user logged in without re-entering credentials.

```
1. User logs in → receives accessToken (15 min) + refreshToken (14 days)
2. Frontend stores both tokens securely (see table above)
3. Frontend uses accessToken in Authorization header for all API calls
4. When API returns 401 (access token expired):
   a. Frontend calls POST /api/v1/auth/refresh  { "refreshToken": "stored_token" }
   b. API returns NEW accessToken + NEW refreshToken
   c. Frontend replaces BOTH stored tokens with the new ones
   d. Frontend retries the original failed request with the new access token
5. If /refresh also returns 401 → refresh token is expired/revoked → redirect to login
6. On logout → call POST /api/v1/auth/logout  { "refreshToken": "stored_token" }
```

### Recommended Frontend Implementation

```
// Pseudocode for an API interceptor (Axios / fetch wrapper)

async function apiCall(url, options) {
    let response = await fetch(url, {
        ...options,
        headers: { "Authorization": `Bearer ${accessToken}` }
    });

    if (response.status === 401) {
        // Access token expired — try refreshing
        const refreshResponse = await fetch("/api/v1/auth/refresh", {
            method: "POST",
            body: JSON.stringify({ refreshToken: storedRefreshToken })
        });

        if (refreshResponse.ok) {
            const { accessToken: newAccess, refreshToken: newRefresh } = await refreshResponse.json();
            // Update stored tokens (IMPORTANT: replace BOTH)
            accessToken = newAccess;
            storedRefreshToken = newRefresh;
            // Retry original request with new token
            response = await fetch(url, {
                ...options,
                headers: { "Authorization": `Bearer ${newAccess}` }
            });
        } else {
            // Refresh token also invalid — force re-login
            redirectToLogin();
        }
    }

    return response;
}
```

### Key Rules for Frontend

1. **Always replace both tokens** after a refresh — the old refresh token is revoked immediately
2. **Never reuse a refresh token** — each token can only be used once (rotation security)
3. **Logout clears tokens** — call `/logout` AND delete locally stored tokens
4. **401 from `/refresh`** = session fully expired → redirect to login page
5. **Access token is NOT sent to `/refresh`** — only the refresh token is needed
6. **Guest users** get only an access token (15 min, no refresh) — intended for checkout flow only
