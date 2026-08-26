---

name: bootstrap-api
description: Design, create, or normalize a maintainable ASP.NET Core Web API codebase that can grow incrementally without unnecessary architectural complexity.
----------------------------------------------------------------------------------------------------------------------------------------------------------------

# Bootstrap ASP.NET Core API

## 1. Purpose

Use this skill when the task requires:

* Creating a new ASP.NET Core Web API project.
* Creating a reusable backend template.
* Establishing the initial backend architecture.
* Normalizing an early-stage backend codebase.
* Preparing a backend for future feature development.
* Creating foundational system modules.
* Preparing architecture that can scale gradually.
* Restructuring an immature project into a maintainable baseline.

This skill establishes the backend foundation.

It does NOT attempt to build every possible enterprise capability from the beginning.

---

# 2. Primary Objective

Build the smallest production-oriented backend foundation that is:

* Easy to understand.
* Easy to maintain.
* Easy to debug.
* Easy to test.
* Easy to extend.
* Safe to refactor.
* API-first.
* Modular enough to grow.
* Free from unnecessary abstraction.

The architecture must support gradual evolution:

```text
Small Backend
     ↓
Medium Application
     ↓
Modular System
     ↓
Large System
```

without requiring a complete rewrite at each stage.

---

# 3. Required Rules

Always follow the project rules defined in:

```text
.claude/rules/
```

Especially:

```text
architecture.md
code-quality.md
naming.md
api-design.md
database.md
security.md
testing.md
task-reporting.md
```

If a rule conflicts with the current repository's intentional architecture, analyze the impact before changing existing behavior.

Do not blindly rewrite working architecture.

---

# 4. Operating Modes

Determine which mode applies before modifying code.

## Mode A — New Project

Use when no backend codebase exists.

Goal:

> Create the initial architecture and minimum viable backend foundation.

---

## Mode B — Existing Early-Stage Project

Use when a backend already exists but architecture is still simple or inconsistent.

Goal:

> Normalize the existing structure incrementally without breaking current functionality.

---

## Mode C — Template Project

Use when the user wants a reusable backend template for future projects.

Goal:

> Build generic infrastructure without embedding project-specific business logic.

---

## Mode D — Architecture Baseline

Use when the user wants to establish architecture first before implementing business features.

Goal:

> Prepare structural foundations, contracts and infrastructure boundaries.

---

# 5. First Step — Inspect Before Building

Before creating or restructuring anything, inspect the repository.

Determine:

* .NET version.
* ASP.NET Core version.
* Existing solution structure.
* Existing projects.
* Existing modules.
* Existing database provider.
* Existing EF Core configuration.
* Existing authentication.
* Existing authorization.
* Existing API conventions.
* Existing dependencies.
* Existing tests.
* Existing naming conventions.
* Existing configuration.
* Existing migrations.
* Existing frontend integration contracts.

Never assume the repository is empty.

---

# 6. Existing Code Protection

When working with an existing codebase:

Do NOT automatically:

* Delete working code.
* Rename public API contracts.
* Replace architecture.
* Replace authentication.
* Replace database structure.
* Replace libraries.
* Introduce new patterns.
* Rebuild features already working.

First understand:

```text
Current State
     ↓
Problems
     ↓
Required Improvements
     ↓
Minimum Safe Change
```

Preserve observable behavior unless explicitly instructed otherwise.

---

# 7. Architecture Strategy

Default to a pragmatic layered/modular architecture.

Recommended direction:

```text
API
 ↓
Application
 ↓
Domain

Infrastructure
 ↓
Application / Domain
```

Typical solution:

```text
src/
├── Api/
├── Application/
├── Domain/
└── Infrastructure/

tests/
├── UnitTests/
└── IntegrationTests/
```

For smaller projects, fewer projects may be used if separation remains clear.

Do not create projects solely to satisfy an architectural diagram.

---

# 8. Recommended Solution Structure

A reusable baseline may resemble:

