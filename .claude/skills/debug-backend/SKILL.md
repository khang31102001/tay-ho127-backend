---

name: debug-backend
description: Diagnose and fix ASP.NET Core backend defects by identifying root cause, limiting change scope, preserving existing behavior, verifying regression risk, and reporting exactly what changed.
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

# Debug Backend

## 1. Purpose

Use this skill when the task requires:

* Investigating runtime errors.
* Fixing HTTP 4xx/5xx defects.
* Fixing incorrect business behavior.
* Fixing EF Core/database issues.
* Fixing dependency injection errors.
* Fixing authentication or authorization issues.
* Fixing configuration problems.
* Fixing integration failures.
* Fixing async/concurrency problems.
* Fixing performance-related defects.
* Investigating failing tests.
* Investigating intermittent or environment-specific issues.

The primary goal is:

> Identify the actual root cause and implement the smallest safe fix.

Do not patch symptoms without understanding why the defect occurs.

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

Debugging does not override architecture, security, or compatibility rules.

---

# 3. Core Debugging Principle

Use this sequence:

```text
Symptom
   ↓
Evidence
   ↓
Reproduce
   ↓
Trace Execution
   ↓
Root Cause
   ↓
Minimal Fix
   ↓
Regression Verification
```

Never use:

```text
Error
  ↓
Guess
  ↓
Change random code
```

---

# 4. Do Not Fix Before Inspecting

Before changing code:

1. Read the error message.
2. Read stack trace if available.
3. Locate the failing component.
4. Read related code.
5. Trace callers.
6. Check configuration.
7. Check database behavior if relevant.
8. Check logs.
9. Check tests.
10. Check recent related changes if available.

Do not immediately rewrite the failing file.

---

# 5. Classify the Defect

Classify the issue before investigation.

Possible categories:

```text
Compilation
Runtime Exception
Business Logic
Validation
API Contract
Database
Migration
Dependency Injection
Authentication
Authorization
Configuration
Integration
Async
Concurrency
Performance
Environment
Deployment
Test Failure
Unknown
```

The classification determines what evidence to inspect first.

---

# 6. Gather Evidence

Use available evidence such as:

* Exception message.
* Stack trace.
* HTTP status.
* Request payload.
* Response payload.
* Logs.
* Database state.
* Generated SQL.
* Failed tests.
* Build output.
* Configuration.
* Environment variables.
* Package versions.

Do not ignore evidence that contradicts the initial assumption.

---

# 7. Reproduce the Issue

When possible, reproduce the failure before fixing.

Record:

```text
Input
Expected Behavior
Actual Behavior
Error / Output
Environment
```

If reproduction is not possible, explicitly state that.

Do not claim the issue is fixed without meaningful verification.

---

# 8. Establish Expected Behavior

Before changing implementation, determine:

> What should the system actually do?

Use:

* Existing tests.
* Existing business logic.
* API contracts.
* Documentation.
* Similar features.
* User requirement.

Do not invent expected business behavior.

---

# 9. Trace Execution Flow

Trace the request through relevant layers.

Example:

```text
HTTP Request
   ↓
Middleware
   ↓
Authentication
   ↓
Authorization
   ↓
Controller / Endpoint
   ↓
Application
   ↓
Domain
   ↓
Infrastructure
   ↓
Database / External Service
```

Identify exactly where behavior first becomes incorrect.

---

# 10. Root Cause vs Symptom

Always distinguish:

```text
Symptom:
HTTP 500

Immediate Failure:
NullReferenceException

Root Cause:
Required relationship was not loaded after query refactor.
```

Fix the root cause, not only the exception site.

---

# 11. Root Cause Statement

Before implementing the fix, be able to state:

```text
The failure occurs because <cause>,
which leads to <incorrect behavior>,
when <condition>.
```

If this statement cannot be made with reasonable confidence, continue investigating.

---

# 12. Scope Control

Fix only what is necessary.

Do not combine debugging with:

* Unrelated architecture cleanup.
* Global renaming.
* Package upgrades.
* Folder restructuring.
* Large refactoring.

unless the root cause genuinely requires it.

---

# 13. Minimal Safe Fix

Prefer the smallest change that:

