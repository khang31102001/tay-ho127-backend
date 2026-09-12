---
name: build-api-endpoint
description: Add or change a single HTTP endpoint (route, request/response contract, validation wiring, status codes, permission) on top of business logic that already exists or needs only a trivial addition. Not for building a new business capability end-to-end — use build-backend-feature for that.
---

# Build Api Endpoint

## 1. Purpose

Use this skill when the task is scoped to the **HTTP contract layer**:

* Exposing an existing Application service method through a new route.
* Adding a query endpoint (list/filter/search) over data an Application service can already fetch, or needs a small, obvious query addition.
* Changing an endpoint's route, verb, status code, or request/response shape.
* Adding a new action route (`POST /api/v1/users/{id}/activate`) that maps to a small, self-contained new service method with no new entity/domain concept.
* Fixing an endpoint's contract to match `api-design.md` (wrong verb, missing pagination, entity leaking as response, etc.).

## 1.1 When NOT to Use This Skill

If satisfying the request requires **any** of the following, use `build-backend-feature` instead — this skill assumes they already exist:

* A new or changed domain entity/aggregate, or new business rules/state transitions on one.
* A new database table, column, or migration.
* New cross-module wiring (a new port + Host adapter).
* Multiple coordinated service methods representing a new capability, not a single action.

If unsure, ask: *"Does the Application/Domain/Infrastructure work already exist, and am I only adding/adjusting the door to it?"* If yes → this skill. If the door and the room behind it both need building → `build-backend-feature`.

---

# 2. Required Rules

```text
.claude/rules/api-design.md
.claude/rules/naming.md
.claude/rules/code-quality.md
.claude/rules/security.md
```

Every rule in `api-design.md` applies in full — this skill exists specifically to get the HTTP contract right.

---

# 3. This Project's Endpoint Shape — Follow Exactly

Grounded in `Api/UsersController.cs` and `Application/Users/*`. A new/changed endpoint should look like this, not like a generic ASP.NET Core tutorial:

```csharp
[ApiController]
[Route("api/v1/<resource>")]
public sealed class <Resource>Controller : ControllerBase
{
    private readonly I<Resource>Service _service;

    public <Resource>Controller(I<Resource>Service service) => _service = service;

    [HttpGet]
    [RequirePermission(<Module>Permissions.<Resource>View)]
    [ProducesResponseType<PagedResult<<Resource>Response>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<<Resource>Response>>> List(
        [FromQuery] PagedRequest request, CancellationToken cancellationToken)
        => Ok(await _service.ListAsync(request, cancellationToken));

    [HttpPost]
    [RequirePermission(<Module>Permissions.<Resource>Create)]
    [ProducesResponseType<<Resource>DetailsResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<<Resource>DetailsResponse>> Create(
        [FromBody] Create<Resource>Request request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
```

Non-negotiable conventions this project already enforces:

* **Controller is thin.** It binds, checks `[RequirePermission]`, calls exactly one Application service method, and shapes the HTTP response. No LINQ, no EF, no business branching in the controller body — if you're writing an `if` that isn't about HTTP status shaping, it belongs in the service.
* **No manual validation calls.** Any `Request` record gets an `IValidator<T>` in `Application/`; `ValidationActionFilter` runs it automatically before the action executes. Do not call `.Validate()`/`.ValidateAsync()` inside the controller or service — that duplicates the pipeline and can produce an inconsistent error shape.
* **No manual try/catch for expected errors.** Throw `NotFoundException`, `ConflictException`, `ForbiddenException`, `BusinessRuleValidationException`, or let `FluentValidation.ValidationException` propagate — `GlobalExceptionHandler` converts them to the right `ProblemDetails` status/shape. A controller-level `try/catch` that manufactures its own error response is a deviation.
* **Every endpoint that isn't intentionally public carries `[RequirePermission(<Module>Permissions.X)]`.** Add the new permission code to that module's `<Module>Permissions` static class (mirrors `IdentityPermissions`) and to its seeded catalog (`AdminPlatform.Migrator/Seeding/PermissionCatalog.cs`) — an endpoint with no permission code registered anywhere is a security gap, not just an API-design nit.
* **Route**: `api/v1/<plural-resource>`, kebab-case for multi-word action segments (`reset-password`), resource-oriented action routes for non-CRUD operations (`POST /api/v1/users/{id}/reset-password`), never `?action=`.
* **DTOs are records** in `Application/<Feature>/<Feature>Contracts.cs` (see `UserContracts.cs`) — `Create<X>Request`, `Update<X>Request`, `<X>Response` (list/summary shape), `<X>DetailsResponse` (single-resource shape). Reuse the existing split between summary and details responses rather than inventing a third shape.
* **Status codes**: `200` for reads/updates, `201` + `CreatedAtAction` for creation, `204` for delete/actions with no meaningful response body, following `UsersController.ResetPassword`'s pattern for action endpoints.
* **Pagination**: any list endpoint takes `[FromQuery] PagedRequest` and returns `PagedResult<T>` via `ToPagedResultAsync` — never a bare `List<T>`.