```text
Backend.sln

src/
│
├── Backend.Api/
│   ├── Controllers/
│   ├── Middleware/
│   ├── Extensions/
│   ├── Configuration/
│   ├── Program.cs
│   └── appsettings.json
│
├── Backend.Application/
│   ├── Abstractions/
│   ├── Common/
│   ├── Features/
│   └── DependencyInjection.cs
│
├── Backend.Domain/
│   ├── Entities/
│   ├── Enums/
│   ├── Exceptions/
│   ├── ValueObjects/
│   └── Common/
│
└── Backend.Infrastructure/
    ├── Persistence/
    ├── Authentication/
    ├── Services/
    ├── Integrations/
    └── DependencyInjection.cs

tests/
│
├── Backend.UnitTests/
└── Backend.IntegrationTests/
```

This is a reference structure, not a mandatory structure.

Adapt it to the actual repository.

---

# 9. Feature Organization

As the application grows, prefer feature/module organization.

Example:

```text
Application/
└── Features/
    ├── Users/
    │   ├── Create/
    │   ├── Update/
    │   ├── GetById/
    │   └── List/
    │
    ├── Roles/
    └── Products/
```

or modular organization:

```text
Modules/
├── Identity/
├── Organization/
├── Products/
└── Reporting/
```

Choose one consistent strategy.

Do not mix multiple organizational styles without justification.

---

# 10. Modular Monolith Default

For small and medium systems:

Prefer:

```text
Modular Monolith
```

before considering:

```text
Microservices
```

Reason:

* Simpler deployment.
* Simpler debugging.
* Easier transactions.
* Lower operational complexity.
* Easier local development.
* Easier refactoring.

Modules should still have clear boundaries so they can be extracted later if required.

---

# 11. Project References

Keep dependency direction controlled.

Recommended:

```text
Api
├── Application
└── Infrastructure

Application
└── Domain

Infrastructure
├── Application
└── Domain

Domain
└── No infrastructure dependency
```

Do not allow circular project references.

---

# 12. Domain Layer

Domain should contain business concepts.

Examples:

```text
Entities
ValueObjects
Enums
Domain Exceptions
Business Rules
```

Domain must not depend on:

* Controllers.
* HttpContext.
* EF Core-specific behavior unnecessarily.
* ASP.NET UI concepts.
* External APIs.
* Infrastructure implementations.

Keep the Domain layer lightweight.

Do not create complex DDD structures unless business complexity justifies them.

---

# 13. Application Layer

Application coordinates use cases.

Possible responsibilities:

```text
Commands
Queries
Use Cases
Application Services
DTOs
Validation
Interfaces
```

Application defines what the system does.

It should not contain:

* HTTP-specific response behavior.
* SQL implementation details.
* Infrastructure credentials.
* UI behavior.

---

# 14. Infrastructure Layer

Infrastructure implements external concerns.

Examples:

```text
EF Core
Database
Authentication implementations
Email
File storage
External APIs
Caching
Messaging
```

Infrastructure should implement contracts required by Application where useful.

Do not expose infrastructure implementation details through the entire system.

---

# 15. API Layer

API owns HTTP concerns.

Responsibilities:

```text
Routing
Authentication boundary
Authorization boundary
Request binding
HTTP responses
Middleware
OpenAPI
```

Controllers/endpoints should remain thin.

Preferred:

```text
HTTP
 ↓
Endpoint
 ↓
Application Use Case
 ↓
Domain / Infrastructure
```

---

# 16. Dependency Injection

Use ASP.NET Core built-in dependency injection unless another established container already exists.

Group registrations by responsibility.

Preferred:

```csharp
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);
```

Avoid putting hundreds of unrelated registrations directly into `Program.cs`.

---

# 17. Program.cs

Keep `Program.cs` primarily responsible for composition.

Example conceptual structure:

```text
Create Builder
    ↓
Register Application
    ↓
Register Infrastructure
    ↓
Register API Services
    ↓
Build App
    ↓
Configure Middleware
    ↓
Map Endpoints
    ↓
Run
```

Do not place business logic inside `Program.cs`.

---

# 18. Configuration

Use strongly typed configuration where it improves clarity.

Example:

```text
JwtOptions
DatabaseOptions
EmailOptions
StorageOptions
```

Use:

```text
IOptions<T>
```

where appropriate.

Do not access raw configuration keys throughout the application.

---

# 19. Environment Configuration

Support normal ASP.NET Core environments:

```text
Development
Staging
Production
```

Do not hard-code environment behavior.

Never put real production secrets into committed configuration files.

---

# 20. Database Foundation