1. Corrects root cause.
2. Preserves existing valid behavior.
3. Does not introduce unnecessary abstraction.
4. Is testable.
5. Matches existing project conventions.

Minimal does not mean temporary or fragile.

---

# 14. Avoid Workarounds

Avoid fixes such as:

```csharp
try
{
    ...
}
catch
{
}
```

or:

```csharp
value ??= "";
```

when they merely hide a deeper defect.

Do not suppress exceptions or warnings without understanding them.

---

# 15. NullReferenceException

When debugging null errors:

Do not automatically add:

```text
?
??
!
```

Find why the value is null.

Determine whether null is:

* Valid.
* Invalid.
* Missing validation.
* Missing query data.
* Missing configuration.
* Incorrect lifecycle.
* Incorrect mapping.

Fix the semantic problem.

---

# 16. InvalidOperationException

Investigate common causes:

* Invalid EF Core state.
* Missing service registration.
* Duplicate registration.
* Invalid LINQ operation.
* Missing sequence item.
* Invalid authentication configuration.

Do not catch and ignore the exception.

---

# 17. Dependency Injection Errors

When DI resolution fails, inspect:

* Registration exists.
* Correct lifetime.
* Interface/implementation mapping.
* Constructor dependencies.
* Circular dependency.
* Configuration registration.
* Open generic registration.

Typical flow:

```text
Requested Service
    ↓
Constructor
    ↓
Dependency Tree
    ↓
Missing / Invalid Registration
```

Do not resolve services manually from `IServiceProvider` merely to bypass DI problems.

---

# 18. Lifetime Problems

Check for invalid lifetime combinations such as:

```text
Singleton
   ↓
Scoped dependency
```

Avoid converting everything to singleton or transient as a workaround.

Choose lifetime based on actual ownership and usage.

---

# 19. API 400 Errors

Investigate:

* Model binding.
* Validation.
* JSON structure.
* Required properties.
* Enum parsing.
* Route/query mismatch.
* Content-Type.

Do not weaken validation just to accept malformed requests unless contract requirements change.

---

# 20. API 401 Errors

Check:

* Authentication scheme.
* Token presence.
* Token expiration.
* Token signature.
* Issuer.
* Audience.
* Cookie configuration.
* Middleware order.

Do not bypass authentication to make the endpoint work.

---

# 21. API 403 Errors

Check:

* User authenticated.
* Required policy.
* Role/permission.
* Claims.
* Resource ownership.
* Tenant context.

Do not replace `403` with unrestricted access as a fix.

---

# 22. API 404 Errors

Determine whether:

* Route is incorrect.
* Resource genuinely does not exist.
* Query scope is wrong.
* Tenant filter removed resource.
* Authorization intentionally hides resource.
* Soft-delete filter applies.

Do not convert all missing data into `500`.

---

# 23. API 409 Errors

Investigate:

* Unique constraints.
* Invalid state transition.
* Concurrency conflicts.
* Duplicate operations.

Ensure the conflict represents real business/system state.

---

# 24. API 500 Errors

For unexpected server errors:

1. Capture exception type.
2. Inspect stack trace.
3. Identify first application frame.
4. Trace input/state.
5. Find root cause.
6. Add regression coverage.

Do not return internal exception details to clients.

---

# 25. Business Logic Defects

When output is technically successful but logically wrong:

Compare:

```text
Expected Rule
     vs
Current Rule
```

Trace:

* Conditions.
* Status transitions.
* Calculations.
* Defaults.
* Date handling.
* Permission logic.
* Duplicate handling.

Do not change business rules without confirming intended behavior.

---

# 26. Calculation Errors

For financial or numeric defects, inspect:

* Data type.
* Decimal precision.
* Rounding.
* Order of operations.
* Currency.
* Quantity.
* Tax rules.
* Null/default behavior.

Do not use `double` as an emergency fix for monetary logic.

---

# 27. Date / Time Defects

Check:

* UTC vs local time.
* `DateTime` vs `DateOnly`.
* Timezone conversion.
* Inclusive/exclusive ranges.
* Midnight boundaries.
* Database provider behavior.

Do not arbitrarily add/subtract hours to make values appear correct.

