---
name: build-backend-feature
description: Build a new business capability end-to-end in this modular monolith — from business requirement through API endpoint, request DTO, validation, application service, domain/business rules, entity, persistence (I<Module>DbContext + EF Core migration), mapping, response DTO, to HTTP response. Use for a new capability, not for adjusting an existing endpoint's contract (that's build-api-endpoint).
---

# Build Backend Feature

## 1. Purpose

Use this skill when the task requires implementing a **new business capability** — something that doesn't yet exist across most or all of these layers:

```text
Business Requirement / Use Case
        ↓
API Endpoint (route, verb, permission)
        ↓
Request DTO
        ↓
Validation (FluentValidation)
        ↓
Application Service (orchestration)
        ↓
Business Rules / Domain Logic
        ↓
Entity / Aggregate (Domain)
        ↓
I<Module>DbContext (persistence port) → EF Core migration → Database
        ↓
Mapping (Entity → Response DTO)
        ↓
Response DTO
        ↓
HTTP Response
```

Examples: "add a Departments capability to Organization", "let a manager approve a leave request", "add bulk user deactivation". Anything where you'd otherwise have to touch Domain, Application, Infrastructure, *and* Api in the same change.

## 1.1 Boundary with `build-api-endpoint`

| | `build-backend-feature` | `build-api-endpoint` |
|---|---|---|
| New/changed entity or business rules | Yes | No |
| New migration | Often | No |
| New Application service (not just a method) | Yes | No |
| Adding one route to expose existing logic | Incidental (last step) | Yes — this is the whole job |
| Adjusting an existing endpoint's contract only | No | Yes |

If, after reading the existing module, the Domain/Application/Infrastructure work already exists and only the HTTP door is missing, stop and use `build-api-endpoint` instead — do not re-walk this whole skill for a one-line addition.

---

# 2. Required Rules

Apply all of these; this skill is the one place all four converge in a single change:

```text
.claude/rules/architecture.md
.claude/rules/api-design.md
.claude/rules/database.md
.claude/rules/naming.md
.claude/rules/code-quality.md
.claude/rules/security.md
```

For the persistence step specifically, defer to `design-database` for anything beyond a straightforward new table. For the security posture of the new endpoint, defer to `review-security`'s checklist before declaring the feature done. For meaningful test coverage, defer to `test-backend`.

---

# 3. This Project's Actual Architecture — Ground Every Step In This

This is a **modular monolith**: one ASP.NET Core process, independent business modules (`Identity`, `AccessControl`, `Organization`, `Navigation`, `Platform`), one shared Postgres database with **one schema per module**. Full detail: [`docs/ARCHITECTURE.md`](../../docs/ARCHITECTURE.md).

Each module is **one C# project with four folders**, not four projects:

```text
Modules/<Module>/AdminPlatform.Modules.<Module>/
  Domain/            Entities + domain exceptions. No EF Core, no ASP.NET Core.
  Application/        DTOs (request/response records), FluentValidation validators,
                       service interfaces + implementations, an I<Module>DbContext port.
  Infrastructure/      The EF Core DbContext, IEntityTypeConfiguration<T> classes,
                       migrations, IDesignTimeDbContextFactory, external-service adapters.
  Api/                 Controllers + a <Module>Permissions static class.
  <Module>Module.cs    Add<Module>Module(services, configuration) — the DI entry point.
```

Dependency direction (enforced by `LayeringTests.cs`, not just documented):

```text
Domain          → SharedKernel only
Application     → Domain, SharedKernel, Common (ICurrentUser, pagination, etc.)
Infrastructure  → Domain, Application, SharedKernel, Common
Api             → Application, Common
```

**No module has a `ProjectReference` to another module** — enforced by `ModuleBoundaryTests.cs`. Cross-module data needs go through a Host-registered port/adapter (§8).

**Deliberately absent from this codebase — do not introduce them for a new feature:**

* No CQRS/MediatR. One Application service interface per feature area (`IUserService`), implemented directly, called directly by the controller.
* No generic repository / Unit of Work. Application depends on `I<Module>DbContext` (a hand-written interface exposing exactly the `DbSet<T>` the module needs) + `SaveChangesAsync`.
* No AutoMapper. Mapping from entity to response DTO is an explicit private static method or inline `new(...)` call (see `UserService.ToDetails`).
* No response wrapper (`{ success, data }`). Endpoints return the resource or `ProblemDetails` directly.

If a requirement seems to need one of these, treat that as a signal to re-check the requirement's actual scope before adding new infrastructure — see `bootstrap-api` §41–45 for when (rarely) they'd be justified.

---

# 4. The Reference Slice

Every step below is illustrated against the real `Identity/Users` slice already in this repo. Read these before writing anything new — match their shape exactly, don't reinvent:

```text
Api/UsersController.cs                          → controller shape, [RequirePermission], status codes
Api/IdentityPermissions.cs                       → permission code catalog
Application/Users/UserContracts.cs               → Request/Response record shapes
Application/Users/UserValidators.cs              → FluentValidation per request
Application/Users/IUserService.cs, UserService.cs → service interface + implementation, mapping
Application/IIdentityDbContext.cs                → persistence port
Domain/User.cs                                   → entity: private setters, factory + behavior methods
Infrastructure/IdentityDbContext.cs              → concrete DbContext implementing the port
Infrastructure/Configurations/UserConfiguration.cs → IEntityTypeConfiguration<T>
IdentityModule.cs                                → DI registration (service, validators, DbContext)
```

---

# 5. Workflow — Walk the Flow Top to Bottom

## Step 0 — Understand the Business Requirement

Before any code, write down:

```text
Use Case:
Owning Module:
Who Can Do This (permission):
Inputs:
Business Rules / Invariants:
State Transitions (if any):
What Gets Persisted:
What the Caller Sees Back:
```

Do not skip straight to code from a one-line request — a vague requirement produces a vague contract. If the requirement is ambiguous about ownership (which module) or a business rule, resolve that first; don't guess and build on the guess.

## Step 1 — Confirm This Isn't Just `build-api-endpoint`

Per §1.1. If Domain/Application/Infrastructure already fully support this and only a route is missing, switch skills now.

## Step 2 — Design the Data Model (if new/changed entity)

Full detail lives in `design-database` — invoke it (or apply its checklist inline for a small addition):