If persistence is required, use the selected database provider consistently.

For EF Core:

Create a clear persistence area:

```text
Infrastructure/
└── Persistence/
    ├── AppDbContext.cs
    ├── Configurations/
    ├── Migrations/
    └── Seed/
```

Do not create repository abstraction automatically for every entity.

---

# 21. DbContext

Keep `DbContext` focused on persistence.

Example:

```csharp
public sealed class AppDbContext : DbContext
{
}
```

Entity configurations should preferably be separated when they become substantial.

Do not place business workflows inside `DbContext`.

---

# 22. Migration Strategy

When establishing migrations:

* Create migrations intentionally.
* Review generated migrations.
* Keep migration history stable.
* Do not regenerate old migrations in established systems.

For template projects, include clear instructions for creating the first migration rather than fabricating unnecessary migration history.

---

# 23. Foundational System Modules

When the user requests a reusable medium-scale backend template, foundational capabilities may include:

```text
Identity
Access Control
Organization
System Configuration
Audit
```

Possible foundational entities:

```text
User
Role
Permission
UserRole
RolePermission
Organization
Department
SystemSetting
AuditLog
```

Only add them when they belong to the requested template scope.

Do NOT turn a medium backend into a full ERP platform.

---

# 24. Identity

If authentication is included, prefer standard mechanisms.

Possible foundation:

```text
User
Role
Permission
Session / RefreshToken
```

Choose between:

```text
ASP.NET Core Identity
```

or:

```text
Custom lightweight identity model
```

based on project requirements.

Do not build custom password cryptography.

---

# 25. Access Control

For systems expected to grow, support a simple:

```text
User
 ↓
Role
 ↓
Permission
```

model.

Do not start with an overly complicated ACL engine unless required.

Keep permission naming stable.

Example:

```text
Users.View
Users.Create
Users.Update
Users.Delete
```

---

# 26. Organization

If organizational structure is required, keep the initial model simple.

Possible:

```text
Organization
Department
```

Do not add:

```text
Company
BusinessUnit
Branch
Division
Team
Region
Group
```

all at once unless the business actually requires them.

Scale the organization model incrementally.

---

# 27. API Foundation

At minimum configure API infrastructure appropriate to the project:

* Controllers or Minimal APIs.
* JSON configuration.
* Validation strategy.
* Exception handling.
* ProblemDetails.
* OpenAPI.
* Authentication when required.
* Authorization when required.

Do not build placeholder endpoints without a real purpose.

---

# 28. Error Handling

Establish centralized exception handling.

Conceptual flow:

```text
Exception
   ↓
Exception Handler
   ↓
Application Error Mapping
   ↓
ProblemDetails
```

Do not duplicate `try/catch` inside every controller.

---

# 29. Result Pattern

Do not introduce a custom `Result<T>` abstraction automatically.

Use it when application behavior benefits from explicit success/failure handling.

If introduced:

* Keep it simple.
* Avoid framework-like complexity.
* Do not create dozens of generic result types.

Exceptions and Result patterns may coexist for different failure categories.

---

# 30. Validation

Establish one consistent validation strategy.

Possible approaches:

* Data Annotations.
* FluentValidation.
* Explicit application validation.

Do not install FluentValidation automatically unless the project benefits from it.

Regardless of library, separate:

```text
Input Validation
```

from:

```text
Business Rules
```

---

# 31. Mapping

Do not introduce AutoMapper automatically.

For small and medium applications, explicit mapping is often sufficient.

Example:

```csharp
var response = new UserResponse(
    user.Id,
    user.Email,
    user.FullName);
```

Introduce a mapping library only if mapping volume justifies it.

---

# 32. Logging

Use standard structured logging.

ASP.NET Core's existing logging abstraction should normally be sufficient.

Example:

```csharp
ILogger<T>
```

Do not introduce additional logging frameworks unless there is a concrete need.

Logging should support diagnostics without leaking secrets.

---

# 33. Request Correlation

For systems requiring operational tracing, support correlation identifiers.

Do not implement complex distributed tracing infrastructure for a simple local system.

Allow observability to evolve with architecture.

---

# 34. Health Checks

Add health checks when the project requires deployment monitoring.

Possible checks:

```text
Application
Database
Critical external dependency
```

Avoid exposing sensitive infrastructure details.

