---
name: optimize-performance
description: Diagnose and fix measured ASP.NET Core / EF Core / PostgreSQL performance problems — query shape, N+1, pagination, indexing, async, and payload size — without speculative optimization or architecture rewrites.
---

# Optimize Performance

## 1. Purpose

Use this skill when the task requires:

* Investigating a slow endpoint or background operation.
* Reducing database round trips or query cost.
* Reviewing/fixing N+1 query patterns.
* Sizing or reviewing indexes for a real query pattern.
* Reducing response payload size or serialization cost.
* Verifying async/EF usage does not block threads unnecessarily.
* Validating a performance fix with measurement, not assumption.

The objective is:

> Measure first, fix the actual bottleneck, and verify the fix with evidence — not general code cleanup labeled as optimization.

Do not use this skill to justify unrelated refactoring, premature caching, or infrastructure additions (Redis, background workers, CDN) without a demonstrated need.

---

# 2. Required Rules

Always follow:

```text
.claude/rules/architecture.md
.claude/rules/code-quality.md
.claude/rules/database.md
.claude/rules/api-design.md
.claude/rules/security.md
```

Performance work must not weaken authorization, validation, or change response contracts (api-design.md §38–39) unless explicitly in scope.

---

# 3. This Project's Performance-Relevant Facts

* **Database**: PostgreSQL via Npgsql, one schema per module, `UseSnakeCaseNamingConvention()`. Every module registers its own `DbContext` (`services.AddDbContext<...DbContext>`) — connection pooling and query plans are per-module, not shared.
* **No repository layer**: Application services query `I<Module>DbContext.Users` (a `DbSet<T>`) directly with LINQ. Performance problems live in these LINQ queries, not in a hidden data-access layer.
* **Pagination**: `PagedRequest`/`PagedResult` + `QueryableExtensions.ToPagedResultAsync` (`AdminPlatform.Common/Pagination/`) is the existing, standard mechanism. Any new list endpoint that doesn't use it is both an API-design and a performance problem — flag/fix both together.
* **Projection pattern already in use**: see `UserService.ListAsync` — the query is projected to the response record (`Select(u => new UserResponse(...))`) *before* `ToPagedResultAsync`, and uses `.AsNoTracking()` implicitly via `AsQueryable()` on the read path. Match this pattern; don't introduce `.ToList()` then map in memory.
* **Audit interceptors** (`AuditableEntitySaveChangesInterceptor`, `AuditLogSinkInterceptor`) run on every `SaveChangesAsync`. Batching multiple entity changes into one `SaveChangesAsync` call (rather than one per entity) reduces interceptor overhead as well as round trips.
* **Optimistic concurrency** uses Postgres `xmin` (`AuditableEntity.RowVersion`, `IsRowVersion()`) — no extra read needed to check a version column.
* **Cross-module reads** go through Host-registered port/adapters (`CrossModuleAdapters/`), which are real HTTP-free in-process calls but still separate service calls — a query pattern that fans out to several cross-module adapters per item in a loop is the modular-monolith equivalent of N+1 and should be batched at the adapter/query level, not called per-row.

---

# 4. Core Principle: Measure Before Changing

```text
Reported Slowness
     ↓
Reproduce / Isolate
     ↓
Measure (query count, duration, payload size)
     ↓
Identify Actual Bottleneck
     ↓
Targeted Fix
     ↓
Re-measure
     ↓
Report Before/After
```

Never "optimize" based on code appearance alone. A LINQ query that looks inefficient but runs once on 20 rows is not a priority; a query that looks fine but runs per-row in a loop is.

---

# 5. How to Measure in This Stack