---

# 28. EF Core Query Defects

Inspect:

* Generated query shape.
* Includes.
* Projection.
* Tracking.
* Global query filters.
* Navigation loading.
* Null relationships.
* Client-side evaluation.
* Materialization timing.

Do not blindly add `Include()` everywhere.

---

# 29. N+1 Problems

When excessive database calls occur:

Identify repeated query patterns.

Prefer:

* Projection.
* Joins.
* Correct include.
* Batch loading.

Do not solve N+1 by loading the entire database graph.

---

# 30. Database Constraint Failures

For FK/unique/not-null failures:

Determine whether the problem is:

* Invalid application state.
* Missing validation.
* Race condition.
* Bad migration.
* Existing corrupt data.
* Incorrect relationship configuration.

Do not remove database constraints merely to allow invalid writes.

---

# 31. Migration Defects

Inspect migration operations carefully.

Check for:

```text
Drop
Rename
Alter
Default
Foreign Key
Index
Data Backfill
```

Do not regenerate migration history casually.

Preserve existing deployed migration history.

---

# 32. Transaction Problems

For inconsistent writes:

Check:

* Number of `SaveChanges`.
* Transaction boundaries.
* External calls.
* Exception handling.
* Retry behavior.

Determine what operations must be atomic.

---

# 33. Concurrency Defects

For lost updates or duplicate processing, investigate:

* Simultaneous requests.
* Optimistic concurrency.
* Unique constraints.
* Idempotency.
* Transaction isolation.

Do not introduce global locks without understanding contention.

---

# 34. Async Defects

Check for:

```text
.Result
.Wait()
async void
missing await
Task.Run()
fire-and-forget
```

inside server request flows.

Ensure exceptions from asynchronous operations are observable.

---

# 35. Cancellation Problems

Check whether cancellation is incorrectly:

* Ignored.
* Converted into server error.
* Triggered too early.

Propagate `CancellationToken` consistently.

---

# 36. External API Failures

Inspect:

* URL.
* Credentials.
* Headers.
* Request payload.
* Response status.
* Response schema.
* Timeout.
* Retry.
* Cancellation.

Distinguish:

```text
Remote business error
Remote availability error
Network error
Local integration bug
```

---

# 37. Timeout Defects

Do not increase timeout immediately.

First determine why the operation exceeds expected duration.

Potential causes:

* Slow query.
* Deadlock.
* External service latency.
* Large payload.
* Infinite/repeated processing.

Increase timeout only when legitimate workload requires it.

---

# 38. Retry Defects

Do not retry permanent failures.

Retry only transient failures when safe.

Check whether retry can duplicate:

* Payments.
* Inserts.
* Messages.
* External actions.

Consider idempotency.

---

# 39. Authentication Defects

Inspect:

* Token creation.
* Claims.
* Signing key.
* Validation parameters.
* Expiration.
* Clock skew.
* Scheme registration.
* Middleware order.

Do not weaken token validation to fix invalid tokens.

---

# 40. Authorization Defects

Inspect:

* Policy registration.
* Permission source.
* Claims transformation.
* Role mappings.
* Ownership checks.
* Cache invalidation.

Do not hard-code admin bypass unless explicitly part of design.

---

# 41. Configuration Defects

Check:

* Key names.
* Environment-specific files.
* Environment variables.
* Options binding.
* Missing required values.
* Deployment configuration.

Never hard-code production values as a debugging fix.

---

# 42. Environment-Specific Defects

Compare:

```text
Development
Staging
Production
```

Differences may include:

* Database provider.
* Connection string.
* File paths.
* Case sensitivity.
* CORS.
* Reverse proxy.
* HTTPS.
* Secrets.
* Timezone.

Do not assume local behavior proves production behavior.

---

# 43. Case Sensitivity

Be aware of differences between Windows and Linux.

Check:

* File names.
* Paths.
* Configuration keys where relevant.
* Database collation.

Do not rely on case-insensitive local environments.

---

# 44. Deployment Defects

Inspect:

* Build output.
* Runtime version.
* Environment variables.
* Database migration state.
* Ports.
* Reverse proxy.
* Health check.
* Startup logs.

