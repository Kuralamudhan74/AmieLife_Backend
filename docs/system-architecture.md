# AmieLife Backend — System Architecture

> **Last Updated:** 2026-02-28
> **Version:** 1.0.0 (Authentication Module)
> **Author:** Initial design by AI Architect (Claude)
>
> **Rule:** Every time new code is added, this file MUST be updated.
> Any AI agent or developer reading this file should be able to understand the entire system from scratch.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Technology Stack](#2-technology-stack)
3. [Architecture Overview](#3-architecture-overview)
4. [Folder Structure](#4-folder-structure)
5. [Authentication Flow](#5-authentication-flow)
6. [Database Design](#6-database-design)
7. [Token Lifecycle](#7-token-lifecycle)
8. [Security Strategy](#8-security-strategy)
9. [Environment Configuration](#9-environment-configuration)
10. [How to Extend the System](#10-how-to-extend-the-system)
11. [Future Scalability Plan](#11-future-scalability-plan)

---

## 1. Project Overview

AmieLife is an e-commerce platform. This repository contains the **backend API** built with .NET 8.

**Current Phase:** Authentication Module only.
- Signup, Login, Refresh Token, Logout
- Email Verification (token generation + stub email)
- Password Reset (token generation + stub email)
- Guest User Creation (checkout without registration)

**Not yet implemented** (see [Future Phase](#11-future-scalability-plan)):
- Products, Cart, Orders, Payments
- OAuth (Google), 2FA, Admin management

---

## 2. Technology Stack

| Concern | Technology |
|---|---|
| Runtime | .NET 8 |
| Framework | ASP.NET Core Web API |
| ORM | EF Core 8 (Code First) |
| Database (Dev) | PostgreSQL via Supabase |
| Database (Prod) | PostgreSQL (Azure / Supabase) |
| Auth | JWT Bearer + Refresh Token Rotation |
| Password Hashing | BCrypt.Net (work factor 12) |
| Validation | FluentValidation 11 |
| Logging | Serilog (Console + File) |
| Rate Limiting | ASP.NET Core built-in RateLimiter |
| Testing | xUnit + Moq + FluentAssertions |
| API Docs | Swagger / OpenAPI |

---

## 3. Architecture Overview

The project follows **Clean Architecture** with strict layer isolation:

```
┌─────────────────────────────────────────────────────────┐
│                     AmieLife.Api                         │  ← HTTP entry point. Controllers, middleware only.
│  Depends on: Application, Infrastructure (via DI)        │
└───────────────────────────┬─────────────────────────────┘
                            │ calls
┌───────────────────────────▼─────────────────────────────┐
│                  AmieLife.Application                    │  ← Business rules, DTOs, interfaces, validators
│  Depends on: Domain, Shared                              │
└───────────────────────────┬─────────────────────────────┘
                            │ depends on interfaces from Application
┌───────────────────────────▼─────────────────────────────┐
│                 AmieLife.Infrastructure                  │  ← EF Core, JWT, BCrypt, Repositories
│  Implements interfaces defined in Application            │
└───────────────────────────┬─────────────────────────────┘
                            │ all layers depend on
┌───────────────────────────▼─────────────────────────────┐
│                   AmieLife.Domain                        │  ← Pure C# entities, enums, domain exceptions
│  NO dependencies on any other project layer              │
└──────────────────────────────────────────────────────────┘
                    +
┌──────────────────────────────────────────────────────────┐
│                   AmieLife.Shared                        │  ← Cross-cutting: constants, HashHelper
└──────────────────────────────────────────────────────────┘
```

**Key rule:** Dependencies point INWARD. Domain knows nothing about EF Core. Application knows nothing about BCrypt. Infrastructure depends on Application interfaces, never the reverse.

---

## 4. Folder Structure

```
AmieLife_Backend/
├── AmieLife.sln
├── .gitignore
│
├── src/
│   ├── AmieLife.Api/                     ← Web API entry point
│   │   ├── Controllers/
│   │   │   └── AuthController.cs
│   │   ├── Middleware/
│   │   │   └── ExceptionHandlingMiddleware.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json              ← Non-sensitive defaults ONLY
│   │   ├── appsettings.Development.json  ← Local dev secrets (GITIGNORED)
│   │   └── appsettings.Production.json   ← Prod log levels only
│   │
│   ├── AmieLife.Application/             ← Business logic
│   │   ├── Common/
│   │   │   ├── Interfaces/               ← IAuthService, ITokenService, IUserRepository, etc.
│   │   │   └── Models/
│   │   │       └── Result.cs             ← Discriminated union result pattern
│   │   ├── DTOs/Auth/                    ← Request/Response DTOs
│   │   ├── Services/
│   │   │   └── AuthService.cs            ← All auth business logic
│   │   └── Validators/Auth/              ← FluentValidation validators
│   │
│   ├── AmieLife.Domain/                  ← Pure domain (no framework deps)
│   │   ├── Entities/                     ← User, RefreshToken, Address, etc.
│   │   ├── Enums/                        ← UserRole, UserStatus
│   │   └── Exceptions/                   ← DomainException, NotFoundException, etc.
│   │
│   ├── AmieLife.Infrastructure/          ← EF Core, JWT, BCrypt
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Configurations/           ← IEntityTypeConfiguration per table
│   │   ├── Repositories/                 ← EF Core implementations
│   │   └── Services/
│   │       ├── TokenService.cs           ← JWT generation
│   │       └── EmailService.cs           ← Email stub (replace with SendGrid)
│   │
│   └── AmieLife.Shared/                  ← No project dependencies
│       ├── Constants/AppConstants.cs
│       └── Helpers/HashHelper.cs
│
├── sql/                                  ← Raw SQL for reference / manual migration
│   ├── tables/                           ← Table DDL scripts (numbered order)
│   ├── indexes/                          ← Additional index scripts
│   ├── stored-procedures/                ← (empty in phase 1)
│   └── seeds/                            ← Dev seed data
│
├── docs/                                 ← All project documentation lives here
│   ├── system-architecture.md            ← THIS FILE
│   ├── deployment-guide.md
│   ├── secrets-configuration.md
│   ├── api-reference.md
│   └── database-schema.md
│
└── tests/
    ├── AmieLife.UnitTests/               ← xUnit + Moq, tests for Application layer
    └── AmieLife.IntegrationTests/        ← ASP.NET Core TestServer, end-to-end API tests
```

---

## 5. Authentication Flow

### 5.1 Signup

```
Client → POST /api/v1/auth/signup
  → FluentValidation (password complexity, email format)
  → AuthService.SignupAsync()
    → Check email uniqueness
    → BCrypt.HashPassword(password, workFactor=12)
    → Save User (IsEmailVerified=false)
    → Generate random token → SHA-256 hash → Save EmailVerificationToken
    → EmailService.SendEmailVerificationAsync() [STUB - logs URL]
  ← 200 OK: "Check your email to verify"
  NOTE: No tokens returned until email is verified
```

### 5.2 Login

```
Client → POST /api/v1/auth/login
  → Rate limiter (10 req / 60s per IP)
  → FluentValidation
  → AuthService.LoginAsync()
    → Look up user by email (lowercase)
    → Check: user exists? not guest? status=Active?
    → Check: IsLockedOut()? (LockoutEndTime > NOW)
    → BCrypt.Verify(password, hash)
      → On failure: RecordFailedLogin() → lock after 5 attempts
    → Check: IsEmailVerified?
    → RecordSuccessfulLogin() → reset counter
    → TokenService.GenerateAccessToken() → JWT (15 min)
    → TokenService.GenerateAndStoreRefreshTokenAsync() → random token, hash stored
  ← 200 OK: { accessToken, refreshToken, expiresInSeconds }
```

### 5.3 Refresh Token

```
Client → POST /api/v1/auth/refresh  { refreshToken: "raw_token" }
  → AuthService.RefreshTokenAsync()
    → SHA-256 hash the raw token
    → Look up by hash in refresh_tokens table
    → Validate: not revoked, not expired
    → Revoke old token (rotation)
    → Generate new access token + new refresh token
  ← 200 OK: new { accessToken, refreshToken }
```

### 5.4 Logout

```
Client → POST /api/v1/auth/logout  { refreshToken: "raw_token" }
  → AuthService.LogoutAsync()
    → Hash token → find in DB
    → Revoke token
  ← 200 OK (always — no token probing possible)
```

### 5.5 Password Reset

```
Client → POST /api/v1/auth/forgot-password  { email }
  → Always returns 200 (anti-enumeration)
  → If user found: generate reset token → store hash → stub email

Client → POST /api/v1/auth/reset-password  { token, newPassword, confirmNewPassword }
  → Hash token → find in DB
  → Validate: not used, not expired
  → Mark token used
  → BCrypt.HashPassword(newPassword)
  → RevokeAllForUser (all refresh tokens invalidated)
  ← 200 OK
```

### 5.6 Guest User

```
Client → POST /api/v1/auth/guest  { email }
  → If email belongs to registered user → 400 error
  → If guest already exists with email → return fresh access token
  → Otherwise create User (IsGuest=true, PasswordHash=null)
  ← 200 OK: { userId, email, guestAccessToken }
```

---

## 6. Database Design

See also: [database-schema.md](./database-schema.md)

| Table | Purpose |
|---|---|
| `users` | All users (registered + guests) |
| `refresh_tokens` | Active session tokens (multi-device) |
| `addresses` | Shipping/billing addresses |
| `email_verification_tokens` | Email ownership verification |
| `password_reset_tokens` | Password change authorization |

**Key design decisions:**
- `UUID` primary keys (not sequential ints — prevents ID enumeration)
- `TIMESTAMPTZ` (timezone-aware) for all timestamps
- Snake_case column names (PostgreSQL convention)
- Soft delete via `status='Deleted'` instead of `DELETE`
- Unique index on `LOWER(email)` — case-insensitive uniqueness
- All token tables store **hashes only** — raw values travel only in emails/responses

---

## 7. Token Lifecycle

### Access Token (JWT)
- **Expiry:** 15 minutes
- **Claims:** `sub` (userId), `email`, `role`, `is_guest`, `jti`, `iat`
- **Storage (client):** In-memory only. Never localStorage.
- **Validation:** Signature + issuer + audience + expiry. `ClockSkew = Zero`.

### Refresh Token (Opaque)
- **Generation:** 64 bytes of `RandomNumberGenerator` → Base64URL encoded
- **Storage (DB):** SHA-256 hex hash only
- **Storage (client):** HttpOnly cookie or secure storage
- **Expiry:** 14 days
- **Rotation:** On every use, old token revoked, new token issued
- **Revocation:** On logout (single device) or password change (all devices)

---

## 8. Security Strategy

| Threat | Mitigation |
|---|---|
| Brute force login | Rate limiting (10 req/60s) + account lockout after 5 failures |
| Account enumeration | Generic error messages on login/forgot-password |
| Stolen refresh token | Token rotation — stolen token detected on reuse |
| JWT tampering | HMAC-SHA256 signature with 32+ char secret |
| Password breach | BCrypt work factor 12 (adaptive hashing) |
| Token replay | SHA-256 hash stored — raw token useless if DB is breached |
| XSS → token theft | Access tokens kept in memory; refresh tokens in HttpOnly cookie |
| CORS attack | Whitelist-only CORS (AllowedOrigins in config) |
| Stack trace exposure | ExceptionHandlingMiddleware maps all exceptions to ProblemDetails |
| Injection | EF Core parameterized queries only |
| Insecure transport | HTTPS enforced in production (`UseHttpsRedirection`) |

---

## 9. Environment Configuration

All secrets are injected via **environment variables** at runtime. The `appsettings.json` contains only safe defaults and structure.

### Required Environment Variables (Production)

| Variable | Example | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=...;Database=...` | Full PostgreSQL connection string |
| `Jwt__Secret` | 32+ random characters | JWT signing key — **never reuse across environments** |
| `Jwt__Issuer` | `AmieLife` | JWT issuer claim |
| `Jwt__Audience` | `AmieLife-Users` | JWT audience claim |
| `Cors__AllowedOrigins__0` | `https://amielife.com` | Frontend URL(s) |
| `App__BaseUrl` | `https://amielife.com` | Used to build email links |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Controls log levels and Swagger visibility |

See [secrets-configuration.md](./secrets-configuration.md) for platform-specific setup.

---

## 10. How to Extend the System

### Adding a New Feature Module (e.g., Products)

1. **Domain:** Add entities to `AmieLife.Domain/Entities/`
2. **Application:** Add repository interface to `Common/Interfaces/`, DTOs to `DTOs/`, service interface + implementation to `Services/`, validators to `Validators/`
3. **Infrastructure:** Add EF configuration to `Data/Configurations/`, add repository implementation to `Repositories/`, register in `InfrastructureServiceExtensions.cs`
4. **API:** Add controller to `Controllers/`, add route under `api/v1/{resource}`
5. **Database:** Add SQL script to `sql/tables/`, update `docs/database-schema.md`
6. **Tests:** Add unit tests in `AmieLife.UnitTests/`, integration tests in `AmieLife.IntegrationTests/`
7. **Docs:** Update this file and `docs/api-reference.md`

### Adding OAuth (Google) — Future

1. Add `Microsoft.AspNetCore.Authentication.Google` package to `AmieLife.Api`
2. Add `GoogleClientId` and `GoogleClientSecret` to secrets config
3. Create `OAuthService` in Application layer
4. Add `POST /api/v1/auth/oauth/google` controller endpoint
5. Update `users` table: `oauth_provider VARCHAR(20) NULL`, `oauth_subject TEXT NULL`

### Adding 2FA — Future

1. Add `TwoFactorSecret TEXT NULL` and `IsTwoFactorEnabled BOOLEAN DEFAULT FALSE` to `users`
2. Implement TOTP (Time-based OTP) via `OtpNet` NuGet package
3. Add setup/verify endpoints
4. Add `RequiresTwoFactor` claim to JWT flow

---

## 11. Future Scalability Plan

### Horizontal Scaling
- Application is **stateless** — no in-memory session state
- Refresh tokens in PostgreSQL → works across multiple API instances
- Add Redis cache for token revocation lists at scale (millions of users)

### Planned Modules (in order)

| Phase | Module | Notes |
|---|---|---|
| 2 | Admin Panel | User management, lockout override |
| 3 | Product Module | Catalog, images, inventory |
| 4 | Cart Module | Session-linked, guest cart merge |
| 5 | Order Module | Order lifecycle, status tracking |
| 6 | Payment | Stripe / Razorpay integration |
| 7 | OAuth | Google sign-in |
| 8 | 2FA | TOTP-based two-factor authentication |
| 9 | Notifications | Email (transactional) + push |

### Database Scaling Path
1. **Current:** Single Supabase/PostgreSQL instance
2. **10k users:** Add connection pooling (PgBouncer — built into Supabase)
3. **100k users:** Read replica for reports/analytics
4. **1M+ users:** Partition `refresh_tokens` and `orders` by date range
