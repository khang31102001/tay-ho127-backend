---

name: refactor-backend
description: Refactor ASP.NET Core backend code to improve structure, readability, naming, cohesion, maintainability, and internal logic while preserving existing observable behavior unless behavior changes are explicitly requested.
----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

# Refactor Backend

## 1. Purpose

Use this skill when the task requires:

* Refactoring existing backend code.
* Cleaning up architecture.
* Simplifying implementation logic.
* Improving naming.
* Removing confirmed dead code.
* Reducing duplication.
* Reducing coupling.
* Splitting oversized classes.
* Improving responsibility boundaries.
* Reorganizing files/folders.
* Normalizing inconsistent code.
* Improving maintainability.
* Preparing code for future extension.
* Reducing technical debt.

The primary principle is:

> Refactor internal implementation without unintentionally changing observable behavior.

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

Refactoring must not bypass project rules.

---

# 3. Definition of Refactoring

Refactoring means changing internal code structure while preserving externally observable behavior.

Examples of allowed refactoring:

```text
Simplify conditions
Extract methods
Split responsibilities
Improve naming
Remove duplication
Remove dead code
Reduce nesting
Reduce coupling
Improve dependency direction
Reorganize files
Improve query readability
Replace harmful abstractions
```

Refactoring does NOT automatically include:

```text
Changing business rules
Changing API contracts
Changing database schema
Changing permissions
Changing authentication
Changing external behavior
Adding new features
Changing response formats
```

Those changes require explicit task scope.

---

# 4. Observable Behavior

Observable behavior includes:

* HTTP routes.
* HTTP methods.
* Request contracts.
* Response contracts.
* Status codes.
* Business results.
* Authorization behavior.
* Authentication behavior.
* Database persistence behavior.
* External integration behavior.
* Events/messages.
* Side effects.
* Existing valid application workflows.

Preserve these unless explicitly instructed otherwise.

---

# 5. Operating Modes

Determine the refactoring mode first.

## Mode A — Local Code Refactor

Use for:

* Method cleanup.
* Class cleanup.
* Condition simplification.
* Duplication removal.

Scope should remain highly localized.

---

## Mode B — Module Refactor

Use for:

* Reorganizing one feature/module.
* Splitting responsibilities.
* Improving module architecture.
* Standardizing naming.

Protect module contracts.

---

## Mode C — Architecture Refactor

Use for:

* Layer normalization.
* Dependency direction correction.
* Moving logic between layers.
* Reducing architectural coupling.

Architecture changes require broader impact analysis.

---

## Mode D — Naming Cleanup

Use for:

* Files.
* Classes.
* Methods.
* Variables.
* Internal DTOs.

Public contract renames require explicit approval.

---

## Mode E — Dead Code Cleanup

Use when removing:

* Unused classes.
* Unused methods.
* Unused imports.
* Obsolete code.
* Duplicate implementations.

Deletion requires usage verification.

---

# 6. Inspect Before Refactoring

Before modifying anything:

1. Read target code.
2. Identify callers.
3. Identify dependencies.
4. Identify API exposure.
5. Identify database usage.
6. Identify security implications.
7. Identify tests.
8. Search references.
9. Understand existing behavior.
10. Identify actual code smell.

Do not refactor purely based on file appearance.

---

# 7. Establish Refactor Goal

Every refactor must have a clear objective.

Examples:

```text
Reduce duplicated user validation
Split oversized UserService
Remove unused legacy repository
Normalize inconsistent naming
Move business rules out of controller
Reduce nested conditional logic
```

Avoid vague goals such as:

```text
Make code cleaner
Make architecture better
Improve everything
```

Define measurable intent.

---

# 8. Scope Control

Clearly classify:

```text
In Scope
Out of Scope
```

Example:

```text
In Scope:
- Refactor User module structure
- Normalize internal naming
- Remove unused User helpers

Out of Scope:
- Change login behavior
- Change API routes
- Change database schema
```

Do not allow refactoring scope to expand silently.

---

# 9. Baseline Behavior

Before refactoring, establish baseline behavior using available evidence.

Possible sources:

* Existing tests.
* API contracts.
* Documentation.
* Current implementation.
* Integration tests.
* Existing consumers.

If automated tests exist, run relevant tests before major refactoring when practical.

---

# 10. Prefer Incremental Refactoring

Prefer:

```text
Small Change
   ↓
Build
   ↓
Test
   ↓
Continue
```

instead of:

```text
Rewrite Entire Module
   ↓
Debug Everything Later
```

Large refactors should be broken into safe stages.

---

# 11. Do Not Rewrite Working Code Without Reason

