---

name: upgrade-backend
description: Safely upgrade ASP.NET Core backend frameworks, .NET versions, EF Core, NuGet dependencies, deprecated APIs, and supporting infrastructure through compatibility analysis, incremental migration, verification, and explicit upgrade reporting.
------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

# Upgrade Backend

## 1. Purpose

Use this skill when the task requires:

* Upgrading .NET versions.
* Upgrading ASP.NET Core.
* Upgrading Entity Framework Core.
* Upgrading NuGet packages.
* Replacing deprecated APIs.
* Replacing obsolete libraries.
* Updating authentication libraries.
* Updating database providers.
* Updating testing libraries.
* Modernizing existing backend infrastructure.
* Migrating configuration to newer framework conventions.
* Reducing technical debt caused by outdated platform versions.

The objective is:

> Upgrade the backend incrementally while preserving application behavior, compatibility, security, and data integrity.

---

# 2. Required Rules

Always follow:

```text
.claude/rules/architecture.md
.claude/rules/code-quality.md
.claude/rules/naming.md
.claude/rules/api-design.md
.claude/rules/database.md
.claude/rules/security.md
.claude/rules/testing.md
.claude/rules/task-reporting.md
```

An upgrade task does not justify unrelated architectural rewrites.

---

# 3. Core Upgrade Principle

Use:

```text
Current State
     ↓
Compatibility Analysis
     ↓
Upgrade Plan
     ↓
Small Upgrade Step
     ↓
Build
     ↓
Test
     ↓
Fix Compatibility Issues
     ↓
Continue
```

Avoid:

```text
Update Everything
      ↓
Hundreds of Errors
      ↓
Rewrite Until Build Passes
```

---

# 4. Operating Modes

Determine the upgrade type first.

## Mode A — .NET Runtime Upgrade

Examples:

```text
.NET 8 → .NET 9
.NET 8 → .NET 10
```

Review:

* Target framework.
* SDK.
* Runtime behavior.
* ASP.NET Core changes.
* C# language changes.
* Deployment environment.

---

## Mode B — EF Core Upgrade

Review:

* Provider compatibility.
* Query translation.
* Migrations.
* Breaking ORM behavior.
* Interceptors.
* Value converters.
* Concurrency.
* Database generation.

---

## Mode C — NuGet Dependency Upgrade

Review:

* Direct dependencies.
* Transitive dependencies.
* Breaking changes.
* Security advisories.
* API changes.
* Compatibility matrix.

---

## Mode D — Deprecated API Migration

Use when existing framework APIs are obsolete.

Goal:

> Replace deprecated implementation without changing intended application behavior.

---

## Mode E — Infrastructure Modernization

Examples:

* Authentication modernization.
* OpenAPI changes.
* Logging changes.
* HttpClient configuration.
* Configuration modernization.
* New ASP.NET Core platform features.

Do not modernize merely because a newer API exists.

---

# 5. Inspect Before Upgrading

Before changing versions inspect:

* `.sln`
* `.csproj`
* `global.json`
* `Directory.Build.props`
* `Directory.Packages.props`
* `NuGet.config`
* Dockerfiles.
* CI/CD configuration.
* Deployment configuration.
* Runtime version.
* Test projects.
* Package references.
* EF Core provider.
* Migration project.
* Authentication packages.

Do not modify versions before understanding project dependency structure.

---

# 6. Identify Current Platform

Record:

```text
Current .NET:
Current ASP.NET Core:
Current EF Core:
Database Provider:
Test Framework:
Major Packages:
Deployment Runtime:
```

Do not assume the project targets the same version as the locally installed SDK.

---

# 7. Identify Target Platform

Define target explicitly.

Example:

```text
Current:
.NET 8

Target:
.NET 10
```

Do not automatically upgrade to the newest available version unless requested.

---

# 8. Direct vs Multi-Step Upgrade

Determine whether direct upgrade is safe.

