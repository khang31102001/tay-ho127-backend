# AdminPlatform

A reusable ASP.NET Core admin-platform backend template: Modular Monolith +
Clean Architecture, PostgreSQL, JWT access/refresh authentication,
Role-Based + Permission-Based authorization, dynamic menus, audit logging,
and a deployment-safe migration story — built as a foundation for future
internal admin systems, not a single branded app.

> Extending this codebase? See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
> and [`docs/DEVELOPMENT_GUIDE.md`](docs/DEVELOPMENT_GUIDE.md) — the second
> one is a step-by-step guide to adding a new module, a new feature, or a new
> entity/permission/menu using this codebase's own conventions.

## Architecture

Five business modules, each a single project with internal
`Domain / Application / Infrastructure / Api` layering (enforced by
[architecture tests](tests/AdminPlatform.ArchitectureTests), not project
boundaries):

| Module | Owns |
|---|---|
| **Identity** | Users, login/refresh/logout, sessions, working-context selection |
| **AccessControl** | Roles, Permissions, role-permission and user-role assignment |
| **Organization** | Organizations, Departments (tree), Brands, user scope assignment |
| **Navigation** | Dynamic Menu tree, filtered per caller by permission |
| **Platform** | FiscalYears, SystemSettings, AuditLogs |

Modules never reference each other's project. Where one module genuinely
needs another (e.g. Identity needs AccessControl's resolved permissions to
put in a JWT), the dependent module defines a small port interface, and the
**Host** — the only project allowed to reference more than one module —
implements it with a thin adapter (`src/Host/AdminPlatform.Api/CrossModuleAdapters`).
Cross-module foreign keys (e.g. `access_control.user_roles.user_id` →
`identity.users.id`) are added by hand as raw SQL in the migration, since the
schemas share one physical database even though the modules don't share code.

```
AdminPlatform.sln
src/
  BuildingBlocks/AdminPlatform.SharedKernel   # Entity, AuditableEntity, domain exceptions — no framework deps
  BuildingBlocks/AdminPlatform.Common         # ICurrentUser, pagination, ProblemDetails, permission authz, audit interceptors
  Modules/{Identity,AccessControl,Organization,Navigation,Platform}/AdminPlatform.Modules.*
  Host/AdminPlatform.Api                      # composition root: DI wiring, JWT, Swagger, middleware
  Tools/AdminPlatform.Migrator                # migrate/seed console tool — the "separate deployment job"
tests/
  AdminPlatform.UnitTests
  AdminPlatform.ArchitectureTests
  AdminPlatform.IntegrationTests
```

## Tech stack

- **.NET 8 (LTS)**, built with the .NET 9.0.305 SDK (pinned in `global.json`)
- **ASP.NET Core** MVC controllers, **EF Core 8** + Npgsql, **PostgreSQL 16**
- JWT bearer auth, **FluentValidation**, **Serilog**, **Swashbuckle**
- **xUnit**, **NetArchTest**, **Testcontainers.PostgreSql**
- Central Package Management (`Directory.Packages.props`) — every NuGet
  version is pinned in one place

## Prerequisites

- .NET SDK 9.0.305+ (see `global.json`)
- Docker (for `docker compose`, and for the integration test suite, which
  spins up a real Postgres via Testcontainers)
- PostgreSQL 16 if you'd rather run it outside Docker

## Getting started — Docker Compose (recommended)

```bash
cp .env.example .env
# edit .env: set POSTGRES_PASSWORD, JWT_SIGNING_KEY (openssl rand -base64 32),
# SEED_ADMIN_EMAIL, SEED_ADMIN_PASSWORD

docker compose up --build
```

This starts `db`, runs `migrator` once (applies all 5 modules' migrations,
then seeds — see below), and starts `api` only after the migrator succeeds.
The API is at `http://localhost:8080`, Swagger at `/swagger` (only enabled
outside Production).

Re-run seeding any time without rebuilding:

```bash
docker compose run --rm migrator seed
```

## Getting started — local dev, no Docker for the API