Code being old or stylistically different is not sufficient reason to rewrite it.

Refactor only when there is meaningful improvement such as:

* Responsibility clarity.
* Reduced duplication.
* Reduced complexity.
* Improved maintainability.
* Fixed architecture violation.
* Easier testing.

---

# 12. Business Logic Protection

Do not change:

```text
Discount thresholds
Approval rules
Financial calculations
Status transitions
Permission decisions
Data validation rules
```

unless explicitly requested.

If business behavior appears incorrect, report it separately instead of silently modifying it during refactoring.

---

# 13. API Contract Protection

Do not change:

* Routes.
* HTTP methods.
* JSON property names.
* Request models.
* Response models.
* Status-code behavior.

during ordinary refactoring.

Internal DTOs may be refactored if no public contract is affected.

---

# 14. Database Protection

Do not rename:

* Tables.
* Columns.
* Keys.
* Constraints.

solely to match code naming style unless explicitly requested.

Database schema changes require the database design workflow.

---

# 15. Security Protection

Never weaken:

* Authentication.
* Authorization.
* Permission checks.
* Tenant filtering.
* Resource ownership validation.

to simplify implementation.

Security behavior must remain equal or stronger.

---

# 16. Simplify Conditional Logic

Reduce unnecessary nesting.

Before:

```csharp
if (user != null)
{
    if (user.IsActive)
    {
        if (user.HasPermission)
        {
            return true;
        }
    }
}

return false;
```

After:

```csharp
return user is not null
    && user.IsActive
    && user.HasPermission;
```

Only simplify when semantics remain identical.

---

# 17. Guard Clauses

Use guard clauses when they improve clarity.

Before:

```csharp
if (request != null)
{
    if (request.IsValid)
    {
        Process(request);
    }
}
```

Possible refactor:

```csharp
if (request is null)
    return;

if (!request.IsValid)
    return;

Process(request);
```

Do not use guard clauses mechanically.

---

# 18. Extract Method

Extract cohesive logic when a method contains multiple responsibilities.

Before:

```text
Validate
Calculate
Save
Send Email
Log
```

Potential result:

```text
ValidateOrder()
CalculateTotal()
PersistOrderAsync()
SendConfirmationAsync()
```

Do not extract trivial one-line methods without readability benefit.

---

# 19. Split Oversized Classes

A class may need splitting when it:

* Has unrelated responsibilities.
* Requires many dependencies.
* Changes for unrelated reasons.
* Has large unrelated method groups.
* Mixes business logic and infrastructure.

Do not split solely because line count is high.

---

# 20. Fat Controllers

Move business logic out of controllers.

Preferred:

```text
Controller
   ↓
Application Use Case
   ↓
Business Logic
```

Avoid controllers performing:

* EF queries.
* Business calculations.
* Workflow orchestration.
* External service integration.

---

# 21. Service Refactor

Watch for services such as:

```text
UserService
SystemService
CommonService
ManagementService
```

that contain unrelated responsibilities.

Split by cohesive capability when justified.

Example:

```text
UserRegistration
UserAuthentication
RoleAssignment
UserProfile
```

Do not over-fragment simple services.

---

# 22. Dependency Count

A class with many constructor dependencies may indicate excessive responsibility.

Investigate before introducing:

* Service locator.
* Dependency aggregator.
* Mega facade.

Fix responsibility boundaries first.

---

# 23. Reduce Coupling

Reduce unnecessary direct dependencies between modules.

Prefer:

```text
Module A
   ↓
Explicit Contract
   ↓
Module B
```

over reaching deeply into another module's internal implementation.

Do not introduce interfaces solely to create visual decoupling.

---

# 24. Improve Cohesion

Keep related behavior close together.

Avoid:

```text
Business rule in Controller
Validation in Helper
Calculation in Utility
Persistence in Service
```

when they belong to one cohesive feature.

Architecture should make feature behavior discoverable.

---

# 25. Duplicate Logic

Identify whether duplication represents the same knowledge.

Good extraction candidate:

```text
Same permission evaluation
Same financial calculation
Same normalization logic
```

Do not abstract coincidentally similar code with different business meaning.

---

# 26. DRY Carefully

DRY means:

> Avoid duplicated knowledge.

It does NOT mean:

> Never repeat lines of code.

Small duplication may be preferable to a harmful generic abstraction.

---

# 27. Remove Harmful Generic Abstractions

Review generic infrastructure such as:

```text
GenericRepository<T>
BaseService<T>
BaseController<T>
CommonHelper
UtilityService
```

If abstraction:

* Hides domain intent.
* Creates difficult inheritance.
* Requires constant special cases.
* Provides no meaningful reuse.