Example:

```text
.NET 6 → .NET 10
```

may benefit from staged analysis:

```text
.NET 6
 ↓
.NET 8
 ↓
.NET 10
```

depending on dependency compatibility.

Do not force sequential upgrades if direct migration is officially supported and simpler.

---

# 9. Compatibility Matrix

Before implementation, identify compatibility among:

```text
.NET
ASP.NET Core
EF Core
Database Provider
Authentication
OpenAPI
Testing
Logging
Other Major Packages
```

One incompatible dependency can block the whole upgrade.

---

# 10. Package Inventory

Classify dependencies:

```text
Framework
Database
Security
Serialization
Validation
Mapping
Testing
Observability
Infrastructure
Utility
```

Identify:

* Required.
* Optional.
* Obsolete.
* Unused.
* Security-sensitive.

---

# 11. Do Not Upgrade Everything Automatically

Avoid changing all packages simply because newer versions exist.

Upgrade packages when:

* Required by target framework.
* Security vulnerability exists.
* Compatibility requires it.
* Explicit task requests it.
* Current package is unsupported.
* New version provides required capability.

---

# 12. Major vs Minor Updates

Classify each dependency update:

```text
PATCH
MINOR
MAJOR
```

Major upgrades require more compatibility analysis.

Do not treat:

```text
8.x → 9.x
```

as equivalent to:

```text
8.0.1 → 8.0.2
```

---

# 13. Framework Compatibility

Check whether packages support the target TFM.

Example:

```text
net8.0
net9.0
net10.0
```

Do not force unsupported packages through warning suppression.

---

# 14. TargetFramework

Update project target framework intentionally.

Example:

```xml
<TargetFramework>net10.0</TargetFramework>
```

Do not change every project blindly.

Check:

* Main API.
* Libraries.
* Tests.
* Tooling projects.

---

# 15. global.json

If `global.json` exists, review SDK pinning.

Example:

```json
{
  "sdk": {
    "version": "10.0.xxx"
  }
}
```

Do not create or remove `global.json` without understanding build infrastructure.

---

# 16. Language Version

Do not explicitly set the latest C# language version unless required.

Prefer framework defaults where possible.

Avoid:

```xml
<LangVersion>preview</LangVersion>
```

for production templates unless preview features are explicitly required.

---

# 17. Nullable

Do not disable nullable reference types to make an upgrade compile.

Fix meaningful nullable problems.

Avoid broad use of:

```csharp
!
```

only to suppress warnings.

---

# 18. Implicit Usings

If upgrading project templates, determine whether implicit usings improve consistency.

Do not introduce broad style churn unrelated to upgrade safety.

---

# 19. Obsolete APIs

Search for:

```text
Obsolete
Deprecated
Compiler warnings
Package deprecations
```

Replace obsolete APIs with recommended alternatives when appropriate.

Understand semantic differences before replacing.

---

# 20. Do Not Suppress Upgrade Warnings

Avoid:

```text
#pragma warning disable
NoWarn
```

as the primary upgrade strategy.

Suppress only when warning is understood and intentionally accepted.

---

# 21. ASP.NET Core Hosting Model

Respect current hosting structure.

Modern ASP.NET Core commonly uses:

```text
WebApplication.CreateBuilder
```

Do not rewrite an established hosting model unless target version or maintainability requires it.

---

# 22. Program.cs Upgrade

Keep `Program.cs` focused on composition.

During framework migration:

* Update obsolete configuration.
* Preserve middleware order.
* Preserve authentication.
* Preserve routing.
* Preserve environment behavior.

Do not reorganize business architecture while updating hosting APIs.

---

# 23. Middleware Order

After ASP.NET Core upgrade verify middleware order.

Especially:

```text
Exception Handling
HTTPS
CORS
Authentication
Authorization
Routing
Endpoints
```

Incorrect middleware order can create runtime/security regressions.

---

