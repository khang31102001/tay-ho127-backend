---
name: design-database
description: Design or evolve relational data models for this modular monolith — entity shape, constraints, relationships, indexes, and EF Core migrations — following the per-module PostgreSQL schema and no-repository persistence convention already in place.
---

# Design Database

## 1. Purpose

Use this skill when the task requires:

* Designing a new entity/aggregate's data model.
* Adding a relationship between entities (same module or cross-module).
* Reviewing or writing an EF Core migration.
* Adding/removing constraints, indexes, or columns on an existing table.
* Deciding how a new module's schema should be organized.

This skill governs *data modeling and migrations*. Wiring the entity into an endpoint (Application service, controller, validation) is `build-backend-feature`'s job — use that skill for the full vertical slice and come back here for the schema decisions it depends on.

---

# 2. Required Rules

```text
.claude/rules/database.md
.claude/rules/architecture.md
.claude/rules/naming.md
```

`database.md`: model from business invariants (not UI screens), explicit PK/constraints/indexes/ownership, deterministic/reversible migrations, no premature denormalization, audit/soft-delete as explicit decisions.

---

# 3. This Project's Actual Persistence Model — Read First

Confirmed from `docs/ARCHITECTURE.md` and the Identity module's implementation. Do not propose an alternative pattern (generic repository, Unit of Work, single shared `DbContext`) without a concrete reason — none of that is used here today.

* **One schema per module, one shared Postgres database.** `IdentityDbContext.Schema` (e.g. `"identity"`), `organization`, `access_control`, etc. Every table for a module lives in that module's schema.
* **Naming convention**: `UseSnakeCaseNamingConvention()` is applied per `DbContext` registration — table/column names in the database are `snake_case` (`users`, `full_name`) even though C# properties are `PascalCase`. Don't hand-pick different casing in `ToTable()`/`HasColumnName()` calls; let the convention do it unless a name must diverge intentionally.
* **No repository, no Unit of Work.** Application code depends on `I<Module>DbContext` — a hand-written interface exposing exactly the `DbSet<T>` properties that module's Application layer needs, plus `SaveChangesAsync`. The concrete `<Module>DbContext` (Infrastructure) implements it. A new entity means: add a `DbSet<T>` to both the interface and the concrete context — nothing else.
* **Base types** (`AdminPlatform.SharedKernel`):
  * `Entity` — abstract base, `Guid Id` (generated client-side via `Guid.NewGuid()` in factory methods, protected setter), value-equality by `Id` + concrete type.
  * `AuditableEntity : Entity` — adds `CreatedAtUtc`, `CreatedBy`, `UpdatedAtUtc`, `UpdatedBy`, and `RowVersion` (`uint`, mapped to Postgres `xmin` via `.IsRowVersion()` — free optimistic concurrency, not a manually maintained column).
  * `CatalogEntity` — for lookup/reference-style entities (check its shape in `SharedKernel` before assuming; do not reinvent a lookup base type).
  * A new entity should inherit `AuditableEntity` unless it explicitly has no audit/concurrency need — that is a deliberate, reportable decision, not a default.
* **Entity configuration**: one `internal sealed class <Entity>Configuration : IEntityTypeConfiguration<Entity>` per entity, under `Infrastructure/Configurations/`, applied via `ApplyConfigurationsFromAssembly` (verify the actual `OnModelCreating` call in `<Module>DbContext` before assuming). Follow the exact shape of `UserConfiguration.cs`: explicit `ToTable(...)`, `HasKey`, `RowVersion.IsRowVersion()`, `HasMaxLength` + `IsRequired` on every string, `HasIndex(...).IsUnique()` for natural keys.
* **Migrations**: live under `Infrastructure/Migrations/` per module, generated with `dotnet ef migrations add <Name> --project src/Modules/<Module>/... `. Each module also has an `IDesignTimeDbContextFactory` (`IdentityDbContextFactory`) so `dotnet ef` works standalone per module. `AdminPlatform.Migrator` (a console tool, never the API process) applies every module's migrations in the right order — never hand-run raw DDL against a live database.
* **Cross-module references are ids only, never EF navigations.** If module B needs a foreign key to module A's table (e.g., `organization.user_departments.user_id → identity.users.id`), store a plain `Guid` column with no navigation property, and add the actual database-level FK by hand in the migration:
  ```csharp
  migrationBuilder.Sql(
      "ALTER TABLE organization.user_departments " +
      "ADD CONSTRAINT fk_user_departments_identity_users_user_id " +
      "FOREIGN KEY (user_id) REFERENCES identity.users (id) ON DELETE CASCADE;");
  ```
  This is deliberate: it keeps the module free of a compile-time `ProjectReference` to another module while the database still enforces integrity. Invalid ids then surface as `400`/`409` automatically through `GlobalExceptionHandler`'s Postgres `23503`/`23505` mapping — do not add a manual existence-check call to another module for this.
* **Seeding**: each module has a `<Module>Seeder`, run by `AdminPlatform.Migrator`. New reference/lookup data goes there, not in a migration's `InsertData` unless the data is truly static and part of the schema itself.

