# API Design Rules

## 1. Purpose

These rules define the REST API design standards for this ASP.NET Core backend.

They apply whenever APIs are:

* Created.
* Modified.
* Refactored.
* Extended.
* Versioned.
* Integrated with frontend or external systems.

The goal is to keep APIs:

* Predictable.
* Consistent.
* Easy to consume.
* Backward compatible.
* Secure.
* Maintainable.
* Scalable over time.

Do not introduce API complexity without a concrete requirement.

---

# 2. API Design Principles

Prefer:

* Resource-oriented APIs.
* Consistent HTTP semantics.
* Explicit contracts.
* Stable response shapes.
* Clear validation behavior.
* Backward compatibility.
* Small and focused endpoints.

The API should describe business capabilities rather than internal implementation.

---

# 3. Base Route Convention

Use lowercase resource-oriented routes.

Preferred:

```text
/api/users
/api/orders
/api/products
/api/roles
```

Avoid:

```text
/api/GetUsers
/api/UserManagement
/api/ProcessOrder
/api/do-something
```

Routes should represent resources or meaningful business operations.

---

# 4. Resource Naming

Use plural nouns for resource collections.

Preferred:

```text
/users
/orders
/products
/permissions
```

Avoid:

```text
/user
/order
/product-list
/get-users
```

Maintain consistent terminology across API, code and documentation.

---

# 5. HTTP Method Semantics

Use HTTP methods consistently.

```text
GET     → Read
POST    → Create / Execute non-idempotent action
PUT     → Replace full resource
PATCH   → Partial update
DELETE  → Delete / deactivate where contract defines deletion
```

Examples:

```text
GET    /api/users
GET    /api/users/{id}
POST   /api/users
PUT    /api/users/{id}
PATCH  /api/users/{id}
DELETE /api/users/{id}
```

Do not use `POST` for every operation by default.

---

# 6. Resource Actions

When an operation represents a real business action that does not map cleanly to CRUD, use explicit action routes.

Examples:

```text
POST /api/orders/{id}/approve
POST /api/orders/{id}/cancel
POST /api/users/{id}/activate
POST /api/users/{id}/deactivate
POST /api/users/{id}/reset-password
```

Avoid:

```text
POST /api/orders/process
POST /api/users/action
```

Action names must represent business intent.

---

# 7. Nested Resources

Use nested routes only when the relationship improves API clarity.

Example:

```text
GET /api/users/{userId}/roles
POST /api/users/{userId}/roles
DELETE /api/users/{userId}/roles/{roleId}
```

Avoid excessively deep nesting:

```text
/api/organizations/{id}/departments/{id}/users/{id}/roles/{id}
```

Prefer shallow routes when nesting becomes difficult to maintain.

---

# 8. Route Parameters

Use route parameters for resource identity.

Example:

```text
GET /api/users/{id}
```

Use query parameters for:

* Filtering.
* Sorting.
* Searching.
* Pagination.
* Optional behavior.

Example:

```text
GET /api/users?page=1&pageSize=20&status=active
```

Do not overload routes with unnecessary path segments.

---

# 9. Route Naming

Use kebab-case for multi-word route segments.

Preferred:

```text
/reset-password
/email-verification
/workflow-status
```

Avoid:

```text
/resetPassword
/ResetPassword
/reset_password
```

Keep route naming stable after publication.

---

# 10. Request Models

Do not bind complex API requests directly to persistence entities.

Prefer explicit request contracts.

Example:

```csharp
public sealed record CreateUserRequest(
    string Email,
    string FullName,
    string Password);
```

Avoid:

```csharp
public async Task<IActionResult> Create(User entity)
```

Request models must represent API intent.

---

# 11. Response Models

Do not expose EF Core entities directly as API responses.

Use explicit response DTOs.

Example:

```csharp
public sealed record UserResponse(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive);
```

API contracts must remain independent from persistence implementation.

---

# 12. Request and Response Separation

Do not reuse the same DTO for all operations.

Prefer:

```text
CreateUserRequest
UpdateUserRequest
UserSummaryResponse
UserDetailsResponse
```

Avoid one generic:

```text
UserDto
```