# 24. Authentication Upgrade

Authentication libraries are security-sensitive.

Review:

* Authentication scheme.
* Token validation.
* Cookie settings.
* Claims.
* Signing.
* Expiration.
* External providers.

Do not weaken validation to restore compatibility.

---

# 25. Authorization Upgrade

Verify:

* Policies.
* Roles.
* Permissions.
* Fallback policy.
* Endpoint metadata.

Framework upgrades must not accidentally expose protected endpoints.

---

# 26. JWT Package Changes

If JWT libraries change, verify:

```text
Issuer
Audience
SigningKey
Lifetime
Claims
ClockSkew
```

Add authentication regression tests.

---

# 27. EF Core Version Alignment

Prefer aligning:

```text
Microsoft.EntityFrameworkCore
Provider
Tools
Design
```

to compatible versions.

Avoid mismatched EF Core major versions unless provider officially supports them.

---

# 28. Database Provider

Check target provider support.

Examples:

```text
SQL Server
PostgreSQL / Npgsql
MySQL
SQLite
```

Do not upgrade EF Core without verifying database provider compatibility.

---

# 29. EF Core Query Behavior

Major EF upgrades may change query translation.

Review critical queries involving:

* GroupBy.
* Contains.
* Date operations.
* JSON.
* Complex projections.
* Owned types.
* Value converters.

A query compiling does not guarantee identical SQL behavior.

---

# 30. Generated SQL Verification

For critical queries, compare SQL when upgrade risk is meaningful.

Look for:

* Additional joins.
* Changed filters.
* Client evaluation.
* Query failures.
* Different null semantics.

---

# 31. EF Core Migrations

Do not regenerate migration history during an EF upgrade.

Existing migrations are part of deployment history.

Only create new migrations when actual model/schema changes require them.

---

# 32. Snapshot Changes

Inspect EF model snapshot changes.

Framework upgrade alone should not unexpectedly generate major schema modifications.

If it does:

> Investigate before applying migration.

---

# 33. Migration Safety

If an upgrade unexpectedly produces:

```text
DropColumn
DropTable
AlterColumn
```

stop and analyze.

Do not accept destructive migrations just because EF generated them.

---

# 34. Database Behavior

After EF/provider upgrade test:

* Reads.
* Writes.
* Relationships.
* Transactions.
* Unique constraints.
* Concurrency.
* Date/time behavior.

---

# 35. Serialization

Framework/library updates can change JSON behavior.

Verify:

* Property casing.
* Null handling.
* Enum representation.
* Date serialization.
* Custom converters.
* Reference handling.

Public API contracts must remain stable unless explicitly changed.

---

# 36. OpenAPI

Review OpenAPI/Swagger package compatibility.

Check:

* Endpoint discovery.
* Authentication scheme.
* Schemas.
* Required properties.
* Enum representation.

Do not let an upgrade unintentionally change API documentation contracts.

---

# 37. Validation Libraries

If validation packages are upgraded, verify:

* Rule behavior.
* Async validation.
* Dependency injection.
* Model validation integration.

Do not rewrite validation architecture merely because APIs changed.

---

# 38. Mapping Libraries

If AutoMapper or another mapper is present:

Verify:

* Profiles load.
* Validation.
* Projection behavior.
* EF translation when applicable.

Do not upgrade mapping library unless necessary.

---

# 39. Logging

Verify logging behavior after upgrade.

Check:

* Structured fields.
* Providers.
* Log level.
* Request logging.
* Sensitive-data exposure.

Do not enable verbose sensitive logs as an upgrade workaround.

---

# 40. HttpClient

Ensure external integrations continue using correct `HttpClient` behavior.

Check:

* Base address.
* Timeout.
* Authentication headers.
* Serialization.
* Resilience pipeline.

---

# 41. Resilience Libraries

If moving from older Polly integrations or equivalent mechanisms:

Preserve intended:

* Retry.
* Timeout.
* Circuit breaker.
* Failure behavior.

Do not blindly recreate old policy syntax without understanding new semantics.

---

# 42. Dependency Injection

Framework/package upgrades may expose invalid DI registrations.

Check:

* Missing registrations.
* Lifetime mismatches.
* Constructor changes.
* Removed extension methods.

Fix actual configuration.

Do not use service locator as a compatibility workaround.

---

# 43. Configuration Binding

Verify strongly typed options.

Changes in binding behavior can expose invalid configuration.

Check:

```text
Required configuration
Validation
Section names
Environment overrides
```

---

# 44. Secrets

Upgrade tasks must not move secrets into source-controlled configuration merely to make local execution easier.

Preserve external secret management.

---

# 45. Date / Time Behavior

Major database/provider/framework changes may affect:

* DateOnly.
* TimeOnly.
* DateTimeOffset.
* UTC conversion.
* JSON formatting.

Test date-sensitive business behavior.

---

# 46. Background Jobs

If Hangfire, Quartz, or similar infrastructure exists:

Check:

* Target framework support.
* Storage compatibility.
* Serialization.
* Job method compatibility.

Do not replace job infrastructure without a real requirement.

---

# 47. Messaging

If messaging infrastructure exists:

Review client-library compatibility.

Verify:

* Message serialization.
* Headers.
* Retry behavior.
* Consumer registration.
* Acknowledgment behavior.

Message contracts must remain stable.

---

# 48. Cache Libraries

When distributed cache/Redis libraries are upgraded:

Verify:

* Serialization.
* Key naming.
* Expiration.
* Connection behavior.
* Failure handling.

Do not flush or invalidate production cache assumptions silently.

---

# 49. Testing Framework Upgrade

When upgrading xUnit/NUnit/MSTest:

Verify:

* Test discovery.
* Fixtures.
* Async tests.
* Parallelization.
* Assertions.
* Test runner compatibility.

A build passing with zero discovered tests is NOT a successful upgrade.

---

# 50. Test Discovery

Always verify expected tests are actually discovered.

Report:

```text
Total tests discovered
Passed
Failed
Skipped
```

when available.

---

# 51. Mocking Libraries

If mocking library APIs changed:

Update tests without changing tested production behavior.

Do not weaken assertions to accommodate library upgrades.

---

# 52. Container / Docker Upgrade

If runtime container changes:

Review:

* Base image.
* .NET runtime version.
* Port behavior.
* User permissions.
* Certificates.
* Timezone.
* Health checks.

Do not assume local SDK upgrade automatically updates deployment images.

---

# 53. CI/CD

Review build pipeline for:

* SDK version.
* Restore.
* Build.
* Test.
* Publish.
* Docker image.
* Deployment environment.

Code upgrade is incomplete if CI still targets an incompatible runtime.

---

# 54. Deployment Runtime

Ensure hosting environment supports target framework.

Examples:

```text
Docker
IIS
Azure App Service
Linux VM
Kubernetes
```

Do not upgrade project target beyond available deployment runtime without reporting deployment impact.

---

# 55. Build Configuration

Test relevant build modes.

At minimum when practical:

```bash
dotnet restore
dotnet build
```

For deployment-sensitive upgrades also consider:

```bash
dotnet publish
```

---

# 56. Restore

Run:

```bash
dotnet restore
```

after dependency changes.

Inspect:

* Restore failures.
* Package conflicts.
* Version resolution.

Do not ignore restore warnings involving critical dependencies.

---

# 57. Build

Run:

```bash
dotnet build
```

Fix errors introduced by upgrade.

Do not suppress new warnings indiscriminately.

---

# 58. Test

Run:

```bash
dotnet test
```

and relevant targeted suites.

Verify test discovery count where possible.

---

# 59. Publish

For runtime/framework upgrades, consider:

```bash
dotnet publish
```

to identify deployment-specific issues.