* **Query count/shape**: enable EF Core logging (`options.LogTo(...)` or `ILoggerFactory` sink) locally, or inspect `dotnet-trace`/`dotnet-counters` if already wired. In its absence, add temporary `LogTo(Console.WriteLine, LogLevel.Information)` scoped to the investigation — remove it before completion (`debug-backend` §50 applies here too).
* **Wall-clock**: `Stopwatch` around the suspect code path in a throwaway test/benchmark, or existing `ILogger` timing if already present. Do not leave permanent timing instrumentation unless the project already has an observability convention for it.
* **Generated SQL**: `ToQueryString()` on an `IQueryable<T>` before execution — cheap way to confirm projection/filter pushdown without running EF logging.
* **Postgres-side**: `EXPLAIN ANALYZE` on the generated SQL when index behavior is in question. Only recommend an index when a real query's plan shows a sequential scan on a large/growing table — not by inspecting the schema in isolation.

Report actual numbers. "Reduced query count from 51 to 2" is a finding; "should be faster now" is not.

---

# 6. N+1 Detection and Fixes

Classic N+1 in this codebase's shape:

```csharp
// BAD — one query for users, then N queries for roles
var users = await _db.Users.ToListAsync(ct);
foreach (var user in users)
{
    var roles = await _roleService.GetRolesForUserAsync(user.Id, ct); // N round trips
}
```

Fix by projecting/joining once, or batching the second call:

```csharp
// GOOD — single query with the data already needed
var users = await _db.Users
    .Select(u => new UserResponse(u.Id, u.Email, u.FullName, u.IsActive, u.CreatedAtUtc))
    .ToListAsync(ct);

// GOOD — batch a cross-module/adapter lookup by ids instead of per-row
var userIds = users.Select(u => u.Id).ToList();
var rolesByUser = await _rolePermissionQueryService.GetRolesForUsersAsync(userIds, ct);
```

If the second call is a cross-module port (`I...QueryService`), the fix is adding a batched method to that port's contract (`GetXForIds(IEnumerable<Guid> ids)`), implemented once in the providing module — not looping the existing single-id method. This is an Application-layer contract change; keep it additive (new method) so it isn't a breaking change to the port.

---

# 7. Query-Level Checklist

* Filter and project *before* materializing (`Where`/`Select` stay in the `IQueryable`, no `.ToList()` followed by in-memory `.Where()`).
* List endpoints paginate via `PagedRequest`/`ToPagedResultAsync` — never return an unbounded `List<T>`.
* Read-only queries don't need change tracking; the existing `UserService` pattern (`AsQueryable()` off a `DbSet` used only for reads) already avoids unnecessary tracking overhead — don't add `.AsNoTracking()` redundantly where the query is never attached in the first place, but do add it explicitly if a read query is built from a tracked context in a write-heavy service.
* Avoid materializing more columns than the response DTO needs — project directly into the response record, not into the entity then mapped in memory (extra column fetch + extra allocation).
* Sorting uses an explicit allow-listed `switch` over `SortBy` (already the pattern in `UserService.ListAsync`) — this is also a performance control, since it prevents an unindexed/arbitrary `OrderBy` column from being requested.
* Avoid `Include()` chains that pull large owned collections when the endpoint only needs summary fields — prefer projection.
* Search filters (`EF.Functions.ILike`) on unindexed large text columns are a common slow-query source — check `EXPLAIN ANALYZE` before recommending an index, since a small table doesn't need one yet.

---

# 8. SaveChanges and Write-Path Checklist

* Multiple related entity changes go through one `SaveChangesAsync` call, not one per entity — batches the audit interceptors and the round trip.
* No `SaveChangesAsync` call inside a loop over user-submitted collections without considering batch size limits (see `api-design.md` §48 on bulk operations).
* Bulk imports/updates define a max batch size explicitly; do not accept unbounded payloads.

---

# 9. Async / Threading Checklist

* No `.Result`, `.Wait()`, or `async void` in request-handling code paths (Controllers, Application services).
* `CancellationToken` is threaded from the controller action through to `SaveChangesAsync`/EF query execution — already the established pattern (`CancellationToken cancellationToken` parameter throughout `UserService`); a new method missing it is a regression, not just a style nit.
* No `Task.Run` used to fire-and-forget request-scoped work inside an HTTP request — this both loses `DbContext` scoping (which is request-scoped/scoped-lifetime) and drops exceptions silently.