for every request and response unless the structure is truly identical and stable.

---

# 13. Response Shape

Use one consistent API response strategy across the project.

If the project uses direct REST responses, prefer:

```json
{
  "id": "...",
  "name": "..."
}
```

For collections:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 120,
  "totalPages": 6
}
```

Do not arbitrarily mix response formats across endpoints.

---

# 14. Response Wrapper

Do not create unnecessary generic wrappers such as:

```json
{
  "success": true,
  "message": "Success",
  "data": {},
  "code": 200
}
```

for every successful response unless the project explicitly requires this contract.

HTTP already provides:

* Status code.
* Headers.
* Body.

Use wrappers only when they provide concrete system-wide value.

---

# 15. HTTP Status Codes

Use status codes semantically.

Common mappings:

```text
200 OK
201 Created
204 No Content
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
422 Unprocessable Entity
500 Internal Server Error
```

Do not return `200 OK` for every outcome.

---

# 16. Creation Response

Successful creation should generally return:

```text
201 Created
```

When appropriate, include the created resource or location.

Example:

```csharp
return CreatedAtAction(
    nameof(GetById),
    new { id = user.Id },
    response);
```

Do not return `201` when no resource was created.

---

# 17. Delete Response

Successful deletion may return:

```text
204 No Content
```

or another project-standard response when required.

Do not return fake response payloads solely to have content.

---

# 18. Validation Errors

Invalid request input should produce a predictable validation response.

Prefer a standardized structure compatible with `ProblemDetails`.

Example:

```json
{
  "type": "validation_error",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "email": [
      "Email is required."
    ]
  }
}
```

Do not create a different validation structure for each endpoint.

---

# 19. Problem Details

Prefer RFC-style `ProblemDetails` for API errors.

Relevant fields may include:

```text
type
title
status
detail
instance
traceId
errors
```

Example:

```json
{
  "type": "not_found",
  "title": "Resource not found",
  "status": 404,
  "detail": "User was not found.",
  "traceId": "..."
}
```

Do not expose internal stack traces to clients.

---

# 20. Business Errors

Map business errors to meaningful HTTP responses.

Typical mapping:

```text
Invalid input                 → 400
Unauthenticated               → 401
Unauthorized                  → 403
Resource missing              → 404
Business state conflict       → 409
Validation failure            → 400 or 422
Unexpected server error       → 500
```

Use one project-wide mapping strategy.

---

# 21. Error Messages

Client-facing error messages must be:

* Safe.
* Understandable.
* Non-sensitive.
* Stable where consumers depend on them.

Do not expose:

* Stack traces.
* SQL errors.
* Connection strings.
* Internal file paths.
* Secrets.
* Infrastructure details.

---

# 22. Error Codes

When frontend or external systems need machine-readable errors, use stable error codes.

Example:

```json
{
  "code": "USER_EMAIL_ALREADY_EXISTS",
  "title": "Email already exists",
  "status": 409
}
```

Error codes must represent business/application meaning.

Avoid arbitrary codes such as:

```text
ERR001
ERR002
E100
```

unless the project has a documented code registry.

---

# 23. Pagination

Any endpoint capable of returning a large collection must support pagination.

Recommended defaults:

```text
page = 1
pageSize = 20
```

Maximum page size should be bounded.

Example:

```text
GET /api/users?page=1&pageSize=20
```

Do not return unbounded datasets.

---

# 24. Pagination Response

Recommended structure:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 120,
  "totalPages": 6
}
```

Use a single pagination model across the API.

---

# 25. Filtering

Use query parameters for filtering.

Example:

```text
GET /api/orders?status=pending
GET /api/users?isActive=true
```

Do not create separate endpoints for every simple filter.

Avoid:

```text
/api/users/active
/api/users/inactive
/api/users/deleted
```

when query parameters are sufficient.

---

# 26. Searching

Use clear query parameters for search.

Example:

```text
GET /api/users?search=tony
```

Do not overload generic parameters such as:

```text
?q=
&x=
&keyword1=
```

unless an established external contract requires it.

---

# 27. Sorting

Use predictable sorting syntax.

Example:

```text
GET /api/users?sortBy=createdAt&sortDirection=desc
```

Only allow supported sortable fields.

Do not dynamically inject arbitrary client field names into SQL.

---

# 28. Date Filtering

Use explicit date parameter names.

Example:

```text
?fromDate=2026-01-01
&toDate=2026-01-31
```

Date semantics must be clear:

* Date only.
* UTC timestamp.
* Local business date.

Do not mix them implicitly.

---

# 29. JSON Naming

Use one JSON property naming strategy across the API.

For ASP.NET Core APIs, prefer:

```text
camelCase
```

Example:

```json
{
  "userId": "...",
  "fullName": "...",
  "createdAt": "..."
}
```

Do not mix:

```text
UserId
user_id
userId
```

within the same API.

---

# 30. Date and Time Responses

Use ISO 8601 compatible formats.

Prefer timestamps containing clear timezone semantics.

Example:

```text
2026-08-26T06:30:00Z
```

Persist and expose UTC timestamps by default unless business requirements require local-time semantics.

---

# 31. Enum Contracts

Do not expose numeric enum values by default when they reduce API clarity.

Prefer meaningful string representations when appropriate.

Example:

```json
{
  "status": "approved"
}
```

instead of:

```json
{
  "status": 3
}
```

If an existing API already exposes numeric enum values, do not change them without compatibility analysis.

---

# 32. Null Handling

API null semantics must be intentional.

Differentiate between:

```text
Missing property
Explicit null
Empty string
Empty collection
```

Do not return null collections when an empty collection better represents the contract.

Prefer:

```json
{
  "items": []
}
```

over:

```json
{
  "items": null
}
```

when there are simply no items.

---

# 33. Partial Updates

Use `PATCH` when partial update behavior is required.

Do not interpret omitted properties as null unless explicitly defined.

For simple systems, dedicated action requests may be clearer than implementing generic JSON Patch.

Do not introduce JSON Patch infrastructure without a concrete need.

---

# 34. PUT Semantics

Use `PUT` when replacing the full resource representation or when project convention clearly defines it as a full update.

Do not mix `PUT` and `PATCH` semantics unpredictably.

---

# 35. Idempotency

GET, PUT and DELETE should be designed to behave idempotently where HTTP semantics expect it.

For critical POST operations that may be retried, consider idempotency mechanisms when required.

Examples:

* Payments.
* Imports.
* External callbacks.
* Transaction creation.

Do not introduce idempotency infrastructure to every endpoint unnecessarily.

---

# 36. Authentication

Authentication determines:

> Who is calling?

Use established authentication mechanisms such as:

* JWT Bearer.
* Cookie authentication.
* External identity provider.

Do not implement custom authentication protocols without a concrete requirement.

Detailed authentication rules belong to `security.md`.

---

# 37. Authorization

Authorization determines:

> What is the caller allowed to do?

Prefer policy/permission-based authorization for scalable systems.

Avoid authorization checks scattered manually throughout controllers.

Example:

```csharp
[Authorize(Policy = Permissions.UsersCreate)]
```

or equivalent endpoint policy configuration.

Detailed authorization rules belong to `security.md`.

---

# 38. API Contract Stability

After an API contract is published, treat these as public contracts:

* Routes.
* HTTP methods.
* Request properties.
* Response properties.
* Property types.
* Status codes relied upon by clients.
* Error codes.
* Authentication requirements.

Do not casually modify them during refactoring.

---

# 39. Breaking Changes

Examples of breaking API changes include:

* Removing an endpoint.
* Renaming a route.
* Renaming response properties.
* Changing property types.
* Making optional fields required.
* Removing enum values.
* Changing authentication requirements.
* Changing important status-code behavior.

Breaking changes require explicit task scope and impact reporting.

---

# 40. Non-Breaking Evolution

Prefer additive changes.

Safer examples:

```text
Add optional response property
Add new endpoint
Add optional query parameter
Add new enum value when consumers tolerate unknown values
```

Evaluate actual consumers before assuming a change is safe.

---

# 41. API Versioning

Do not introduce API versioning before it is needed.

Introduce versioning when:

* Multiple incompatible contracts must coexist.
* External clients require stable legacy APIs.
* Breaking evolution cannot be coordinated.

Possible versioning strategy:

```text
/api/v1/users
/api/v2/users
```

Use one project-wide strategy.

Do not version individual endpoints inconsistently.

---

# 42. API Version Lifecycle

If multiple API versions exist:

* Document supported versions.
* Avoid duplicating business logic.
* Share application/domain behavior where possible.
* Keep version-specific differences at the API boundary.
* Define deprecation strategy.

Do not fork the entire backend solely because an API contract changes.

---

# 43. Controller Responsibilities

Controllers should remain thin.

Allowed responsibilities:

1. Receive HTTP request.
2. Bind request data.
3. Apply HTTP-level authorization.
4. Call application logic.
5. Convert application result to HTTP response.

Avoid:

* Complex business calculations.
* Database queries directly in controllers.
* Large mapping logic.
* Workflow orchestration.
* Duplicate validation.

---

# 44. Endpoint Responsibilities

When using Minimal APIs, the same principles apply.

Avoid large endpoint lambdas.

Prefer:

```text
Endpoint
   ↓
Application Handler / Service
   ↓
Domain / Infrastructure
```

Minimal API does not mean placing the entire application logic inside `MapPost()`.

---

# 45. API-to-Application Boundary

API models should be translated into application inputs when needed.

Example:

```text
HTTP Request
    ↓
CreateUserRequest
    ↓
CreateUserCommand / Use Case
    ↓
Application
```

Do not allow HTTP-specific types to spread through business layers.

Avoid dependencies on:

```text
HttpContext
IFormFile
IActionResult
ClaimsPrincipal
```

inside domain code.

---

# 46. File Upload APIs

File upload endpoints must define:

* Allowed file types.
* Maximum file size.
* Validation.
* Storage behavior.
* Authorization.
* Error handling.

Do not trust:

* File name.
* MIME type.
* File extension.

Avoid using client-provided file names directly as storage paths.

---

# 47. File Download APIs

File downloads must:

* Verify authorization.
* Validate requested resource ownership when relevant.
* Use appropriate content type.
* Avoid path traversal.
* Avoid exposing physical server paths.

---

# 48. Bulk Operations

Bulk APIs should be introduced when they provide clear value.

Example:

```text
POST /api/users/bulk-import
```

For bulk operations, define:

* Maximum batch size.
* Partial failure behavior.
* Transaction behavior.
* Validation result.
* Error reporting.

Do not accept unlimited payload sizes.

---

# 49. Long-Running Operations

Do not keep HTTP requests open unnecessarily for very long processing tasks.

For long-running operations, consider:

```text
Request
   ↓
Create Job
   ↓
202 Accepted
   ↓
Background Processing
   ↓
Job Status
```

Only introduce background job infrastructure when justified.

---

# 50. 202 Accepted

Use:

```text
202 Accepted
```

when a request is accepted but processing completes asynchronously.

Do not return `200 OK` pretending the operation has completed.

---

# 51. Concurrency

When concurrent update conflicts are realistic, consider optimistic concurrency.

Possible mechanisms:

* RowVersion.
* ETag.
* Version field.
* Database concurrency token.

Return an appropriate conflict response when updates collide.

Do not implement complex concurrency handling without an actual requirement.

---

# 52. Caching

API caching must not compromise correctness or authorization.

Consider:

* Response caching.
* Output caching.
* Application caching.

Only cache data when:

* Staleness is acceptable.
* Cache invalidation is understood.
* User-specific authorization is handled correctly.

Detailed performance decisions belong to optimization workflows.

---

# 53. Rate Limiting

Apply rate limiting when required for:

* Public endpoints.
* Authentication endpoints.
* Expensive operations.
* Abuse-sensitive APIs.

Do not apply arbitrary global limits without understanding normal traffic.

Security-related configuration belongs to `security.md`.

---

# 54. OpenAPI

Public API endpoints should be discoverable through OpenAPI where practical.

Descriptions should communicate:

* Endpoint purpose.
* Required parameters.
* Response types.
* Status codes.
* Authentication requirements.

Do not let generated OpenAPI expose internal-only endpoints unintentionally.