Build success alone may not prove publish success.

---

# 60. Runtime Smoke Test

When environment permits, verify application startup.

Check:

* Dependency injection.
* Configuration.
* Database connection.
* Middleware.
* Endpoint mapping.

Do not claim runtime compatibility based only on compilation.

---

# 61. Health Check

If project exposes health checks, verify them after runtime upgrade.

Health checks may reveal infrastructure incompatibility.

---

# 62. API Regression

Test important endpoints.

Especially:

* Authentication.
* Users.
* Critical business flows.
* Database operations.
* External integrations.

Framework upgrade must preserve existing contracts.

---

# 63. Security Regression

After security-related library upgrades verify:

```text
Unauthenticated → denied
Unauthorized → denied
Valid authentication → works
Expired token → denied
```

Do not assume newer packages preserve configuration semantics automatically.

---

# 64. Database Regression

Verify critical:

```text
Read
Create
Update
Delete
Transaction
Migration
```

behavior.

---

# 65. Integration Regression

Test critical third-party integrations if available.

Do not make actual production transactions during verification.

Use appropriate test/sandbox boundaries.

---

# 66. Upgrade in Small Batches

Prefer:

```text
Framework
   ↓
Build/Test

EF Core
   ↓
Build/Test

Remaining Required Packages
   ↓
Build/Test
```

when dependency graph allows it.

This makes failures easier to attribute.

---

# 67. Package Grouping

Packages that must stay version-aligned may be upgraded together.

Example:

```text
EF Core Runtime
EF Core Design
EF Core Tools
Database Provider
```

Do not artificially separate tightly coupled packages.

---

# 68. Avoid Mixed Refactoring

Do not combine upgrade with:

* Large naming cleanup.
* Folder redesign.
* Full architecture refactor.
* Business feature development.

unless required.

A clean upgrade diff is easier to review and rollback.

---

# 69. Deprecated Package Replacement

When replacing an obsolete package:

1. Understand what capability it provides.
2. Identify all usages.
3. Select replacement.
4. Compare semantics.
5. Migrate incrementally.
6. Remove old dependency.
7. Verify.

Do not install replacement and leave old package indefinitely without reason.

---

# 70. Built-In Framework Preference

When modern .NET provides built-in functionality equivalent to an old third-party dependency, evaluate removing the dependency.

Examples may include:

* Rate limiting.
* Output caching.
* ProblemDetails.
* Resilience capabilities.

Do not migrate merely for novelty.

---

# 71. Dependency Removal

Before removing a package:

Search:

* Source references.
* DI registration.
* Configuration.
* Reflection.
* Build tooling.
* Tests.

Build/test afterward.

---

# 72. Breaking Change Analysis

For every major upgrade identify:

```text
Source Breaking Changes
Behavior Breaking Changes
Binary Compatibility
Configuration Changes
Deployment Changes
Database Changes
API Changes
```

Do not assume compiler errors reveal every breaking change.

---

# 73. Compatibility Classification

Classify impact:

```text
NONE
LOW
MEDIUM
HIGH
```

Examples:

```text
LOW:
Patch-level package upgrade.

MEDIUM:
EF Core major upgrade.

HIGH:
.NET + authentication + database provider major upgrade.
```

---

# 74. Upgrade Risk

Classify overall upgrade risk:

```text
LOW
MEDIUM
HIGH
```

Higher risk means stronger staging and verification.

---

# 75. Rollback Awareness

For high-risk upgrades identify rollback considerations.

Examples:

* Previous application image.
* Previous package lock state.
* Database compatibility.
* Migration reversibility.

Do not claim rollback is possible if schema changes are irreversible.

---

# 76. Database Compatibility Window

For staged deployments, consider whether old and new application versions can operate against the same schema.

Do not deploy incompatible schema ahead of code without planning.

---

# 77. Feature Flags

Do not introduce feature flags automatically.

They may help high-risk migrations where old/new behavior must coexist.

