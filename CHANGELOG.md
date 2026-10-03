# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Seo module (`seo` schema), step 1: the site-wide SEO settings singleton (title template with `%s`, default
  description / share image, default robots index/follow, Twitter handles, robots.txt disallow paths). Admin
  `GET/PUT /api/v1/seo/settings` gated by `seo-settings.view|update`; anonymous `GET /api/v1/seo/public/settings`
  for the website. `SeoSeeder` (part of `seed`) creates the defaults only when absent. Metadata, redirects and
  schema follow in later steps.
- Seo module, step 2: per-entity SEO metadata overrides (product/category/article/page/homepage; one row per
  entity, homepage singleton). Admin `GET /api/v1/seo/metadata`, `GET .../lookup`, upsert `PUT`, reset `DELETE`
  gated by `seo-metadata.view|update|delete`; anonymous `GET /api/v1/seo/public/metadata` and `/public/noindex`.
- Catalog module (`catalog` schema): categories (3-level tree), products (unique slug,
  ordered images and modifier groups), sales menus (immutable code) and their product
  placements (price override, order, availability), modifier groups with options.
  Admin CRUD under `/api/v1/catalog/*` gated by `categories.*`, `products.*`,
  `sales-menus.*`, `modifier-groups.*`; anonymous `GET /api/v1/catalog/public` snapshot
  of the active catalog for the website.
- `CatalogSeeder` (part of `seed`): the initial restaurant catalog, only into an empty catalog.
- Integration tests can target an existing Postgres via `INTEGRATION_TESTS_CONNECTION_STRING`.

### Changed

- Admin sidebar catalog entries are now gated by the new catalog permissions.

## [0.1.0] - 2026-08-26

### Added

- Modular Monolith / Clean Architecture solution on .NET 8 (LTS), Central Package
  Management, `global.json` pinned to the installed 9.0.305 SDK.
- Five business modules — Identity, AccessControl, Organization, Navigation,
  Platform — each a single project with internal Domain/Application/
  Infrastructure/Api layering, enforced by architecture tests rather than
  project-reference boundaries.
- JWT access tokens + rotating, hashed refresh tokens with reuse detection;
  login/refresh/logout/logout-all, session listing and per-session revoke.
- Role-Based + Permission-Based access control: dynamic `Permission:*`
  authorization policies resolved from the caller's JWT claims, no
  compile-time policy list.
- Organizations, self-referencing Department tree, Brands, and per-user
  Department/Brand scope assignment; a working-context switch validated
  against that scope.
- Dynamic, self-referencing Menu tree filtered per-caller by permission code,
  with a seeded base Dashboard + Administration sidebar.
- FiscalYears, SystemSettings, and an AuditLogs trail populated automatically
  from every module's `AuditableEntity` changes via a shared SaveChanges
  interceptor and a cross-module `IAuditEventSink` port.
- PostgreSQL via Npgsql/EF Core 8, snake_case naming convention, `xmin`-based
  optimistic concurrency, EF Core migrations per module with hand-added raw
  FK constraints for the cross-module (cross-schema) references.
- `AdminPlatform.Migrator`: a separate console tool with `migrate`/`seed`/`all`
  commands — the API host itself never migrates in Production, only
  optionally in Development behind an explicit flag.
- Centralized ProblemDetails error handling, FluentValidation wired as a
  global MVC filter, Serilog + correlation id, rate limiting on `/auth/*`,
  a Postgres health check, Swagger with a bearer scheme.
- Unit tests (43), architecture tests (20, including a module-boundary check),
  and an integration test suite (WebApplicationFactory + Testcontainers.
  PostgreSql) covering auth, CRUD, authorization, and the navigation tree.
- Multi-stage Dockerfile, docker-compose (db → migrator → api), `.env.example`,
  and a GitHub Actions CI workflow.

[Unreleased]: https://example.com/compare/v0.1.0...HEAD
[0.1.0]: https://example.com/releases/tag/v0.1.0