1. Start a local Postgres 16 (Docker is the easy way even here: `docker run --name adminplatform-db -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:16-alpine`).
2. Set the required configuration — either environment variables or
   `dotnet user-secrets` inside `src/Host/AdminPlatform.Api`:

   ```bash
   export ConnectionStrings__Default="Host=localhost;Port=5432;Database=adminplatform;Username=postgres;Password=postgres"
   export Jwt__SigningKey=$(openssl rand -base64 32)
   ```
3. Apply migrations and seed:

   ```bash
   export SEED_ADMIN_EMAIL=admin@example.com
   export SEED_ADMIN_PASSWORD='Passw0rd!Passw0rd!'
   dotnet run --project src/Tools/AdminPlatform.Migrator -- all
   ```
4. Run the API:

   ```bash
   dotnet run --project src/Host/AdminPlatform.Api
   ```

   In Development, the host can also auto-migrate on startup as a
   convenience — set `Database__AutoMigrate=true`. This path is
   Development-only by design; Production always uses the Migrator as a
   separate step (never migrates silently at API startup, per the "don't
   migrate implicitly with multiple instances starting up" constraint).

## Configuration reference

| Variable | Required | Notes |
|---|---|---|
| `ConnectionStrings__Default` | yes | Postgres connection string, shared by all 5 module DbContexts (each owns its own schema) |
| `Jwt__SigningKey` | yes | Base64, ≥32 bytes. `openssl rand -base64 32`. The API refuses to start without it |
| `Jwt__Issuer` / `Jwt__Audience` | no | Default `AdminPlatform` / `AdminPlatform.Clients` |
| `Jwt__AccessTokenMinutes` / `Jwt__RefreshTokenDays` | no | Default `15` / `7` |
| `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD` | yes, for seeding | Read by the Migrator only, never hardcoded |
| `Database__AutoMigrate` | no | Development-only convenience flag; ignored outside Development |

## Database & migrations

Each module owns one PostgreSQL schema and one independent EF Core migration
history (`identity`, `access_control`, `organization`, `navigation`,
`platform`). To add a migration after changing a module's model:

```bash
dotnet ef migrations add <Name> \
  --project src/Modules/<Module>/AdminPlatform.Modules.<Module>/AdminPlatform.Modules.<Module>.csproj \
  --context AdminPlatform.Modules.<Module>.Infrastructure.<Module>DbContext \
  --output-dir Infrastructure/Migrations
```

(Each module has its own `IDesignTimeDbContextFactory`, so `dotnet ef` needs
no `--startup-project` and never executes the Migrator's `Program.cs`.)

## Seeding

`dotnet run --project src/Tools/AdminPlatform.Migrator -- seed` (or `all`) is
idempotent — safe to run on every deploy:

- SuperAdmin user, from `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD` (upserted by email)
- `super-admin` role granted the full cross-module permission catalog (upserted by code)
- One sample Organization / Department / Brand / FiscalYear (upserted by code)
- The base Dashboard + Administration menu tree (upserted by code)

## Tests

```bash
dotnet test tests/AdminPlatform.UnitTests            # 43 tests, no external dependencies
dotnet test tests/AdminPlatform.ArchitectureTests     # 20 tests, no external dependencies
dotnet test tests/AdminPlatform.IntegrationTests      # needs Docker (Testcontainers.PostgreSql)
```

## Known limitations

- **No frontend.** This repository is scoped to the backend (`CLAUDE.md`:
  "Backend/API service"); there is no React/Next.js admin UI here.
- **Permissions are embedded in the JWT at login/refresh time.** A role or
  permission change takes effect on the user's next login/refresh, not
  instantly. Swap `IUserPermissionsProvider`'s Host adapter for a per-request
  DB/cache lookup if you need immediate revocation.
- **No per-user FiscalYear scoping table.** The task's data model defines
  `UserDepartments`/`UserBrands` but no `UserFiscalYears`; any active fiscal
  year is selectable by any user. Add a scoping table + tighten
  `IFiscalYearAccessQueryService` if your business needs it.
- **`system_settings`'s unique (OrganizationId, Code) index only fully
  enforces uniqueness within one organization** — Postgres treats multiple
  `NULL` OrganizationId rows as distinct, so the DB-level index alone doesn't
  block two differently-worded duplicate *global* settings; the application
  service's own check does. A partial unique index would close this gap if
  needed.