Use only when operational need exists.

---

# 78. Remove Temporary Compatibility Code

During migration, temporary adapters may be needed.

After upgrade is complete and old path is no longer required:

Remove temporary compatibility code when safe.

Do not leave permanent migration scaffolding without purpose.

---

# 79. Architecture Modernization

Framework upgrade does not automatically mean architecture rewrite.

If old architecture remains valid:

> Keep it.

Use separate refactoring tasks for structural improvements.

---

# 80. New Framework Features

Adopt new framework features only when they:

* Replace obsolete APIs.
* Reduce dependency.
* Improve maintainability.
* Solve an actual requirement.

Do not migrate every pattern to the newest possible syntax.

---

# 81. Minimal APIs

Do not convert controllers to Minimal APIs merely because newer ASP.NET Core supports them.

This is an architecture decision, not a framework-upgrade requirement.

---

# 82. CQRS / MediatR

Do not introduce/remove CQRS or MediatR during framework upgrade unless compatibility requires it.

Keep behavioral architecture stable.

---

# 83. Generic Repository

Do not introduce repository abstraction while upgrading EF Core.

Likewise, do not remove established repository architecture unless it causes an actual upgrade issue.

---

# 84. Performance Regression

Major upgrades should consider performance regression.

Potential areas:

* EF queries.
* Serialization.
* Startup.
* Memory.
* Database provider.

Measure critical paths where performance is important.

---

# 85. Performance Improvements

Do not claim an upgrade improves performance merely because release notes suggest it.

Report actual project measurement separately.

---

# 86. Package Security

Where tooling supports it, check package vulnerabilities.

Possible:

```bash
dotnet list package --vulnerable
```

Use actual output only.

Do not make unsupported security claims.

---

# 87. Outdated Packages

If supported, identify outdated packages.

Do not automatically upgrade every outdated package.

Classify:

```text
Required Upgrade
Recommended
Optional
Leave As-Is
```

---

# 88. Upgrade Planning

For significant upgrades create a concise plan.

Example:

```text
Phase 1 — Platform
- TargetFramework
- SDK

Phase 2 — Core Packages
- ASP.NET
- EF Core
- Database provider

Phase 3 — Supporting Packages
- Authentication
- OpenAPI
- Tests

Phase 4 — Verification
- Build
- Tests
- Smoke test
```

---

# 89. Upgrade Workflow

When this skill is activated:

## Step 1 — Inspect

Determine current platform and dependencies.

---

## Step 2 — Define Target

Identify exact desired versions.

---

## Step 3 — Inventory Dependencies

Classify relevant packages and tools.

---

## Step 4 — Compatibility Analysis

Identify incompatible or risky components.

---

## Step 5 — Risk Assessment

Classify:

```text
LOW
MEDIUM
HIGH
```

---

## Step 6 — Create Upgrade Plan

Break upgrade into controlled stages.

---

## Step 7 — Baseline Build/Test

When practical:

```bash
dotnet build
dotnet test
```

before upgrading.

Record existing failures.

---

## Step 8 — Upgrade Platform

Change target framework/SDK where required.

---

## Step 9 — Upgrade Required Dependencies

Update compatible packages.

---

## Step 10 — Fix Compilation Issues

Replace deprecated APIs and incompatibilities.

Do not change business behavior unnecessarily.

---

## Step 11 — Review Configuration

Check framework/security/database configuration.

---

## Step 12 — Review EF Core

If applicable:

* Queries.
* Provider.
* Migrations.
* Snapshot.

---

## Step 13 — Restore

Run:

```bash
dotnet restore
```

---

## Step 14 — Build

Run:

```bash
dotnet build
```

---

## Step 15 — Test

Run:

```bash
dotnet test
```

---

## Step 16 — Publish / Smoke Test

When applicable:

```bash
dotnet publish
```

and verify application startup.

---

## Step 17 — Regression Review

