# Deployment Guide

> Step-by-step instructions to deploy AmieLife Backend to Render, Railway, or Azure.

---

## Prerequisites

- .NET 8 SDK installed locally
- Git repository pushed to GitHub/GitLab
- Supabase project created (or PostgreSQL database provisioned)
- Secrets ready (see [secrets-configuration.md](./secrets-configuration.md))

---

## Step 1 — Build and Test Locally

```bash
# Restore packages
dotnet restore AmieLife.sln

# Build
dotnet build AmieLife.sln --configuration Release

# Run tests
dotnet test AmieLife.sln

# Run locally (Development environment)
cd src/AmieLife.Api
dotnet run
# → Swagger UI available at: http://localhost:5000
```

---

## Step 2 — Database Migration

EF Core migrations are applied automatically on startup (`db.Database.Migrate()` in `Program.cs`).

To create a new migration after schema changes:
```bash
cd src/AmieLife.Api
dotnet ef migrations add <MigrationName> --project ../AmieLife.Infrastructure --startup-project .
```

To verify pending migrations before deploying:
```bash
dotnet ef migrations list --project ../AmieLife.Infrastructure --startup-project .
```

---

## Deploying to Render.com

1. **Create a Web Service** in Render dashboard
2. Connect your GitHub repository
3. Set:
   - **Environment:** Docker OR .NET
   - **Build Command:** `dotnet publish src/AmieLife.Api -c Release -o publish`
   - **Start Command:** `dotnet publish/AmieLife.Api.dll`
4. Add all environment variables from [secrets-configuration.md](./secrets-configuration.md)
5. Deploy

**Render Dockerfile (optional — for more control):**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/AmieLife.Api -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AmieLife.Api.dll"]
```

---

## Deploying to Railway

1. Create a new project → Deploy from GitHub
2. Select your repository
3. Railway auto-detects .NET projects
4. Set environment variables in the Variables tab
5. Railway sets `PORT` automatically → the app respects `ASPNETCORE_URLS=http://+:$PORT`

---

## Deploying to Azure App Service

```bash
# Build publish artifact
dotnet publish src/AmieLife.Api -c Release -o ./publish

# Deploy using Azure CLI
az webapp deploy \
  --resource-group amielife-rg \
  --name amielife-api \
  --src-path ./publish \
  --type zip
```

Or use **GitHub Actions** for CI/CD:
1. Create `.github/workflows/deploy.yml`
2. Use `azure/webapps-deploy@v2` action
3. Add `AZURE_WEBAPP_PUBLISH_PROFILE` to GitHub Secrets

---

## Health Check

After deployment, verify the application is running:
```
GET https://your-deployed-url/health
```
Expected: `{"status":"Healthy"}`

---

## Swagger (Development Only)

Swagger UI is only enabled when `ASPNETCORE_ENVIRONMENT=Development`.
In production, it is disabled for security.