Separate application defects from deployment/configuration defects.

---

# 45. Package Problems

If a package causes failure:

Check:

* Version compatibility.
* Transitive dependencies.
* .NET version.
* Breaking changes.

Do not upgrade multiple unrelated packages simultaneously unless required.

---

# 46. Failing Tests

A failing test may indicate:

```text
Implementation defect
Test defect
Requirement change
Environment defect
```

Do not automatically modify tests to make them green.

First determine which expectation is correct.

---

# 47. Regression Test

Whenever practical, add a test that fails before the fix and passes after the fix.

Preferred sequence:

```text
Reproduce with Test
      ↓
Test FAIL
      ↓
Apply Fix
      ↓
Test PASS
```

This is especially valuable for recurring defects.

---

# 48. Existing Test Preservation

Do not weaken existing assertions to accommodate a broken implementation.

Update tests only when expected behavior intentionally changes.

---

# 49. Logging During Investigation

Temporary diagnostic logging may be added when required.

After root cause is understood:

* Remove noisy temporary logging.
* Keep only operationally valuable logs.

Never log sensitive values.

---

# 50. Debug Code

Do not leave behind:

```text
Console.WriteLine
temporary flags
test credentials
debug endpoints
hard-coded IDs
temporary bypasses
```

unless intentionally part of the implementation.

---

# 51. Security During Debugging

Never disable:

* Authentication.
* Authorization.
* Input validation.
* Certificate validation.
* SQL parameterization.

as a permanent fix.

Security controls must remain intact.

---

# 52. Performance Defects

For performance bugs:

Measure first.

Inspect:

* Query count.
* Query duration.
* CPU.
* Memory.
* External calls.
* Serialization.
* Payload size.
* Blocking operations.

Do not call general code cleanup a performance fix without evidence.

---

# 53. Memory Issues

Check for:

* Large collection materialization.
* Unbounded caching.
* Large file buffering.
* Static references.
* Improper singleton state.
* Missing disposal.

Do not force garbage collection as an application fix.

---

# 54. Resource Disposal

Ensure disposable resources follow correct ownership.

Prefer framework-managed lifetimes.

Do not dispose DI-managed services manually unless ownership semantics require it.

---

# 55. Duplicate Processing

For duplicate requests/jobs:

Investigate:

* Retry.
* Queue redelivery.
* User double-submit.
* Multiple workers.
* Missing idempotency.
* Transaction boundaries.

Do not rely only on frontend disabling a button.

---

# 56. Intermittent Defects

For intermittent issues, look for:

* Race conditions.
* Shared state.
* Timing assumptions.
* External dependency instability.
* Cache.
* Concurrency.
* Random data.
* Environment differences.

Avoid declaring success after one non-reproducing attempt.

---

# 57. Static and Global State

Inspect global mutable state when behavior differs across requests.

ASP.NET Core services should avoid shared mutable state without proper synchronization.

---

# 58. Cache Defects

Check:

* Key construction.
* Tenant/user scope.
* Expiration.
* Invalidation.
* Stale values.
* Serialization.

Do not disable caching permanently without understanding the consistency problem.

---

# 59. Tenant Data Defects

If multi-tenancy exists, verify:

* Tenant context.
* Query filters.
* Write scope.
* Cache keys.
* Background jobs.

Cross-tenant leakage is a critical defect.

---

# 60. Breaking Change Protection

A bug fix should not silently alter:

* Public API contracts.
* Database schema.
* Authentication flow.
* Authorization behavior.
* Existing valid business behavior.

If such change is necessary, report it explicitly.

---

# 61. Root Cause Categories

Use one or more categories in the final report:

```text
CODE_DEFECT
BUSINESS_RULE
DATA
CONFIGURATION
DATABASE
MIGRATION
SECURITY
INTEGRATION
CONCURRENCY
PERFORMANCE
ENVIRONMENT
DEPLOYMENT
DEPENDENCY
TEST
```

This helps future maintenance.

---

# 62. Confidence Level

When root cause is not fully verifiable, state confidence:

```text
HIGH
MEDIUM
LOW
```

Example:

```text
Root Cause Confidence: HIGH
```

Do not present assumptions as confirmed facts.