---

# 4. Workflow

## Step 1 — Understand the Business Concept

Before touching a model, state:

```text
Entity/Aggregate:
Owning Module:
Business Invariants:
Lifecycle / Status Transitions (if any):
Who Creates It / Who Can Change It:
Relationships to Other Entities:
```

Do not derive the model from a UI mockup or a single screen's fields — ask what the entity *means*, not just what one form collects.

## Step 2 — Decide Ownership and Schema

Which module owns this table? A table lives in exactly one module's schema. If two modules both seem to need it, that is a sign the concept needs to be split (each module owns its own data) or one module is clearly the source of truth and the other references it by id (§3, cross-module rule).

## Step 3 — Model the Entity

* Choose `AuditableEntity` vs bare `Entity` vs `CatalogEntity` deliberately.
* Identify required vs optional fields, and their real constraints (max length, numeric precision/scale, allowed range) — not just "string" for everything.
* Use `decimal` for any monetary/financial value, never `float`/`double`.
* Model status/lifecycle as an enum with explicit valid transitions enforced by domain methods (mirroring `User.Activate()`/`Deactivate()`), not a bare mutable string.
* Identify uniqueness constraints (natural keys) and required relationships up front — retrofitting a unique index after data exists is more expensive than declaring it now.

## Step 4 — Model Relationships

* Same-module relationship: normal EF navigation + FK, configured explicitly in the entity's `IEntityTypeConfiguration`.
* Cross-module relationship: id-only column + raw-SQL FK in the migration (§3). Never add a `ProjectReference` to make a navigation property compile.
* Decide cascade behavior (`ON DELETE CASCADE` / `RESTRICT` / `SET NULL`) intentionally based on business meaning — a cascade delete on a financially relevant table is a decision worth calling out explicitly in the report.

## Step 5 — Add Persistence Plumbing

1. Add the entity class under `Domain/` (module), inheriting the right base type, with private setters and factory/behavior methods — not a public-setter anemic model (mirror `User.cs`).
2. Add `DbSet<TEntity>` to `I<Module>DbContext` (Application) and the concrete `<Module>DbContext` (Infrastructure).
3. Add `<Entity>Configuration : IEntityTypeConfiguration<TEntity>` under `Infrastructure/Configurations/`.
4. Generate the migration:
   ```bash
   dotnet ef migrations add <DescriptiveName> --project src/Modules/<Module>/AdminPlatform.Modules.<Module>
   ```
5. **Review the generated migration by hand** before accepting it — confirm the table lands in the correct schema, columns are `snake_case`, `RowVersion` maps to `xmin` correctly (should not appear as a normal column with a C# default), and no unintended `Drop`/`Alter` appears against existing tables.
6. Add any cross-module raw-SQL FK manually to the generated migration (EF will not generate it, since there's no navigation to infer it from).

## Step 6 — Indexes

Add indexes for:
* Natural keys / uniqueness constraints (`HasIndex(...).IsUnique()`).
* Columns used in real, expected filter/sort/join patterns (see `optimize-performance` §11 for when to add one after the fact based on measurement).

Do not index every column speculatively.

## Step 7 — Verify

```bash
dotnet build
```
Then, if a local/test database is available, apply the migration through the Migrator tool (never by hand) and confirm `dotnet ef migrations has-pending-model-changes` (or equivalent) shows no drift.

## Step 8 — Report

```text
## Database Change Summary

Module / Schema:
...

Entities Added/Changed:
...

Relationships:
Same-module: ...
Cross-module (id-only + raw SQL FK): ...

Constraints Added:
...

Indexes Added:
...

Migration:
<file name>

Reviewed Generated Migration:
YES — <what was checked/adjusted>

Breaking Change:
YES/NO — <existing table altered/dropped?>

Seeding Required:
YES/NO
```

---

# 5. Do Not

* Do not introduce a generic repository, Unit of Work, or a second `DbContext` per module — `I<Module>DbContext` + the module's single `<Module>DbContext` is the established pattern.
* Do not add a cross-module `ProjectReference` to satisfy a navigation property — use the id-only + raw-SQL-FK pattern instead.
* Do not regenerate or hand-edit historical migrations already applied elsewhere — only add new ones.
* Do not accept an EF-generated migration without reading it; a snake_case naming mismatch or an accidental `DropColumn` on an unrelated property is easy to miss.
* Do not use `float`/`double` for money, or a bare mutable string for a lifecycle/status field.
* Do not add PII/audit-sensitive columns without checking `review-security`'s data-exposure checklist for the response side.

---

# 6. Definition of Done

A database design task is complete only when:

1. Ownership (which module/schema) is explicit.
2. The entity models real business invariants, not just a UI form.
3. Relationships follow the same-module-navigation vs cross-module-id-only rule.
4. `I<Module>DbContext` and the concrete `DbContext` are both updated.
5. The migration was generated, hand-reviewed, and confirmed to touch only the intended schema/tables.
6. Constraints and indexes are deliberate, not speculative.
7. `dotnet build` passes.
8. Breaking changes (dropped/altered existing tables) are explicitly reported.