---

# 10. Response / Payload Checklist

* Response DTOs return only fields the consumer needs (also a data-exposure concern — cross-reference `review-security` if a large unused field is also sensitive).
* Large collections are always paginated; consider whether a "summary" response record (fewer fields) is more appropriate than "details" for list endpoints — this project already separates `UserResponse` (list) from `UserDetailsResponse` (single-resource) for this reason. Follow that pattern rather than returning the details shape from a list endpoint.

---

# 11. Indexing

* Recommend an index only when:
  1. A real query filters/sorts/joins on that column, confirmed via `EXPLAIN ANALYZE` or query shape review, and
  2. The table is large enough, or growing toward large enough, for a sequential scan to matter.
* `UserConfiguration.HasIndex(u => u.Email).IsUnique()` is the existing pattern for both uniqueness and lookup performance — mirror it for new modules' natural-key lookups.
* New indexes are added through an EF Core migration like any other schema change (see `design-database`), never applied by hand against a live database.
* Do not add an index for every filterable column speculatively — each index has a write-cost trade-off.

---

# 12. Caching

* This project has no caching layer today. Do not introduce `IMemoryCache`, output caching, or Redis as a "performance improvement" unless:
  1. Measurement shows the same expensive read repeating for the same input within a request/short window, and
  2. Staleness is acceptable for that data, and
  3. The user has agreed to the added invalidation complexity.
* If introduced, keep it minimal and scoped to the one proven hot path — not a general caching framework.

---

# 13. Workflow

## Step 1 — Capture the Complaint
Record the reported symptom: which endpoint/operation, what latency/load, what "slow" means concretely.

## Step 2 — Reproduce and Measure
Use §5. Get a baseline number before touching code.

## Step 3 — Identify Bottleneck Category
Query count (N+1), query shape (missing filter pushdown, unindexed scan), payload size, write-path (SaveChanges overhead), or async/threading.

## Step 4 — Design Targeted Fix
Smallest change that addresses the measured bottleneck — prefer the patterns in §6–§11 already established in this codebase over inventing new ones.

## Step 5 — Implement
Modify only the affected query/service/endpoint.

## Step 6 — Re-measure
Confirm the same metric captured in Step 2 actually improved.

## Step 7 — Build and Test
```bash
dotnet build
dotnet test
```

## Step 8 — Regression Check
Confirm response contract, pagination shape, and authorization are unchanged (cross-reference `api-design.md` §38 and `review-security` if authorization logic was anywhere near the touched code).

## Step 9 — Report

```text
## Performance Summary

Symptom:
...

Bottleneck:
N+1 / Unindexed Scan / Unbounded Payload / Write-Path Overhead / Blocking Async

Before:
Query count: X, Duration: Yms

After:
Query count: X, Duration: Yms

Fix:
...

Regression Risk:
LOW / MEDIUM / HIGH

Verification:
Build: PASS/FAIL
Tests: PASS/FAIL
Measurement Method: <how before/after numbers were obtained>
```

---

# 14. Do Not

* Do not report "should be faster" without a before/after number.
* Do not add caching, background jobs, or a new NuGet dependency as a first response to a slow endpoint.
* Do not change API contracts (response shape, pagination behavior) to "fix" performance without calling it out as a contract change.
* Do not recommend indexes without query evidence.
* Do not mix performance work with unrelated refactoring — if the query needs restructuring beyond the performance fix, hand that off to `refactor-backend`.

---

# 15. Definition of Done

A performance task is complete only when:

1. The bottleneck was measured, not assumed.
2. The fix targets the actual measured cause.
3. Before/after numbers are reported.
4. API contract and authorization are unchanged unless explicitly in scope.
5. Build and relevant tests pass.
6. No speculative infrastructure was introduced.
