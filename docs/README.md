# AdminPlatform — Docs Index

## Requirements

- .NET SDK 9.0.305+ (pinned in [`global.json`](../global.json))
- Docker (for the database, and for `AdminPlatform.IntegrationTests`, which uses Testcontainers)

## Run the database

```bash
docker run --name adminplatform-db -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:16-alpine
```

(Or `docker compose up db` — see the [root README](../README.md) for the full Docker Compose path.)

## Configure

```bash
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=adminplatform;Username=postgres;Password=postgres"
export Jwt__SigningKey=$(openssl rand -base64 32)
```

## Migrate

```bash
dotnet run --project src/Tools/AdminPlatform.Migrator -- migrate
```

## Seed

```bash
export SEED_ADMIN_EMAIL=admin@example.com
export SEED_ADMIN_PASSWORD='Passw0rd!Passw0rd!'
dotnet run --project src/Tools/AdminPlatform.Migrator -- seed
```

(`-- all` runs migrate + seed together. Both are idempotent — safe to re-run.)

## Run the API

```bash
dotnet run --project src/Host/AdminPlatform.Api
```

Swagger: `http://localhost:5000/swagger` (Development only).

## Run tests

```bash
dotnet test tests/AdminPlatform.UnitTests            # no external dependencies
dotnet test tests/AdminPlatform.ArchitectureTests     # no external dependencies
dotnet test tests/AdminPlatform.IntegrationTests      # needs Docker (Testcontainers.PostgreSql)
```

## More

- [ARCHITECTURE.md](ARCHITECTURE.md) — folder-by-folder purpose, dependency rules, how modules talk to each other
- [DEVELOPMENT_GUIDE.md](DEVELOPMENT_GUIDE.md) — step-by-step: add a new module, add a feature to an existing one, add an entity/API/permission/menu
- [root README](../README.md) — Docker Compose, full configuration reference, known limitations