1. Decide ownership (which module's schema).
2. Model the entity: inherit `AuditableEntity` (audit fields + `xmin` concurrency) unless there's a stated reason not to.
3. Identify required fields, constraints, natural keys, relationships (same-module navigation vs cross-module id-only + raw-SQL FK).
4. Model lifecycle/status as an enum with domain methods enforcing valid transitions — not a public-setter string.

Do not persist anything yet — this step is modeling, not migration.

## Step 3 — Entity / Aggregate (`Domain/`)

Write the entity with:

* Private setters, a `private` parameterless constructor for EF (`// EF Core` comment, matching `User.cs`).
* A `public static Create(...)` factory that validates invariants via `Guard.*` and returns a fully-valid instance.
* `public void <Behavior>()` methods for every state change (`Activate()`, `UpdateProfile(...)`) — never a public setter that lets the caller put the entity in an invalid state.
* Domain exceptions (`BusinessRuleValidationException`, or a specific one from `SharedKernel/Exceptions.cs` if it fits) thrown from inside these methods when an invariant would be violated — not checked ad hoc in the Application service.

No EF Core, no ASP.NET Core references in this file.

## Step 4 — Persistence Port + Infrastructure

1. Add `DbSet<TEntity>` to `I<Module>DbContext` (Application) and the concrete `<Module>DbContext` (Infrastructure).
2. Add `<Entity>Configuration : IEntityTypeConfiguration<TEntity>` under `Infrastructure/Configurations/`, mirroring `UserConfiguration.cs` (`ToTable`, `HasKey`, `RowVersion.IsRowVersion()`, `HasMaxLength`/`IsRequired` per string, unique indexes on natural keys).
3. Generate and **hand-review** the migration (`design-database` §4 Step 5) — confirm schema, snake_case naming, and that only the intended tables change. Add any cross-module raw-SQL FK manually.

Skip this step entirely if the feature only adds behavior to an existing entity with no schema change.

## Step 5 — Request DTO

Add `Create<X>Request` / `Update<X>Request` / action-specific request records to `Application/<Feature>/<Feature>Contracts.cs`, as plain `record`s — not bound directly to the entity (api-design.md §10). One record per operation; don't reuse `Create<X>Request` for update.

## Step 6 — Validation

Add `<X>RequestValidator : AbstractValidator<<X>Request>` to `Application/<Feature>/<Feature>Validators.cs`, mirroring `UserValidators.cs`'s style (`RuleFor(x => x.Field).NotEmpty().MaximumLength(...)`). This covers **input validation** only (shape/format/required-ness) — business-rule validation belongs in Step 3's domain methods or Step 7's service, not here. Registration is automatic via `services.AddValidatorsFromAssembly(...)` in `<Module>Module.cs` — no manual registration needed per validator.

## Step 7 — Application Service (Orchestration + Business Rules)

Add the method to the feature's `I<X>Service`/`<X>Service` (or create the pair if this is a brand-new feature area):

* Orchestrate: check pre-conditions against `I<Module>DbContext` (e.g., duplicate check via `AnyAsync`), call the Domain factory/behavior method, `Add`/mutate on the `DbSet`, call `SaveChangesAsync(cancellationToken)` once.
* Throw the right exception for the right business condition: `NotFoundException` (missing resource), `ConflictException` (duplicate/state conflict `GlobalExceptionHandler` maps to `409`), `ForbiddenException` (authorization-adjacent business rule, e.g. scope access), `BusinessRuleValidationException` (a domain invariant violated at the orchestration level). Never a bare `throw new Exception(...)`.
* If the feature needs another module's data, depend on a port interface here (§8) — never reach across modules directly.
* Thread `CancellationToken` through every async call.
* Map entity → response DTO via a small private static method (mirror `UserService.ToDetails`) — not a mapping library, not inline duplication in every method.

Register the new service (and its interface) in `<Module>Module.cs` if it's new.

## Step 8 — Cross-Module Data (Only If Needed)

If this feature genuinely needs another module's data:

1. Define an interface in *this* module's `Application/` describing exactly what's needed (e.g., `IRoleLookupProvider`).
2. Confirm the *providing* module already exposes an equivalent public read contract in its own `Application`/`Infrastructure`; if not, that's a separate change to that module, not something this feature reaches in and grabs directly.
3. The adapter implementing the new port lives in `Host/AdminPlatform.Api/CrossModuleAdapters/`, registered once in `Program.cs` — never inside a module.

If the cross-module "reference" is really just an id with no behavior needed (e.g., storing `UserId` on a new entity), skip the port entirely — store the plain `Guid` and enforce integrity at the database level (§Step 4 / `design-database` §3).

## Step 9 — Response DTO

Add `<X>Response` (list/summary shape) and, if the resource has a richer single-item view, `<X>DetailsResponse` — following the existing `UserResponse`/`UserDetailsResponse` split. Never return the entity type from Application or Api.

## Step 10 — API Endpoint

Add the controller action following `build-api-endpoint` §3's exact shape: route (`api/v1/<resource>`, kebab-case action segments), `[RequirePermission(<Module>Permissions.X)]` with a new permission code added to both `<Module>Permissions.All` and the Migrator's seed catalog, correct status code (`201`+`CreatedAtAction` for creation, `204` for no-body actions, `200` otherwise), `[ProducesResponseType]`. The controller body is one call to the Application service — nothing else.

If the feature is a list endpoint, it takes `[FromQuery] PagedRequest` and returns `PagedResult<T>` via `ToPagedResultAsync` — never an unbounded collection.

## Step 11 — Tests

Hand off to `test-backend` for depth, but at minimum for a new feature:

* Domain: valid/invalid transitions and invariant violations tested directly against the entity.
* Application/Integration: happy path, validation failure, duplicate/conflict, not-found where relevant.
* API: `401` unauthenticated, `403` missing permission, `200`/`201`/`204` success shape.

## Step 12 — Security Self-Check

Before declaring done, run `review-security`'s checklist (§5–§11 there) against just this new surface: permission present and seeded, no entity leaking as a response, ownership/scope checked where relevant, no raw SQL built from input, no secret/PII over-logged.

## Step 13 — Build and Verify

```bash
dotnet build
dotnet test --filter FullyQualifiedName~<Feature>
dotnet test
```

Do not report success without having actually run these.

## Step 14 — Report

```text
## Feature Summary

Use Case:
...

Module:
...

## Layers Touched

Domain:        Added/Changed <Entity> — <behavior methods>
Persistence:   DbSet + Configuration + Migration <name>
Application:   <X>Service.<Method>, <X>RequestValidator
Api:           <Verb> /api/v1/<route> — permission <code> (new/existing, seeded: YES/NO)

## Contracts

Request:  <record>
Response: <record>

## Cross-Module Dependencies

None / Port: <interface> implemented by <adapter> in CrossModuleAdapters

## Verification

Build:  PASS/FAIL
Tests:  <N> PASS / <N> FAIL
Security self-check: PASS — see notes / issues found

## Breaking Changes

NONE / <explicit list>
```

---

# 6. Do Not

* Do not introduce MediatR, a generic repository, AutoMapper, or a response wrapper "to keep things consistent with typical Clean Architecture" — they are not part of this project's actual architecture; follow §3.
* Do not put business rules in the controller or the entity's setters — they belong in Domain factory/behavior methods and the Application service's orchestration.
* Do not skip the hand-review of a generated migration.
* Do not add a `ProjectReference` between modules to make a cross-module relationship "easier."
* Do not ship an endpoint without a registered, seeded permission unless its public status is explicit and intentional.
* Do not call this skill "done" without running build and tests.

---

# 7. Definition of Done

A feature is complete only when:

1. The business requirement was stated explicitly before coding (§Step 0).
2. Every layer the requirement actually touches was built following this project's real patterns (§3–§4), and no layer was skipped that the requirement needed.
3. Business rules live in Domain/Application, not the controller.
4. Persistence goes through `I<Module>DbContext`; any migration was hand-reviewed.
5. The endpoint has explicit request/response DTOs, FluentValidation, correct status codes, and `[RequirePermission]` with a seeded code (or documented public intent).
6. Cross-module data access (if any) went through a port + Host adapter, never a direct module reference.
7. `review-security`'s checklist was applied to the new surface.
8. `dotnet build` and relevant `dotnet test` runs actually passed.
9. The report lists every layer touched and any breaking change explicitly.