---

# 63. Debug Workflow

When this skill is activated, follow this sequence.

## Step 1 — Capture Symptom

Record:

```text
Expected
Actual
Error
```

---

## Step 2 — Classify

Classify the defect type.

---

## Step 3 — Inspect

Read relevant source, configuration and logs.

---

## Step 4 — Reproduce

Reproduce when possible.

---

## Step 5 — Trace

Trace execution to the first incorrect state.

---

## Step 6 — Identify Root Cause

State the actual cause clearly.

---

## Step 7 — Impact Analysis

Identify affected:

```text
Files
Modules
API
Database
Security
Frontend
Tests
```

---

## Step 8 — Design Minimal Fix

Choose the smallest safe correction.

---

## Step 9 — Implement

Modify only required code.

---

## Step 10 — Add Regression Coverage

Add/update relevant tests when practical.

---

## Step 11 — Build

Run:

```bash
dotnet build
```

when available.

---

## Step 12 — Test

Run relevant:

```bash
dotnet test
```

and targeted test suites.

---

## Step 13 — Reproduce Again

Repeat the original failing scenario if possible.

---

## Step 14 — Regression Review

Check nearby valid scenarios for unintended changes.

---

## Step 15 — Clean Up

Remove temporary diagnostic artifacts.

---

## Step 16 — Report

Produce the required CHANGE REPORT.

---

# 64. Debug Report Format

In addition to the global change report, include:

```text
## Debug Summary

Symptom:
...

Expected:
...

Actual:
...

Root Cause:
...

Root Cause Category:
...

Root Cause Confidence:
HIGH / MEDIUM / LOW

Fix:
...

Why This Fix:
...

Regression Risk:
LOW / MEDIUM / HIGH
```

---

# 65. Files Report

List every meaningful file changed.

Example:

```text
[MODIFIED]
src/Application/Users/GetUserHandler.cs

[MODIFIED]
tests/IntegrationTests/Users/GetUserTests.cs
```

Do not hide supporting changes.

---

# 66. Behavior Impact

Always report:

```text
Business Logic:
Changed / Unchanged

API Contract:
Changed / Unchanged

Database Schema:
Changed / Unchanged

Security:
Changed / Unchanged

Breaking Change:
Yes / No
```

---

# 67. Verification Report

Report exactly what ran:

```text
Build: PASS
Tests: PASS
Regression Scenario: PASS
Manual Verification: NOT RUN
```

Never claim verification that was not performed.

---

# 68. Unresolved Issues

If investigation reveals additional issues outside scope:

Report separately.

Example:

```text
Remaining Issue:
The module still performs an N+1 query.

Not fixed because:
It is unrelated to the reported authentication defect.

Recommendation:
Create a separate performance task.
```

Do not silently expand task scope.

---

# 69. Do Not Over-Refactor During Debugging

Avoid changing:

```text
Folder structure
Naming across module
Architecture pattern
Unrelated services
```

while fixing a localized defect.

Use the dedicated refactoring skill for broader cleanup.

---

# 70. Do Not Hide Failure

If the defect cannot be reproduced or fixed confidently:

State:

```text
Status: NOT CONFIRMED
```

or:

```text
Status: PARTIALLY RESOLVED
```

Explain remaining uncertainty.

Do not claim completion for an unverified fix.

---

# 71. Escalation Rule

If root cause requires a major architectural change:

Do not perform it automatically.

First report:

* Root cause.
* Why local fix is insufficient.
* Required architecture change.
* Compatibility impact.
* Migration risk.
* Recommended next task.

Then use an appropriate architecture/refactor workflow.

---

# 72. Definition of Done

A debugging task is complete only when:

1. The symptom is understood.
2. Expected behavior is established.
3. Root cause is identified with reasonable confidence.
4. The fix addresses root cause.
5. Scope remains controlled.
6. Existing valid behavior is preserved.
7. Relevant build/tests are executed when possible.
8. Original scenario is rechecked when possible.
9. Temporary debug artifacts are removed.
10. Regression risk is evaluated.
11. Root cause and changes are reported clearly.

The objective is:

> Fix the cause, verify the behavior, and leave the codebase safer than before.