Check:

```text
API
Database
Authentication
Authorization
Business Logic
Integrations
```

---

## Step 18 — Clean Temporary Code

Remove unnecessary migration workarounds.

---

## Step 19 — Review Diff

Ensure upgrade did not contain unrelated changes.

---

## Step 20 — Report

Produce global CHANGE REPORT plus upgrade-specific report.

---

# 90. Upgrade Report

Always include:

```text
## Upgrade Summary

Upgrade Type:
...

From:
...

To:
...

Risk:
LOW / MEDIUM / HIGH

## Versions

.NET:
old → new

EF Core:
old → new

Database Provider:
old → new

Packages:
- PackageA old → new
- PackageB old → new
```

Only list packages actually changed.

---

# 91. Compatibility Report

Include:

```text
## Compatibility Changes

Deprecated APIs Replaced:
...

Configuration Changes:
...

Runtime Behavior Changes:
...

Database Compatibility:
...

API Compatibility:
...

Deployment Impact:
...
```

---

# 92. Breaking Change Report

Explicitly state:

```text
Breaking API Change:
YES / NO

Database Breaking Change:
YES / NO

Authentication Change:
YES / NO

Deployment Requirement:
YES / NO
```

If yes, explain exactly what changed.

---

# 93. Removed Dependencies

Report removed packages:

```text
[REMOVED]
Old.Package

Reason:
Capability is now provided by framework / package is obsolete / no longer used.
```

---

# 94. New Dependencies

Report:

```text
[ADDED]
New.Package

Reason:
Required for target framework compatibility.
```

Do not hide dependency additions.

---

# 95. Upgrade Verification

Example:

```text
Restore:
PASS

Build:
PASS

Tests:
154 PASS
0 FAIL
2 SKIPPED

Publish:
PASS

Application Startup:
PASS

Database Migration:
NOT RUN

Production Deployment:
NOT RUN
```

Use only actual verification results.

---

# 96. Baseline Comparison

If failures existed before upgrade:

```text
Before:
2 failing tests.

After:
2 same tests failing.

New upgrade-related failures:
0
```

This distinguishes existing problems from upgrade regressions.

---

# 97. Remaining Upgrade Issues

Report deferred items.

Example:

```text
Remaining:

Package:
Legacy.Reporting 4.x

Status:
Not upgraded.

Reason:
Latest version requires breaking reporting API migration.

Recommendation:
Handle as separate migration task.
```

---

# 98. Upgrade Recommendations

Recommendations must be prioritized.

Use:

```text
NEXT
LATER
OPTIONAL
```

Avoid generating a long wishlist unrelated to current upgrade.

---

# 99. No False Upgrade Claims

Never say:

```text
Fully compatible
Production ready
Zero regression
Safe to deploy
```

unless appropriate verification supports it.

Prefer precise wording:

```text
Build and automated test suite pass.
Production deployment and production-data migration were not performed.
```

---

# 100. Stop Condition

Stop when:

* Requested target version is reached.
* Required dependencies are compatible.
* Build succeeds.
* Relevant tests are verified.
* Critical runtime regressions are addressed.
* Remaining optional upgrades are documented.

Do not continue upgrading unrelated packages indefinitely.

---

# 101. Definition of Done

An upgrade task is complete only when:

1. Current platform is understood.
2. Target version is explicit.
3. Dependency compatibility is analyzed.
4. Upgrade is performed incrementally.
5. Deprecated APIs are handled intentionally.
6. Security behavior is preserved.
7. API compatibility is reviewed.
8. Database compatibility is reviewed.
9. Deployment requirements are reviewed.
10. Build succeeds when executable.
11. Relevant tests are run when available.
12. Upgrade regressions are identified.
13. Version changes are reported explicitly.
14. Remaining incompatibilities are disclosed.

The objective is:

> Move the backend forward without turning a platform upgrade into an uncontrolled application rewrite.