---

# 35. OpenAPI

Enable OpenAPI where useful for API development and frontend integration.

Document:

* Endpoints.
* Contracts.
* Authentication requirements.
* Status codes.

Do not use Swagger-generated documentation as a substitute for clear API design.

---

# 36. Testing Foundation

When creating a reusable backend foundation, prepare testing structure.

Recommended:

```text
tests/
├── UnitTests/
└── IntegrationTests/
```

Do not create hundreds of meaningless placeholder tests.

Create only representative tests when necessary to demonstrate architecture.

---

# 37. Integration Testing

Integration testing should be possible without major architectural rewrites.

Design application composition so the API can be hosted through:

```text
WebApplicationFactory
```

when appropriate.

Do not couple application behavior tightly to production infrastructure.

---

# 38. Testability

Do not create interfaces for every class merely to enable mocking.

Prefer testing meaningful boundaries.

Examples:

* Domain logic directly.
* Application use cases.
* API integration.
* Persistence integration.

---

# 39. Dependency Policy

Before adding a NuGet dependency:

1. Determine whether .NET already provides the feature.
2. Check existing project packages.
3. Verify the dependency solves a real need.
4. Prefer mature packages.
5. Avoid unnecessary framework dependencies.

Record meaningful new packages in the final change report.

---

# 40. Recommended Minimal Dependencies

There is no mandatory package list.

A basic ASP.NET Core API may require only:

```text
ASP.NET Core
Entity Framework Core
Database Provider
OpenAPI
```

and additional packages only when requirements justify them.

Do not install packages merely because common templates use them.

---

# 41. CQRS

Do not introduce full CQRS automatically.

Use command/query separation where it improves feature clarity.

For example:

```text
CreateUser
GetUser
ListUsers
```

may be represented independently without introducing messaging infrastructure.

Do not confuse CQRS with MediatR.

---

# 42. MediatR

Do not add MediatR by default.

Use it if:

* The project benefits from pipeline behaviors.
* Many independent use cases exist.
* Handler-based organization is established.
* The abstraction reduces coupling.

Avoid introducing MediatR merely for architectural appearance.

---

# 43. Design Patterns

Patterns may include:

* Strategy.
* Factory.
* Adapter.
* Decorator.
* Specification.
* Repository.
* Unit of Work.
* Mediator.

Apply only when a concrete problem exists.

Do not pre-create empty design-pattern infrastructure.

---

# 44. Generic Infrastructure

Avoid premature generic frameworks such as:

```text
BaseController<T>
GenericService<T>
GenericRepository<T>
BaseHandler<T>
BaseCrudService<T>
```

unless repeated behavior genuinely requires them.

Generic CRUD architecture often becomes harder to maintain when business rules diverge.

---

# 45. CRUD Generation

Do not treat every business module as generic CRUD.

Even when operations look similar, business rules may differ.

Prefer explicit feature intent.

Example:

```text
CreateUser
AssignRole
DeactivateUser
ResetPassword
```

instead of a universal:

```text
CrudService<User>
```

---

# 46. Caching

Do not introduce Redis or distributed caching during initial bootstrap unless required.

Initial progression should be:

```text
Correct Implementation
        ↓
Measure
        ↓
Identify Repeated Expensive Work
        ↓
Add Appropriate Cache
```

Architecture should allow caching later without requiring it now.

---

# 47. Messaging

Do not introduce:

* Kafka.
* RabbitMQ.
* Service Bus.
* Event bus infrastructure.

unless asynchronous communication is genuinely required.

Simple in-process domain/application events may be sufficient for early stages.

---

# 48. Background Jobs

Do not add Hangfire, Quartz or other job infrastructure automatically.

Introduce background processing when actual use cases require:

* Scheduled processing.
* Long-running work.
* Retryable async work.

---

# 49. Microservices

Never bootstrap the application as microservices simply because future scaling is expected.

Prefer:

```text
Well-structured Modular Monolith
```

first.

Scale architecture based on evidence and organizational requirements.

---

# 50. Scalability Preparation

Prepare for growth by maintaining:

* Stateless API behavior where practical.
* Clear module boundaries.
* Efficient database access.
* Explicit contracts.
* Externalized configuration.
* Testability.
* Dependency boundaries.