consider replacing it with explicit feature code.

Do not remove established abstractions without impact analysis.

---

# 28. Inheritance

Prefer composition when inheritance does not represent a meaningful "is-a" relationship.

Avoid deep inheritance trees.

Example warning:

```text
BaseService
  ↓
GenericService
  ↓
UserBaseService
  ↓
UserService
```

Simplify when possible.

---

# 29. Interfaces

Remove unnecessary interfaces only after verifying:

* DI usage.
* Multiple implementations.
* Testing.
* External contracts.
* Future extension actually in use.

Do not remove interfaces mechanically.

---

# 30. Naming Refactor

Use naming conventions from `naming.md`.

Improve names that are:

```text
data
info
temp
item
manager
helper
processor
obj
value
```

when better domain terminology exists.

---

# 31. Rename by Responsibility

Before:

```text
UserHelper
```

After, depending on responsibility:

```text
PasswordGenerator
UserNameNormalizer
UserPermissionChecker
```

Do not create overly long names.

---

# 32. File Renaming

When renaming files:

1. Rename primary type.
2. Rename file consistently.
3. Update references.
4. Check namespaces.
5. Check DI.
6. Build.
7. Report rename.

---

# 33. Public Contract Rename

Do not rename public:

```text
Routes
JSON properties
Public DTO fields
Message contracts
Database columns
```

as part of naming cleanup unless explicitly requested.

---

# 34. Folder Refactoring

Reorganize folders only when structure becomes clearer.

Before:

```text
Services/
Repositories/
Helpers/
Models/
```

Potential feature structure:

```text
Features/
├── Users/
├── Roles/
└── Orders/
```

Do not reorganize a mature project merely because another style is preferred.

---

# 35. Namespace Refactoring

When folders move, ensure namespaces remain:

* Predictable.
* Consistent.
* Not unnecessarily deep.

Build after namespace changes.

---

# 36. Dead Code Detection

Potential dead code includes:

* Unused classes.
* Unused private methods.
* Unused imports.
* Obsolete DTOs.
* Duplicate implementations.
* Commented-out legacy code.

Never delete based only on appearance.

---

# 37. Dead Code Verification

Before deleting code:

1. Search static references.
2. Search DI registration.
3. Search reflection usage.
4. Search serialization.
5. Search configuration.
6. Search route registration.
7. Search tests.
8. Search scripts/jobs if present.

Only delete when reasonably confirmed unused.

---

# 38. Commented-Out Code

Remove obsolete commented implementation.

Version control should preserve history.

Keep comments only when they explain:

* Why.
* Business constraint.
* Technical limitation.
* Important workaround.

---

# 39. Remove Unused Dependencies

Review unused NuGet packages carefully.

Before removal:

* Search direct usage.
* Check transitive requirements.
* Check configuration.
* Build after removal.

Do not remove a dependency merely because no obvious class references exist.

---

# 40. Async Refactoring

Improve async flow where appropriate.

Avoid:

```csharp
.Result
.Wait()
```

in normal ASP.NET Core request paths.

Propagate `CancellationToken`.

Do not convert CPU-bound synchronous logic to async unnecessarily.

---

# 41. Query Refactoring

Improve EF Core queries without changing results.

Possible improvements:

* Projection.
* `AsNoTracking`.
* Filter before materialization.
* Remove duplicate queries.
* Avoid unnecessary includes.

Always verify semantic equivalence.

---

# 42. Query Result Protection

When modifying LINQ:

Be careful with differences in:

* Ordering.
* Null semantics.
* Duplicate rows.
* Client vs server evaluation.
* Case sensitivity.
* Deferred execution.

Cleaner-looking LINQ may behave differently.

---

# 43. Performance During Refactor

Minor obvious performance improvements are allowed when behavior remains identical.

Example:

```text
Remove duplicate query
Avoid repeated enumeration
Avoid unnecessary object creation
```

Major performance redesign should use the performance optimization skill.

---

# 44. Error Handling Refactor

Centralize repeated exception handling where appropriate.

Do not change expected status codes or error contracts unintentionally.

Avoid swallowing exceptions for cleaner code.

---

# 45. Validation Refactor

Consolidate duplicated validation only when semantics are identical.

Do not merge:

```text
Request Validation
```

with:

```text
Business Rule Validation
```

simply because conditions look similar.

---

# 46. Mapping Refactor

Simplify mapping when useful.

Do not introduce AutoMapper solely because manual mapping exists.

Do not hide business logic in mapping code.

---

# 47. Constants and Magic Values

Replace meaningful repeated magic values with:

* Enum.
* Constant.
* Configuration.
* Domain value.

