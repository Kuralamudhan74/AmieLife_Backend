# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (layer cache optimization)
COPY AmieLife.sln .
COPY src/AmieLife.Api/AmieLife.Api.csproj                           src/AmieLife.Api/
COPY src/AmieLife.Application/AmieLife.Application.csproj           src/AmieLife.Application/
COPY src/AmieLife.Domain/AmieLife.Domain.csproj                     src/AmieLife.Domain/
COPY src/AmieLife.Infrastructure/AmieLife.Infrastructure.csproj     src/AmieLife.Infrastructure/
COPY src/AmieLife.Shared/AmieLife.Shared.csproj                     src/AmieLife.Shared/

RUN dotnet restore AmieLife.sln

# Copy all source files and build
COPY . .
RUN dotnet publish src/AmieLife.Api -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

# Render/Railway/Azure all inject PORT — respect it
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AmieLife.Api.dll"]