This provides more practical scalability than premature distributed architecture.

---

# 51. Performance

During bootstrap, avoid known performance mistakes.

Examples:

* Unbounded queries.
* N+1 queries.
* Blocking async I/O.
* Loading unnecessary data.
* Excessive SaveChanges calls.

Do not implement speculative optimization infrastructure.

---

# 52. Security Baseline

At minimum:

* Never hard-code secrets.
* Use HTTPS in production.
* Validate external input.
* Parameterize database queries.
* Protect authenticated endpoints.
* Do not expose sensitive entities.
* Do not return stack traces in production.
* Protect credentials.

Apply detailed rules from `security.md`.

---

# 53. Code Style

Respect:

```text
.editorconfig
```

when present.

Enable nullable reference types unless existing repository requirements dictate otherwise.

Do not introduce repository-wide formatting changes during bootstrap normalization unless requested.

---

# 54. Naming

Use naming defined by:

```text
.claude/rules/naming.md
```

Do not invent alternative naming systems inside individual modules.

---

# 55. Reuse

Create shared components only when they represent genuinely reusable concepts.

Possible examples:

```text
Pagination
ProblemDetails extensions
CurrentUser abstraction
Clock abstraction when required
```

Avoid creating `Common` folders filled with unrelated utilities.

---

# 56. Bootstrap Scope Control

Before implementation, classify each proposed component:

```text
Required Now
Useful Foundation
Future Requirement
Speculative
```

Implement primarily:

```text
Required Now
+
Small amount of Useful Foundation
```

Do not implement speculative capabilities.

---

# 57. Phased Architecture

When requirements are broad, implement in phases.

Recommended:

```text
Phase 1
Foundation

Phase 2
Identity & Access

Phase 3
Core Business Features

Phase 4
Operational Capabilities

Phase 5
Optimization / Scale
```

Do not attempt all phases in one uncontrolled change.

---

# 58. Bootstrap Workflow

When this skill is activated, follow this sequence.

## Step 1 — Inspect

Analyze:

* Repository.
* Requirements.
* Existing architecture.
* Current dependencies.

---

## Step 2 — Define Scope

List:

```text
Required
Optional
Out of Scope
```

Avoid silently expanding scope.

---

## Step 3 — Architecture Plan

Define:

* Solution structure.
* Project dependencies.
* Persistence strategy.
* API strategy.
* Authentication strategy if required.
* Testing strategy.

Keep the plan proportional to project size.

---

## Step 4 — Identify Risks

Check for:

* Breaking API changes.
* Database changes.
* Authentication changes.
* Migration risks.
* Package additions.
* Existing architecture conflicts.

---

## Step 5 — Build Foundation

Implement foundation incrementally.

Do not create dozens of empty folders/classes merely to illustrate architecture.

Every created component should have a reason to exist.

---

## Step 6 — Build Core Infrastructure

Only where required:

* Persistence.
* Exception handling.
* Validation.
* Authentication.
* Authorization.
* OpenAPI.
* Dependency registration.

---

## Step 7 — Implement Representative Feature

When useful for a template, implement one small vertical slice demonstrating how future features should be built.

Example:

```text
Users
 ├── Request
 ├── Use Case
 ├── Persistence
 └── Endpoint
```

Do not implement many duplicate sample modules.

---

## Step 8 — Verify Dependencies

Check architecture references and prevent circular dependencies.

---

## Step 9 — Build

Run the appropriate build command.

Example:

```bash
dotnet build
```

Fix compilation errors introduced by the task.

---

## Step 10 — Test

Run relevant tests if available.

Example:

```bash
dotnet test
```

Do not claim tests passed unless actually executed.

---

## Step 11 — Review

Review the created architecture against:

* Simplicity.
* Responsibility.
* Naming.
* Testability.
* Security.
* Extensibility.

Remove unnecessary complexity discovered during review.

---

## Step 12 — Report

Produce the required CHANGE REPORT according to project task-reporting rules.

---

# 59. Completion Report — Bootstrap Specific

In addition to the global change report, report:

```text
Architecture Created
Projects Created
Modules Created
Infrastructure Added
NuGet Packages Added
Database Setup
Authentication Setup
Authorization Setup
Testing Setup
Configuration Added
Migrations Added
Breaking Changes
Deferred Capabilities
Recommended Next Step
```

