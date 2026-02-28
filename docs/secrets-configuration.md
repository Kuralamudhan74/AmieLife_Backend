# Secrets Configuration Guide

> Every deployment environment needs these secrets configured.
> NEVER commit real secrets to Git. NEVER put them in `appsettings.json`.

---

## How Configuration Works

.NET resolves configuration in this priority order (highest wins):

```
Environment Variables  >  appsettings.{Environment}.json  >  appsettings.json
```

In production, **only use environment variables**. The `appsettings.json` in this repo contains safe placeholder strings that will cause a startup failure if not overridden — this is intentional.

---

## Required Secrets

| Key | Type | Description | Example |
|---|---|---|---|
| `ConnectionStrings__DefaultConnection` | string | Full PostgreSQL connection string | See below |
| `Jwt__Secret` | string | JWT HMAC-SHA256 signing key (min 32 chars) | Random 64-char string |
| `Jwt__Issuer` | string | JWT `iss` claim | `AmieLife` |
| `Jwt__Audience` | string | JWT `aud` claim | `AmieLife-Users` |
| `Cors__AllowedOrigins__0` | string | First allowed frontend origin | `https://amielife.com` |
| `App__BaseUrl` | string | Frontend base URL for email links | `https://amielife.com` |
| `ASPNETCORE_ENVIRONMENT` | string | Runtime environment | `Production` |

---

## Connection String Formats

### Supabase (Development)
```
Host=db.XXXXXXXXXXXXXXXXXXXXXX.supabase.co;Database=postgres;Username=postgres;Password=YOUR_DB_PASSWORD;Port=5432;SSL Mode=Require;Trust Server Certificate=true
```
Find this in: Supabase Dashboard → Project Settings → Database → Connection String → .NET

### Supabase (Production — Direct or Pooler)
Use the **Transaction Pooler** (port 6543) for serverless/container deployments:
```
Host=db.XXXXXXXXXXXXXXXXXXXXXX.supabase.co;Database=postgres;Username=postgres.XXXXXX;Password=YOUR_PASSWORD;Port=6543;SSL Mode=Require;Pooling=true
```

### Azure PostgreSQL Flexible Server
```
Host=your-server.postgres.database.azure.com;Database=amielife;Username=adminuser@your-server;Password=YOUR_PASSWORD;Port=5432;SSL Mode=Require
```

---

## Generating a Strong JWT Secret

Use any of these methods — output must be at least 32 characters:

**PowerShell:**
```powershell
[System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
```

**Linux/Mac:**
```bash
openssl rand -base64 64
```

**Online (for dev only, never prod):** https://generate-secret.vercel.app/64

---

## Platform-Specific Setup

### Render.com

1. Go to your Web Service → **Environment** tab
2. Add each variable as a Key/Value pair:
   ```
   ASPNETCORE_ENVIRONMENT = Production
   ConnectionStrings__DefaultConnection = Host=...
   Jwt__Secret = your-64-char-secret
   Jwt__Issuer = AmieLife
   Jwt__Audience = AmieLife-Users
   Cors__AllowedOrigins__0 = https://your-frontend.com
   App__BaseUrl = https://your-frontend.com
   ```
3. Render sets `PORT` automatically — the app reads it via `ASPNETCORE_URLS`.

### Railway.app

1. Open your project → Service → **Variables** tab
2. Add the same key/value pairs as above.
3. Railway also provides `${{RAILWAY_PUBLIC_DOMAIN}}` — you can reference this.

### Azure App Service

**Via Azure Portal:**
1. App Service → Configuration → Application Settings
2. Add each variable (the double-underscore `__` is recognized as a section separator)

**Via Azure CLI:**
```bash
az webapp config appsettings set \
  --resource-group amielife-rg \
  --name amielife-api \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    "ConnectionStrings__DefaultConnection=Host=..." \
    Jwt__Secret=your-secret \
    Jwt__Issuer=AmieLife \
    Jwt__Audience=AmieLife-Users \
    "Cors__AllowedOrigins__0=https://amielife.com" \
    "App__BaseUrl=https://amielife.com"
```

---

## Local Development

1. Copy `appsettings.Development.json` and fill in your Supabase credentials.
2. This file is listed in `.gitignore` — it will never be committed.
3. Alternatively use .NET User Secrets:
   ```bash
   cd src/AmieLife.Api
   dotnet user-secrets set "Jwt:Secret" "your-dev-secret-here"
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=..."
   ```

---

## Rotation Policy

| Secret | When to Rotate |
|---|---|
| `Jwt__Secret` | On suspected compromise, or every 6 months |
| DB Password | On team member departure or compromise |
| Refresh tokens | Automatic (rotation on every use) |

When rotating `Jwt__Secret`:
1. Update the environment variable
2. Restart the API
3. All existing access tokens are immediately invalid
4. Users will need to log in again (refresh tokens also become unusable)
