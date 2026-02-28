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