Do not create constants for obvious local values without benefit.

---

# 48. Static State

Remove unnecessary mutable static state where it creates:

* Concurrency risk.
* Hidden dependencies.
* Testing difficulty.

Do not replace all static helpers automatically.

Pure stateless utility functions may be acceptable.

---

# 49. Helper Cleanup

Generic helper classes should be reviewed for multiple responsibilities.

Example:

```text
CommonHelper
├── HashPassword
├── FormatDate
├── GenerateCode
├── SendEmail
└── ParseJson
```

should likely be decomposed by responsibility.

---

# 50. Feature Boundaries

When refactoring modules, make dependencies explicit.

Prefer:

```text
Identity
Organization
Orders
Reporting
```

with clear ownership.

Avoid modules directly modifying another module's internal persistence without reason.

---

# 51. Architecture Direction

Refactoring should improve dependency direction.

Preferred:

```text
API → Application → Domain
Infrastructure → Application / Domain
```

Avoid introducing reverse dependencies.

---

# 52. Circular Dependencies

Remove circular dependencies when encountered within scope.

Do not solve cycles by introducing a generic `Common` project that becomes a dumping ground.

Identify the correct ownership boundary.

---

# 53. Refactoring Toward Modules

For growing systems, migration may follow:

```text
Layered Code
   ↓
Identify Business Capability
   ↓
Group Related Behavior
   ↓
Establish Module Boundary
```

Do not create microservices as part of ordinary refactoring.

---

# 54. Preserve Existing Conventions

If project conventions are internally consistent and not harmful, follow them.

Do not create a second style inside the same module.

---

# 55. Refactor Tests Carefully

Tests may also be refactored for:

* Naming.
* Duplication.
* Fixtures.
* Readability.

Do not weaken assertions or remove behavioral coverage.

---

# 56. Characterization Tests

When refactoring poorly tested legacy code, consider characterization tests.

Purpose:

> Capture current behavior before changing internal implementation.

This helps protect against accidental behavior changes.

---

# 57. Regression Tests

Add targeted regression tests around risky refactoring where useful.

High-risk areas include:

* Financial calculations.
* Permissions.
* Status transitions.
* Data transformations.
* Complex queries.

---

# 58. Build Frequently

For significant refactors:

Run build after meaningful stages.

Example:

```text
Rename
   ↓
Build
   ↓
Move Logic
   ↓
Build
   ↓
Cleanup
   ↓
Build
```

Do not wait until hundreds of changes accumulate.

---

# 59. Test Frequently

Run targeted tests after logical stages when practical.

This reduces the debugging surface if behavior changes accidentally.

---

# 60. Compiler Warnings

Do not introduce new warnings.

Do not silence warnings to complete the refactor.

Fix the actual issue when possible.

---

# 61. Formatting

Avoid repository-wide formatting during unrelated refactoring.

Only format affected code unless task explicitly requests normalization.

Large formatting diffs make review difficult.

---

# 62. Git-Friendly Refactoring

Prefer changes that are easy to review.

Avoid mixing in one change:

```text
File Move
+
Mass Formatting
+
Business Logic Change
+
Dependency Upgrade
```

unless explicitly required.

---

# 63. Refactor Risk Classification

Classify refactor risk:

```text
LOW
MEDIUM
HIGH
```

Examples:

```text
LOW:
Internal method rename.

MEDIUM:
Split application service.

HIGH:
Move logic across architectural layers affecting many modules.
```

Use risk to determine verification depth.

---

# 64. Breaking Change Check

Before completion explicitly evaluate:

```text
API Contract
Database
Security
Business Logic
Configuration
External Integration
```

Each should be marked:

```text
Changed
Unchanged
```

---

# 65. Behavior Comparison

For significant refactoring compare:

```text
Before
vs
After
```

Example:

```text
Before:
UserController contained validation and persistence.

After:
Controller delegates to CreateUser use case.

Behavior:
Unchanged.
```

---

# 66. Refactor Workflow

When this skill is activated, follow this sequence.

## Step 1 — Inspect

Read target area and dependencies.

---

## Step 2 — Define Refactor Goal

State exactly what is being improved.

---

## Step 3 — Define Scope

Identify in-scope and out-of-scope areas.

---

## Step 4 — Establish Baseline

Understand current behavior and tests.

---

## Step 5 — Identify Code Smells

Examples:

```text
Duplication
Large Class
Large Method
Poor Naming
High Coupling
Deep Nesting
Dead Code
Architecture Violation
```

---

## Step 6 — Risk Analysis

Classify:

```text
LOW
MEDIUM
HIGH
```