---

# 55. Endpoint Documentation

Document non-obvious behavior.

Especially:

* Business constraints.
* Required permissions.
* Pagination.
* Filter semantics.
* Date semantics.
* Idempotency behavior.
* Error codes.

Avoid documentation that simply repeats the method name.

---

# 56. API Naming Consistency

Use the same domain vocabulary across:

```text
Route
Request
Response
Application
Database
Documentation
```

If the business concept is:

```text
Organization
```

do not randomly alternate between:

```text
Organization
Company
Tenant
BusinessUnit
```

unless they represent different concepts.

---

# 57. Frontend Integration

When APIs are consumed by frontend applications, contracts should be easy to integrate.

Prefer:

* Predictable response properties.
* Consistent pagination.
* Consistent errors.
* Stable IDs.
* Explicit optional fields.
* ISO timestamps.

Do not design API response shapes specifically around one UI component when the API represents a reusable business capability.

---

# 58. API Contract Ownership

Backend owns the API contract.

Frontend and external consumers depend on it.

Therefore, before changing an existing contract:

1. Search for consumers if available.
2. Identify compatibility impact.
3. Prefer additive evolution.
4. Preserve old behavior when required.
5. Report the change explicitly.

---

# 59. No Database Leakage

API clients should not need to understand database implementation.

Do not expose names such as:

```text
TblUserId
FK_ROLE_ID
DbRowVersionInternal
```

unless they are intentional public concepts.

Persistence naming and API naming may differ.

---

# 60. Sensitive Data

Never return sensitive fields unless explicitly required.

Examples:

* Password hashes.
* Password salts.
* Refresh token secrets.
* Internal security stamps.
* Private keys.
* API keys.
* Unnecessary personal information.

Response DTOs must explicitly select what clients need.

---

# 61. Endpoint Scope

Each endpoint should perform one clear operation.

Avoid endpoints that behave differently based on arbitrary flags.

Avoid:

```text
POST /api/users?action=create
POST /api/users?action=delete
POST /api/users?action=reset
```

Prefer distinct operations.

---

# 62. Query Parameter Complexity

When a query becomes extremely complex, consider introducing a structured search endpoint or query object.

Do not create dozens of loosely defined parameters without clear semantics.

However, do not create POST-based search endpoints solely because a GET has several simple filters.

---

# 63. Duplicate Endpoints

Before creating a new endpoint:

1. Search existing routes.
2. Search existing use cases.
3. Check whether an existing endpoint can be extended safely.
4. Avoid duplicating business operations under different route names.

---

# 64. API Refactoring

During API refactoring:

* Internal implementation may change.
* Public behavior should remain stable.

Do not rename routes or contracts solely for style cleanup unless explicitly requested.

Refactoring should preserve:

* HTTP method.
* Route.
* Contract.
* Behavior.
* Authorization.
* Relevant status codes.

---

# 65. API Testing

Critical API behaviors should be covered by integration tests where practical.

Important scenarios include:

* Successful request.
* Validation failure.
* Unauthorized.
* Forbidden.
* Not found.
* Business conflict.
* Pagination.
* Contract serialization.

Detailed testing rules belong to `testing.md`.

---

# 66. API Change Review

Before completing an API task, verify:

* Is the route RESTful and consistent?
* Is the HTTP method correct?
* Are request/response models explicit?
* Are entities hidden?
* Are validation errors consistent?
* Are status codes meaningful?
* Is authorization correct?
* Is pagination required?
* Are large results bounded?
* Does the change break an existing contract?
* Are error details safe?
* Is frontend integration predictable?

---

# 67. API Change Report

Any task that modifies API behavior must report:

```text
Endpoints Added
Endpoints Modified
Endpoints Removed
Request Contract Changes
Response Contract Changes
Status Code Changes
Authorization Changes
Breaking Changes
```

If no change occurred, explicitly state:

```text
API Contract: No change
```

---

# 68. Final API Design Principle

A good API should allow a consumer to understand:

> What can I do, what must I send, what will I receive, and what happens when something goes wrong?

without needing to understand the backend implementation.

Favor stable, explicit and predictable contracts over clever API design.