---

# 4. Workflow

## Step 1 — Confirm Scope (§1.1)

State explicitly what already exists (service method, entity, persistence) and what this task adds (route + contract only, or route + one small service method). If the answer reveals new domain/persistence work, stop and redirect to `build-backend-feature`.

## Step 2 — Inspect the Target Module

Read the module's existing `Api/*Controller.cs`, `Application/<Feature>/*Contracts.cs`, and `<Module>Permissions.cs` before writing anything — match its exact conventions, don't introduce a new style for one endpoint.

## Step 3 — Define/Adjust the Contract

Add or change the `Request`/`Response` record(s) in `<Feature>Contracts.cs`. Keep request and response types distinct per operation (api-design.md §12) — do not reuse one DTO for both directions.

## Step 4 — Add/Adjust Validation

Add an `IValidator<T>` in `<Feature>Validators.cs` for any new/changed request record, matching the existing rule style (`RuleFor(x => x.Field).NotEmpty().MaximumLength(...)`).

## Step 5 — Wire the Service Call

If the Application service method already exists, call it. If it needs a small, obvious addition (e.g., a new overload or a one-line new method that only orchestrates existing persistence — no new entity/rules), add it here; anything larger goes back to `build-backend-feature`.

## Step 6 — Add the Controller Action

Follow §3's shape exactly: route, `[RequirePermission]`, `[ProducesResponseType]`, single call to the service, correct status code.

## Step 7 — Register the Permission (if new)

Add the code to `<Module>Permissions.All` and to the Migrator's `PermissionCatalog` seed list — an endpoint guarded by a permission code that is never seeded will `403` for everyone until seeded.

## Step 8 — Build and Verify

```bash
dotnet build
dotnet test --filter FullyQualifiedName~<Feature>
```

Manually sanity-check (or write a targeted test) for: success path, validation failure (`400`), unauthenticated (`401`), unauthorized/missing permission (`403`), not-found where relevant (`404`).

## Step 9 — Report

Use the API Change Report format from `api-design.md` §67:

```text
Endpoints Added / Modified / Removed:
...

Request Contract Changes:
...

Response Contract Changes:
...

Status Code Changes:
...

Authorization Changes:
Permission code: <code> (new/existing), seeded: YES/NO

Breaking Changes:
YES/NO
```

If nothing changed the contract, state `API Contract: No change` explicitly.

---

# 5. Do Not

* Do not build a new entity, migration, or cross-module port under this skill — that's `build-backend-feature`.
* Do not put business rules, EF queries, or branching logic in the controller.
* Do not call a validator manually or add a controller-level `try/catch` for an error type `GlobalExceptionHandler` already maps.
* Do not add an endpoint without a `[RequirePermission]` unless it is deliberately public (state that explicitly in the report).
* Do not invent a new response-wrapper shape (`{ success, data }`) — this project uses direct REST responses (api-design.md §14).

---

# 6. Definition of Done

1. Scope was confirmed to be contract-only (or contract + trivial service addition) before starting.
2. Route, verb, and status codes match `api-design.md` and this module's existing controllers.
3. Request/response are distinct explicit DTOs, not entities.
4. Validation runs through the existing `IValidator<T>` + `ValidationActionFilter` pipeline.
5. `[RequirePermission]` is present and the permission code is seeded, or the endpoint's public status is explicit.
6. `dotnet build` and relevant tests pass.
7. The API Change Report is filled in, including "No change" when applicable.