---

## Step 7 — Create Refactor Plan

Break into small safe stages.

---

## Step 8 — Refactor Incrementally

Make focused changes.

---

## Step 9 — Build

Run:

```bash
dotnet build
```

when available.

---

## Step 10 — Test

Run relevant:

```bash
dotnet test
```

---

## Step 11 — Remove Dead Code

Only after usage verification.

---

## Step 12 — Review Naming

Apply naming rules.

---

## Step 13 — Review Architecture

Check dependency direction and responsibilities.

---

## Step 14 — Regression Check

Confirm existing behavior remains intact.

---

## Step 15 — Report

Produce the global CHANGE REPORT plus refactor-specific details.

---

# 67. Refactor-Specific Report

Always include:

```text
## Refactor Summary

Goal:
...

Scope:
...

Risk:
LOW / MEDIUM / HIGH

## Before

...

## After

...

## Improvements

- ...
- ...

## Behavior Preservation

Business Logic:
UNCHANGED / CHANGED

API Contract:
UNCHANGED / CHANGED

Database Schema:
UNCHANGED / CHANGED

Security:
UNCHANGED / CHANGED

External Integration:
UNCHANGED / CHANGED
```

---

# 68. File Change Report

Clearly list:

```text
[ADDED]
...

[MODIFIED]
...

[RENAMED]
old → new

[MOVED]
old → new

[DELETED]
...
```

For deleted files, explain why removal is safe when relevant.

---

# 69. Naming Changes

List important renames separately.

Example:

```text
UserManager
→ UserApplicationService

CommonHelper
→ removed

Process()
→ CalculateInvoiceTotal()
```

Do not force reviewers to infer renames from file diffs.

---

# 70. Dead Code Report

Example:

```text
Removed:
LegacyUserRepository.cs

Reason:
No references found in application code, DI registration,
tests, configuration, serialization or endpoint registration.
```

Do not state "unused" without some verification.

---

# 71. Logic Optimization Report

When implementation logic is simplified:

Report:

```text
Before:
Three nested conditions.

After:
Guard clauses.

Behavior:
Equivalent.

Reason:
Reduced nesting and improved readability.
```

---

# 72. Architecture Change Report

If architecture changed:

Report:

```text
Before:
Controller → DbContext

After:
Controller → Application Use Case → DbContext

Reason:
Move business/application responsibility out of HTTP layer.

Public Behavior:
Unchanged.
```

---

# 73. Verification Report

Always report actual verification:

```text
Build: PASS / FAIL / NOT RUN
Tests: PASS / FAIL / NOT RUN
Integration Tests: PASS / FAIL / NOT RUN
Regression Check: PASS / FAIL / NOT RUN
```

Never claim more than was executed.

---

# 74. Remaining Technical Debt

If issues remain outside scope, report:

```text
Remaining:
Order module still contains duplicated validation.

Reason not changed:
Outside current User module refactor scope.

Recommended Task:
Refactor Order validation separately.
```

---

# 75. Do Not Use Refactor to Hide Feature Work

If the implementation requires a new business capability, classify it as feature development.

Do not label business changes as refactoring merely to avoid contract review.

---

# 76. Do Not Refactor Everything

A repository-wide refactor is high risk.

If the user requests "refactor entire codebase":

Perform it incrementally:

```text
Audit
 ↓
Prioritize
 ↓
Module 1
 ↓
Build/Test
 ↓
Module 2
 ↓
Build/Test
```

Do not rewrite the entire repository in one uncontrolled pass.

---

# 77. Refactor Priority

When many issues exist, prioritize:

1. Correctness risks.
2. Security risks.
3. Architecture violations.
4. High coupling.
5. Duplicated business logic.
6. Complex code.
7. Naming.
8. Cosmetic cleanup.

Do not spend most effort on naming while structural problems remain.

---

# 78. Stop Condition

Stop refactoring when:

* Goal is achieved.
* Code is clearly maintainable.
* Further changes provide marginal value.
* Remaining changes would expand scope unnecessarily.

Refactoring does not require making every file perfect.

---

# 79. Definition of Done

A refactoring task is complete only when:

1. The refactor goal is satisfied.
2. Scope remained controlled.
3. Existing behavior is preserved unless explicitly changed.
4. Responsibilities are clearer.
5. Naming is improved where relevant.
6. Duplication is reduced where meaningful.
7. Confirmed dead code is removed.
8. Architecture remains valid.
9. Security is not weakened.
10. Build/tests are executed when possible.
11. Regression risk is evaluated.
12. All significant changes are reported clearly.

The objective is:

> Improve how the code is built without accidentally changing what the system does.
