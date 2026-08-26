# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
