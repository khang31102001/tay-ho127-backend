---
name: review-security
description: Perform a focused security audit of ASP.NET Core backend code — authentication, authorization, input handling, data exposure, and infrastructure hygiene — and report prioritized, evidence-based findings without weakening controls.
---

# Review Security

## 1. Purpose

Use this skill when the task requires:

* A dedicated security audit of a module, endpoint, or the whole backend.
* Reviewing authentication/authorization changes before merge.
* Investigating a suspected vulnerability (IDOR, injection, data leak, broken access control).
* Verifying permission coverage on new or existing endpoints.
* Checking secret handling, logging hygiene, or dependency vulnerabilities.
* A pre-release or pre-deployment security gate.

This skill produces findings and, when explicitly asked, fixes them. It does not silently patch security behavior as a side effect of another task — that belongs to whichever skill triggered the change (`build-backend-feature`, `debug-backend`, `refactor-backend`), which must consult this skill's checklist rather than invent its own.

> Assist with authorized defensive review of this codebase. Do not produce exploit payloads or attack tooling beyond what is needed to demonstrate a finding to the owning team.

---

# 2. Required Rules

Always evaluate against:

```text
.claude/rules/security.md
.claude/rules/api-design.md
.claude/rules/architecture.md
.claude/rules/database.md
.claude/rules/naming.md
```

`security.md`'s four pillars anchor this review: never commit secrets, validate/authorize at server boundaries, least privilege, no leaked internals.

---

# 3. This Project's Security Model — Read Before Reviewing

Do not assume a generic ASP.NET Core security stack. This repository has a specific, already-implemented model. Verify it is used correctly rather than re-deriving it:

* **AuthN**: JWT bearer tokens issued by `AuthService` (Identity module) via `IJwtTokenService`/`JwtTokenService`. Permission codes and working-context (current brand/fiscal year) are embedded as claims at login/refresh — see [`docs/ARCHITECTURE.md`](../../docs/ARCHITECTURE.md) and `AdminPlatform.Common/Security/JwtOptions.cs`.
* **AuthZ**: dynamic permission policies, not a fixed enum. `[RequirePermission("users.create")]` (`AdminPlatform.Common/Security/PermissionAuthorization.cs`) expands to `Authorize(Policy = "Permission:{code}")`; `PermissionPolicyProvider` builds the policy on the fly and `PermissionAuthorizationHandler` checks the caller's `permission` claims (`AppClaimTypes.Permission`). There is no `[Authorize(Roles = ...)]` anywhere — a hard-coded role check is a deviation, not a stylistic choice.
* **Validation**: FluentValidation validators run automatically for every action argument with a registered `IValidator<T>`, via `ValidationActionFilter` (global action filter) — controllers never call `.Validate()` manually. A failure throws `FluentValidation.ValidationException`.
* **Error surface**: `GlobalExceptionHandler` (`IExceptionHandler`) is the *only* place exceptions become HTTP responses. It maps a fixed exception set (`NotFoundException`, `ConflictException`, `ForbiddenException`, `AuthenticationFailedException`, `BusinessRuleValidationException`, `ValidationException`, Postgres `23503`/`23505`, `DbUpdateConcurrencyException`) to `ProblemDetails`; everything else becomes a generic `500` with no exception detail. A `catch` block anywhere that returns exception internals to the client bypasses this and is a finding.
* **Tenancy/scope**: no row-level multi-tenant filter exists. Instead, users have an optional `CurrentBrandId`/`CurrentFiscalYearId` "working context" (`User.SetWorkingContext`), validated via `IUserScopeValidator` before being set. Any endpoint that reads/writes brand- or fiscal-year-scoped data must check scope explicitly — there is no global query filter doing it silently. Treat missing scope checks on scoped resources as the closest equivalent of cross-tenant leakage in this codebase.
* **Persistence boundary**: no generic repository. Application code calls `I<Module>DbContext` directly. This means authorization must happen in the Application service (or the controller's `[RequirePermission]`) — there is no repository-layer interceptor to fall back on.
* **Cross-module data**: modules never reference each other's projects. A module needing another module's data goes through a Host-registered port/adapter (`Host/AdminPlatform.Api/CrossModuleAdapters/`). Review any place that seems to reach into another module's internals — it should not compile, but check `CrossModuleAdapters` for adapters that over-expose data across a trust boundary (e.g., returning more than the consuming module needs).

---

# 4. Review Modes

## Mode A — Full Backend Security Audit

Cover every category in §6 across all modules. Use for periodic or pre-release audits.

## Mode B — Endpoint / Feature Audit

Scope to one controller or one vertical slice (e.g., a new feature built with `build-backend-feature`). Still check the full checklist, but only for that surface.

## Mode C — Incident / Suspected Vulnerability

Start from the reported symptom (e.g., "user A can see user B's data"). Reproduce the access path, trace it through `Api → Application → I<Module>DbContext`, and confirm or refute with evidence before recommending a fix.

## Mode D — Dependency / Infrastructure Check

Run `dotnet list package --vulnerable` (report actual output only), check `appsettings*.json` / Docker / CI for secret handling, and review `Program.cs` middleware order.

---

# 5. Checklist — Authentication

* Every controller that should require a caller is either `[Authorize]`-scoped or carries `[RequirePermission]` (which already requires authentication). Flag any controller/action with no auth attribute that isn't an intentional public endpoint (login, refresh, health check).
* JWT validation parameters (issuer, audience, signing key, lifetime, clock skew) are read from `JwtOptions`/configuration, never hard-coded.
* Refresh-token flow: confirm rotation/revocation behavior in `AuthService` — a stolen refresh token should not grant indefinite access.
* Password handling goes through `IPasswordHasher` (`PasswordHasherAdapter`) only. Flag any custom hashing, plaintext comparison, or password logging.
* No endpoint or code path bypasses authentication "for testing" or "for the frontend" left in place.

---

# 6. Checklist — Authorization

* Every mutating and every sensitive-read endpoint carries `[RequirePermission(...)]` with a permission code that actually exists in that module's `<Module>Permissions` static class and is seeded (`AdminPlatform.Migrator/Seeding/PermissionCatalog.cs`).
* Permission codes are checked, not just present as decoration — verify the code passed to `[RequirePermission]` matches the operation's real risk (e.g., a delete endpoint should not accept a `*.view` permission).
* No endpoint relies on the UI to hide an action as its only protection — the API must independently enforce it.
* Ownership/scope checks (working-context brand/fiscal year, or any "does this resource belong to the caller" check) happen in the Application service, using data already loaded from `I<Module>DbContext` or `ICurrentUser` — not inferred from client-supplied request fields alone (an attacker can set any `id` in a request body).
* IDOR check: for any `GET/PUT/DELETE .../{id}`, confirm the service verifies the caller may access that specific `id`'s resource, not merely that the resource exists.
* No hard-coded admin bypass (`if (user.Email == "admin@...")`, `if (isDevelopment) skip auth`) outside of documented, intentional design.

---

# 7. Checklist — Input Handling & Injection

* All EF Core queries use LINQ/parameterized APIs. Flag any raw SQL built via string concatenation or interpolation with user input; `FromSqlInterpolated`/parameterized `FromSqlRaw` is acceptable, string-built `FromSqlRaw` is not.
* Every `Request` DTO bound from the client that can affect business state has a registered `IValidator<T>` — an unvalidated request record is a finding.
* Distinguish input validation (`CreateUserRequestValidator`-style, in `Application/`) from business-rule validation (domain methods, e.g. `User.Create`'s `Guard.NotNullOrWhiteSpace`) — both should exist for meaningful fields; input validation alone is not enough for a business invariant.
* Search fields (`ILike`/`Contains` patterns) are parameterized through EF, not string-concatenated into SQL.
* Sort/filter parameters exposed via query string (`request.SortBy`) are matched against an explicit allow-list `switch`, never used to build a dynamic column name.
* If file upload exists anywhere: verify content-type/size limits, and that stored file names are never taken directly from client input (path traversal risk).

---

# 8. Checklist — Data Exposure

* No controller/service returns an EF Core entity (`User`, `Order`, etc.) directly from an endpoint — only `*Response` records. Check the actual return type of every action, not just its declared `ProducesResponseType`.
* Response DTOs never carry `PasswordHash`, refresh-token secrets, security stamps, or other module's internal ids that the caller has no reason to see.
* `GlobalExceptionHandler`'s `500` path is the only place a generic message reaches the client — confirm no other exception handler or `catch` block writes `ex.Message`, `ex.StackTrace`, or a raw DB error to the response body.
* Logging (`ILogger<T>`) never logs a password, token, connection string, or full PII payload. Structured log arguments should reference an id, not an entire request object, when that object contains secrets.
* Swagger/OpenAPI does not document internal-only or diagnostic endpoints as if they were public contract.

---

# 9. Checklist — Secrets & Configuration

* No secret (connection string, JWT signing key, API key) is committed in `appsettings.json`, `appsettings.Development.json`, or source. `appsettings.*.json` should reference environment variables / user-secrets / a secret store; only non-sensitive defaults belong in committed config.
* `Program.cs` and module `Add<Module>Module` methods read secrets via `IConfiguration`/`IOptions<T>`, never as inline literals.
* Docker/CI files (`docker-compose.yml`, `.github/workflows/*`) do not bake real credentials into the image or pipeline logs.
* `.gitignore` covers local secret files (`appsettings.*.local.json`, `.env`) if any are used.

---

# 10. Checklist — Database & Infrastructure

* `PostgresException` unique/FK violations (`23503`/`23505`) are mapped generically by `GlobalExceptionHandler` — no module should catch and re-expose the raw constraint name or table name to the client.
* Migrations that add cross-module foreign keys use the raw-SQL pattern (`migrationBuilder.Sql("ALTER TABLE ... ADD CONSTRAINT ...")`) documented in `docs/ARCHITECTURE.md` — a module should never gain a compile-time reference to another module's entities just to add a constraint.
* `RowVersion`/`xmin`-based optimistic concurrency (`AuditableEntity`) is in place for entities where concurrent conflicting writes would be a business risk; a missing concurrency check on a financially or permission-sensitive entity is worth flagging.
* Audit interceptors (`AuditableEntitySaveChangesInterceptor`, `AuditLogSinkInterceptor`) are registered for every module's `DbContext` — a module that opts out silently loses audit trail.

---

# 11. Checklist — Rate Limiting & Abuse

* Login, refresh-token, and password-reset endpoints are the highest-value rate-limiting targets — check whether any throttling exists; flag its absence as a recommendation (P1/P2, not automatically P0, unless brute-force risk is concretely elevated).
* No endpoint allows unbounded collection retrieval — list endpoints must go through `PagedRequest`/`PagedResult` with a bounded max page size.

---

# 12. Severity & Reporting

Use the same finding format as `review-backend` for consistency:

```text
ID:
SEC-01

Category:
AUTHORIZATION / AUTHENTICATION / INPUT_VALIDATION / DATA_EXPOSURE / SECRETS / DATABASE / RATE_LIMITING

Severity:
CRITICAL / HIGH / MEDIUM / LOW / INFO

Location:
UsersController.ResetPassword

Problem:
...

Impact:
...

Recommendation:
...

Confidence:
HIGH / MEDIUM / LOW
```

Severity guide specific to this project:

```text
CRITICAL — missing [RequirePermission] on a mutating endpoint; IDOR on scoped resource; secret committed to source; auth bypass.
HIGH      — entity returned directly from an endpoint; raw SQL built from user input; password/token in logs.
MEDIUM    — missing rate limiting on auth endpoints; overly broad permission code for a sensitive action; missing optimistic concurrency on a risk-relevant entity.
LOW       — inconsistent error message specificity; missing OpenAPI auth documentation.
```

---

# 13. Do Not

* Do not weaken a control to "fix" a false positive — if a finding turns out to be intentional design (e.g., a public health-check endpoint with no auth), mark it `NOT AN ISSUE` with reasoning, don't touch the code.
* Do not invent a new authorization mechanism (custom middleware, ad hoc claims check) when `[RequirePermission]` already covers the case.
* Do not fix findings outside the requested scope without calling them out separately first — use the same out-of-scope escalation pattern as `review-backend` §74.
* Do not claim a codebase is "secure" or "production ready" from source review alone. State precisely what was and was not checked (e.g., "no dynamic penetration testing was performed").

---

# 14. Workflow

1. **Scope** — Mode A/B/C/D per §4.
2. **Re-derive the security model** from §3 for the area under review (don't assume; confirm the actual attributes/services present).
3. **Walk the checklist** (§5–§11) against the real code, not from memory of generic ASP.NET Core patterns.
4. **Trace suspicious paths** end-to-end: `HTTP → [RequirePermission] → Controller → Application Service → I<Module>DbContext → Database`, and for anything scoped, confirm where the scope check actually happens.
5. **Classify and prioritize findings** (§12).
6. **Report** — findings first, then an explicit statement of what was NOT verified (penetration testing, load-based abuse testing, secret-scanning of full git history).
7. If asked to fix: fix CRITICAL/HIGH first, preserve all other observable behavior (routes, contracts, unrelated business logic), and re-verify with `dotnet build` / targeted tests.

---

# 15. Definition of Done

A security review is complete only when:

1. The actual authn/authz/validation/error-handling mechanisms in use were confirmed against source, not assumed.
2. Findings are evidence-based, each pointing to a real file/line.
3. Findings are prioritized by severity, not just listed.
4. No control was weakened.
5. Anything fixed was verified with `dotnet build` and relevant tests.
6. The report states what was and was not covered, without overclaiming.