Example:

```text
## Architecture

- API
- Application
- Domain
- Infrastructure

## Foundation Added

- Global exception handling
- EF Core registration
- OpenAPI
- Authentication

## Deferred

- Distributed cache
- Message queue
- Background jobs
- Microservices

Reason:
No current requirement justifies these capabilities.
```

---

# 60. Do Not Generate Placeholder Architecture

Avoid output such as:

```text
Services/
Repositories/
Managers/
Handlers/
Factories/
Builders/
Strategies/
Providers/
Helpers/
```

with no concrete responsibility.

Empty architecture is not architecture.

Create components only when they participate in the application design.

---

# 61. Do Not Over-Engineer

Reject unnecessary complexity when a simpler design satisfies the requirement.

Examples:

Do not introduce:

```text
Microservices
Event Sourcing
Distributed Transactions
Kafka
Redis
CQRS Framework
Generic Repository
Custom Event Bus
Complex DDD
```

without a concrete requirement.

The codebase must be capable of evolving toward these techniques if needed, not contain them from day one.

---

# 62. Do Not Under-Engineer

Simplicity does not mean placing everything into:

```text
Controllers/
Services/
Models/
```

until the codebase becomes unmanageable.

Maintain clear responsibility boundaries from the beginning.

Use enough structure to support safe growth.

---

# 63. Template Neutrality

When building a reusable template:

Do not embed:

* Customer-specific names.
* Company-specific business logic.
* Hard-coded organization structures.
* Real credentials.
* Project-specific routes.

Keep the template reusable.

Sample business code must be clearly replaceable.

---

# 64. Existing Project Normalization

When normalizing an existing project:

Prefer incremental transitions.

Example:

```text
Current Code
    ↓
Establish Boundary
    ↓
Move One Feature
    ↓
Build/Test
    ↓
Continue
```

Avoid:

```text
Delete architecture
    ↓
Rewrite everything
    ↓
Hope it works
```

---

# 65. Architecture Documentation

When a significant architecture is created, update or create concise documentation describing:

* Layer responsibility.
* Dependency direction.
* Feature creation pattern.
* Database migration workflow.
* Local startup instructions.

Do not generate large documentation that will immediately become outdated.

---

# 66. New Feature Readiness

The bootstrap is successful when a developer can add a new feature without guessing:

```text
Where should the endpoint go?
Where should business logic go?
Where should persistence go?
Where should validation go?
Where should DTOs go?
How should dependencies be registered?
How should the feature be tested?
```

The architecture should make these answers predictable.

---

# 67. Maintenance Readiness

The codebase should make it easy to:

* Locate responsibility.
* Diagnose failures.
* Change business logic.
* Replace infrastructure.
* Add tests.
* Upgrade packages.
* Refactor modules.

Avoid hidden or magical behavior that makes maintenance difficult.

---

# 68. Upgrade Readiness

Do not couple application code unnecessarily to:

* Specific framework internals.
* Deprecated APIs.
* Static global state.
* Package-specific abstractions.

This reduces future .NET upgrade cost.

---

# 69. Scale-Up Readiness

Future scale should primarily be supported through clear boundaries.

Expected evolution:

```text
Module
   ↓
Independent application boundary
   ↓
Independent persistence if required
   ↓
Independent deployment if required
```

Do not require distributed architecture before those needs exist.

---

# 70. Final Decision Rule

When choosing between two architectures, prefer the one that:

1. Correctly supports current requirements.
2. Has fewer unnecessary moving parts.
3. Clearly separates responsibilities.
4. Fits ASP.NET Core conventions.
5. Is easy to test.
6. Is easy to maintain.
7. Can evolve incrementally.
8. Does not lock the project into premature infrastructure.

---

# 71. Definition of Done

Bootstrap is complete only when:

* The project builds.
* Architecture boundaries are understandable.
* Dependencies follow intended direction.
* Configuration is externalized.
* Persistence is correctly configured if required.
* API errors are handled consistently.
* Security baseline is respected.
* New features have a predictable implementation path.
* Tests can be added without structural rewrites.
* No unnecessary infrastructure was introduced.
* Significant changes are reported.

The final objective is:

> Create a backend foundation that is simple enough to work with today and structured enough to grow tomorrow.
