---

name: test-backend
description: Design, implement, run, and review meaningful ASP.NET Core backend tests including unit, integration, API, persistence, security, and regression tests while preserving real application behavior.
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

# Test Backend

## 1. Purpose

Use this skill when the task requires:

* Writing unit tests.
* Writing integration tests.
* Writing API tests.
* Writing persistence tests.
* Writing regression tests.
* Testing business rules.
* Testing authorization.
* Testing validation.
* Testing database behavior.
* Increasing meaningful test coverage.
* Verifying bug fixes.
* Verifying refactoring.
* Reviewing existing tests.
* Fixing broken test infrastructure.
* Establishing backend testing conventions.

The objective is:

> Verify meaningful system behavior, not maximize test count.

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

Tests must represent intended behavior.

Do not weaken production code purely to make it easier to test.

---

# 3. Core Testing Strategy

Think in layers:

```text
Business Rule
     ↓
Unit Test

Application Use Case
     ↓
Application Test

Database Behavior
     ↓
Integration Test

HTTP Contract
     ↓
API Integration Test

Bug
     ↓
Regression Test
```

Choose the lowest test level that can verify the behavior reliably.

Do not test everything through HTTP.

Do not mock everything either.

---

# 4. Testing Priority

Prioritize tests in this order:

1. Critical business rules.
2. Security and authorization.
3. Data integrity.
4. State transitions.
5. Bug regression.
6. Complex application logic.
7. Important API contracts.
8. Integration boundaries.
9. Edge cases.
10. Trivial implementation details.

Do not spend large effort testing simple property assignments.

---

# 5. Inspect Existing Test Architecture

Before adding tests, inspect:

* Existing test projects.
* Existing test framework.
* Existing fixture strategy.
* Existing naming conventions.
* Existing integration setup.
* Existing database setup.
* Existing mocks.
* Existing test utilities.
* Existing authentication helpers.
* Existing test data builders.

Prefer existing conventions unless they are clearly harmful.

---

# 6. Supported Testing Framework

Respect the framework already used.

Common examples:

```text
xUnit
NUnit
MSTest
```

Do not introduce another test framework without a concrete reason.

For a new ASP.NET Core codebase, `xUnit` is a reasonable default unless project requirements specify otherwise.

---

# 7. Assertion Libraries

Use the existing assertion strategy.

Possible:

```text
xUnit Assert
FluentAssertions
Shouldly
```

Do not add an assertion library solely for stylistic preference.

Consistency is more important.

---

# 8. Mocking Libraries

Use existing mocking infrastructure.

Possible examples:

```text
Moq
NSubstitute
FakeItEasy
```

Do not introduce a mocking library when simple fakes or real objects are clearer.

---

# 9. Test Naming

Test names must communicate:

```text
Scenario
+
Expected Result
```

Preferred examples:

```text
CreateUser_WithValidRequest_ReturnsCreatedUser

CreateUser_WithExistingEmail_ReturnsConflict

ApproveOrder_WhenOrderIsCancelled_ThrowsBusinessRuleException
```

Avoid:

```text
Test1
CreateUserTest
ShouldWork
TestCreate
```

---

# 10. Test Structure

Prefer a clear Arrange / Act / Assert structure.

Example:

```csharp
[Fact]
public async Task CreateUser_WithValidRequest_CreatesUser()
{
    // Arrange

    // Act

    // Assert
}
```

Comments are optional when structure is already obvious.

---

# 11. One Behavior per Test

Each test should verify one meaningful behavior.

Avoid one test asserting dozens of unrelated scenarios.

Prefer:

```text
Scenario A → Test A
Scenario B → Test B
Scenario C → Test C
```

This makes failures easier to diagnose.

---

# 12. Do Not Test Implementation Details

Test externally meaningful behavior.

Avoid tests tightly coupled to:

* Private methods.
* Exact internal method-call order.
* Temporary internal structure.
* Internal variable names.

Refactoring internal implementation should not break good behavioral tests.

---

# 13. Unit Tests

Use unit tests for isolated logic.

Good candidates:

* Calculations.
* Business rules.
* Value objects.
* Domain state transitions.
* Validators.
* Pure application logic.

Unit tests should normally:

* Execute quickly.
* Avoid network.
* Avoid real database.
* Be deterministic.

---

# 14. Domain Tests

Domain rules should be tested directly where possible.

Example:

```text
Order.Approve()

Valid:
Pending → Approved

Invalid:
Cancelled → Approved
```

Test business meaning, not ORM behavior.

---

# 15. Application Tests

Application-level tests may verify use cases with controlled dependencies.

Example:

```text
CreateUser
    ↓
Check Duplicate
    ↓
Hash Password
    ↓
Save User
```

Mock only boundaries whose real implementation is not relevant to the behavior being tested.

---

# 16. Integration Tests

Use integration tests when multiple real components must work together.

Examples:

* API → Application.
* Application → EF Core.
* Authentication → Authorization.
* Serialization → HTTP contract.
* Database constraint behavior.

Integration tests should verify realistic system behavior.

---

# 17. API Integration Tests

For ASP.NET Core APIs, use `WebApplicationFactory` where appropriate.

Concept:

```text
Test
 ↓
WebApplicationFactory
 ↓
ASP.NET Core Pipeline
 ↓
Endpoint
 ↓
Application
 ↓
Infrastructure
```

This verifies more than directly calling controller methods.

---

# 18. Controller Tests

Do not unit-test thin controllers excessively.

If a controller only delegates to application logic, API integration testing usually provides more value.

Unit-test controllers only when they contain meaningful HTTP-specific behavior requiring isolated verification.

---

# 19. Database Tests

Use database tests when behavior depends on:

* Relationships.
* Constraints.
* Transactions.
* SQL translation.
* Concurrency.
* Unique indexes.
* Query filters.

Do not mock the database when the database itself is the behavior being tested.

---

# 20. EF Core InMemory Provider

Do not assume EF Core InMemory provider behaves like a relational database.

It does not reliably test:

* SQL translation.
* Foreign keys.
* Transactions.
* Relational constraints.
* Provider-specific behavior.

Use only when its limitations do not affect the test.

---

# 21. Relational Integration Database

When realistic relational behavior matters, prefer:

* SQLite where compatible.
* Testcontainers.
* Dedicated test database.
* Same production database engine when practical.

Choose based on project complexity.

Do not introduce Docker/Testcontainers infrastructure for trivial tests without need.

---

# 22. Testcontainers

Use Testcontainers when high-fidelity database or infrastructure testing provides meaningful value.

Good candidates:

```text
SQL Server
PostgreSQL
Redis
RabbitMQ
```

Do not start infrastructure containers for every unit test.

---

# 23. Test Isolation

Tests must not depend on execution order.

Each test should establish the state it requires.

Avoid:

```text
Test A creates user
Test B assumes user from Test A
```

unless explicitly implementing an ordered end-to-end scenario.

---

# 24. Database Isolation

Integration tests should prevent state leakage.

Possible strategies:

* New database per suite.
* Transaction rollback.
* Respawn/reset.
* Unique test identifiers.
* Container recreation where justified.

Choose a strategy appropriate to test cost.

---

# 25. Deterministic Tests

Tests should produce the same result repeatedly.

Avoid uncontrolled dependencies on:

* Current time.
* Random values.
* External network.
* Environment state.
* Test order.

Control these dependencies where behavior requires it.

---

# 26. Time Testing

For time-sensitive business logic, avoid tests depending directly on:

```text
DateTime.Now
```

when controllable time would improve reliability.

Use a clock/time abstraction only where justified.

Do not introduce time abstractions everywhere automatically.

---

# 27. Random Data

Random test data may improve variety but can make failures difficult to reproduce.

If randomness is used:

* Make failures reproducible.
* Use deterministic seeds where practical.

Critical tests should remain understandable.

---

# 28. Test Data Builders

Use builders/factories when setup becomes repetitive.

Example:

```text
UserBuilder
OrderBuilder
```

Do not create complex test-builder frameworks before repetition justifies them.

---

# 29. Fixtures

Use fixtures for expensive shared setup where appropriate.

Do not let shared mutable fixture state create test coupling.

---

# 30. Happy Path

Important features should normally have at least one successful scenario.

Example:

```text
Given valid user request
When creating user
Then user is persisted
And response is correct
```

---

# 31. Validation Tests

Test meaningful validation boundaries.

Examples:

```text
Missing email
Invalid email
Name too long
Invalid page size
```

Do not duplicate every framework-level validation behavior without value.

---

# 32. Business Rule Tests

Critical business rules require explicit tests.

Example:

```text
Cancelled order cannot be approved.
```

Test:

```text
Order = Cancelled
Action = Approve
Expected = Rejected
```

Business-rule tests are high-value regression protection.

---

# 33. State Transition Tests

For lifecycle entities test valid and invalid transitions.

Example:

```text
Draft → Pending        VALID
Pending → Approved     VALID
Cancelled → Approved   INVALID
```

Do not assume status setters are harmless CRUD fields.

---

# 34. Financial Tests

Financial calculations require careful coverage.

Test:

* Precision.
* Rounding.
* Zero.
* Negative values when valid/invalid.
* Discounts.
* Taxes.
* Boundaries.

Use `decimal` values.

Avoid floating-point comparisons for financial logic.

---

# 35. Boundary Testing

Important boundaries should include values such as:

```text
Minimum
Maximum
Just below
Exact threshold
Just above
```

Especially useful for:

* Discounts.
* Limits.
* Pagination.
* Validation.
* Approval thresholds.

---

# 36. Null Cases

Test null behavior only where null is a legitimate input or failure risk.

Do not create meaningless null tests for types that cannot legally be null.

---

# 37. Empty Collections

Test empty collection behavior when it changes business/API semantics.

Example:

```text
GET /users
→ 200
→ items = []
```

not necessarily:

```text
404
```

Follow API contract.

---

# 38. Not Found Tests

Resource lookup endpoints should test missing resources.

Example:

```text
GET /api/users/{unknownId}
→ 404
```

where that is the defined contract.

---

# 39. Conflict Tests

Test real conflict scenarios.

Examples:

* Duplicate email.
* Duplicate code.
* Invalid state.
* Concurrency conflict.

Ensure conflict maps to the correct application/API behavior.

---

# 40. Authentication Tests

Important authentication scenarios may include:

```text
Valid credentials
Invalid credentials
Expired token
Invalid token
Disabled account
```

Do not test cryptographic framework internals.

Test application behavior around them.

---

# 41. Authorization Tests

Protected endpoints should verify meaningful permission behavior.

Minimum relevant scenarios:

```text
Unauthenticated
→ 401

Authenticated without permission
→ 403

Authenticated with permission
→ Success
```

---

# 42. Ownership Tests

For owner-scoped resources:

```text
Owner
→ Allowed

Different user
→ Denied
```

This is important protection against IDOR vulnerabilities.

---

# 43. Tenant Isolation Tests

For multi-tenant systems, test cross-tenant access explicitly.

Example:

```text
Tenant A user requests Tenant B resource
→ Must not receive data
```

Tenant isolation failures are critical defects.

---

# 44. API Contract Tests

Verify important HTTP contract behavior:

* Method.
* Route.
* Status.
* JSON shape.
* Required fields.
* Error structure.

Avoid snapshot testing huge responses unless it provides clear value.

---

# 45. Serialization Tests

Test serialization when contract behavior depends on:

* Enum representation.
* Custom converter.
* Date format.
* Null behavior.
* Polymorphism.

Do not test default `System.Text.Json` behavior redundantly.

---

# 46. Pagination Tests

For paginated endpoints, test:

```text
Page size
Page number
Total count
Ordering
Empty page
Maximum page size
```

Ensure paging is deterministic.

---

# 47. Filtering Tests

Test important filters independently and in meaningful combinations.

Do not exhaustively test every mathematically possible filter combination unless risk requires it.

---

# 48. Sorting Tests

Verify supported sorting fields and directions.

Also test unsupported sort fields if the API explicitly rejects them.

---

# 49. Database Constraint Tests

When integrity relies on database constraints, integration tests should verify them where important.

Examples:

* Unique email.
* Foreign key.
* Required relation.
* Composite uniqueness.

---

# 50. Transaction Tests

Test transactional behavior when partial persistence would be harmful.

Example:

```text
Create Order
+
Create Order Items
+
Update Inventory

One operation fails
→ No partial business state
```

---

# 51. Concurrency Tests

Use concurrency tests only where concurrent behavior is a real requirement.

Examples:

* Stock decrement.
* Order approval.
* Duplicate payment.
* RowVersion conflict.

Do not create complex concurrency tests for ordinary static lookup data.

---

# 52. External Integration Tests

For external integrations, determine the appropriate test boundary.

Possible strategies:

```text
Mock external HTTP boundary
Fake provider
Sandbox environment
Contract test
```

Do not make normal test suites depend on unstable public internet services.

---

# 53. HttpClient Testing

For application-level tests, mock or fake `HttpMessageHandler` when appropriate.

Do not mock `HttpClient` incorrectly if handler-based testing is more suitable.

---

# 54. Webhook Tests

Test important webhook behavior:

* Signature valid.
* Signature invalid.
* Duplicate event.
* Invalid payload.
* Successful processing.

Do not bypass signature validation in production code for testing convenience.

---

# 55. File Upload Tests

Test relevant:

* Valid type.
* Invalid type.
* Oversized file.
* Invalid file name.
* Authorization.
* Storage failure.

Avoid huge real test files unless necessary.

---

# 56. Background Job Tests

Separate:

```text
Scheduling
```

from:

```text
Job Business Logic
```

Business logic should usually be testable without waiting for a scheduler.

---

# 57. Cache Tests

Only test caching where cache behavior matters.

Examples:

* Cache hit.
* Cache invalidation.
* Tenant-aware key.
* Expiration-sensitive logic.

Do not write tests for framework internals.

---

# 58. Regression Tests

Whenever a confirmed bug is fixed, add regression coverage when practical.

A good regression test should:

```text
Fail before fix
Pass after fix
```

Name the scenario clearly.

---

# 59. Refactor Protection

Before risky refactoring, use existing tests as a behavior baseline.

If important legacy code has no tests, consider characterization tests before restructuring.

---

# 60. Characterization Tests

Characterization tests capture existing behavior without asserting that the implementation is ideal.

Use when:

* Legacy logic is complex.
* Behavior must be preserved.
* Documentation is incomplete.
* Refactoring risk is high.

Do not permanently preserve behavior known to be incorrect when requirements explicitly change it.

---

# 61. Bug Reproduction

For debugging tasks:

Prefer:

```text
Create failing regression test
       ↓
Confirm FAIL
       ↓
Fix root cause
       ↓
Confirm PASS
```

when feasible.

---

# 62. Do Not Modify Expected Behavior Silently

If a test fails because business requirements changed:

Update:

```text
Implementation
+
Test expectation
```

only when the requirement clearly changed.

Do not simply change expected output to whatever current implementation returns.

---

# 63. Test Failure Investigation

When a test fails, classify:

```text
Production defect
Test defect
Environment defect
Flaky test
Requirement change
Infrastructure failure
```

Do not immediately rewrite the assertion.

---

# 64. Flaky Tests

A flaky test is a defect.

Common causes:

* Time dependency.
* Randomness.
* Shared state.
* Concurrency.
* External dependency.
* Test order.
* Slow timing assumptions.

Fix the cause.

Do not simply add large delays or retries to hide flakiness.

---

# 65. Thread.Sleep

Avoid:

```csharp
Thread.Sleep(...)
```

in tests unless testing timing itself.

Prefer deterministic synchronization.

---

# 66. Retry in Tests

Do not retry assertions just to get intermittent tests green.

Retries may be appropriate when intentionally testing eventually consistent distributed behavior.

Use deliberately.

---

# 67. Performance Tests

Do not mix ordinary unit tests with performance benchmarks.

Use dedicated benchmarking/load testing approaches where required.

Normal tests should validate correctness.

---

# 68. Benchmarking

For micro-performance questions, tools such as BenchmarkDotNet may be appropriate.

Do not add benchmarking infrastructure without a performance requirement.

---

# 69. Load Testing

API load testing is separate from unit/integration testing.

Use when requirements involve:

* Throughput.
* Concurrent users.
* Latency.
* Capacity.

Do not claim scalability based only on unit tests.

---

# 70. Code Coverage

Coverage is a diagnostic metric, not the objective.

Do not optimize solely for:

```text
100% coverage
```

A lower percentage covering critical behavior is more valuable than high coverage of trivial code.

---

# 71. Coverage Priority

Prioritize covering:

```text
Business decisions
Security decisions
Data transformations
Critical workflows
Error scenarios
```

over:

```text
Getters
Setters
Simple mappings
Framework plumbing
```

---

# 72. Mocking Philosophy

Mock external boundaries when isolation helps.

Good mock candidates:

* Email provider.
* Payment gateway.
* External HTTP API.
* Clock when relevant.

Avoid mocking every internal class.

Excessive mocking tightly couples tests to implementation.

---

# 73. Verify Behavior, Not Calls

Prefer:

```text
Expected result
Expected persisted state
Expected external outcome
```

over asserting every intermediate method invocation.

Interaction verification is useful only where the interaction itself is part of behavior.

---

# 74. Test Helpers

Test helper code should remain simple.

Do not build an internal test framework more complicated than production code.

Reuse setup only after meaningful repetition exists.

---

# 75. Test Project Structure

A reasonable structure may be:

```text
tests/
├── Backend.UnitTests/
│   ├── Domain/
│   └── Application/
│
└── Backend.IntegrationTests/
    ├── Api/
    ├── Persistence/
    └── Security/
```

Follow existing repository convention where established.

---

# 76. Test File Naming

Match production responsibility.

Examples:

```text
CreateUserTests.cs
OrderTests.cs
UsersApiTests.cs
UserRepositoryTests.cs
```

Avoid:

```text
TestHelpers2.cs
GeneralTests.cs
BackendTests.cs
```

for unrelated behavior.

---

# 77. Production Code Changes During Testing

Do not alter production architecture merely to satisfy a preferred mocking style.

Production-code changes are acceptable when they improve legitimate design/testability.

Examples:

* Isolating real external dependency.
* Removing hidden global state.
* Separating business logic from framework code.

Do not introduce meaningless interfaces only for tests.

---

# 78. Security Test Data

Never use real:

* Passwords.
* API keys.
* Tokens.
* Production customer data.

Use synthetic test values.

---

# 79. Test Configuration

Testing environment must use isolated configuration.

Do not point automated tests at production databases or production integrations.

---

# 80. Database Safety

Before executing integration tests that modify a database, verify the target is a test environment.

Never intentionally run destructive automated tests against production.

---

# 81. Test Execution

Use targeted tests during development when appropriate.

Example:

```bash
dotnet test --filter FullyQualifiedName~CreateUserTests
```

Then run broader relevant tests before completion.

---

# 82. Build Before Tests

When compilation may be affected, run:

```bash
dotnet build
```

before or as part of test execution.

Do not report tests if the project cannot compile.

---

# 83. Full Test Suite

Run the full relevant suite after significant changes when practical.

Especially:

* Architecture refactor.
* Shared infrastructure change.
* Authentication change.
* Database change.

---

# 84. Existing Failures

If tests already fail before the task:

Do not claim your change caused or fixed them unless verified.

Record baseline when relevant.

Example:

```text
Baseline:
3 tests already failing before modification.

After change:
Same 3 tests remain failing.
```

---

# 85. Do Not Hide Failing Tests

Never delete, skip or disable a legitimate failing test merely to obtain a green build.

If a test must be temporarily skipped, explain why and report it explicitly.

---

# 86. Skip Tests

Use skipped tests sparingly.

Every skipped test should have a concrete reason.

Do not accumulate forgotten skipped tests.

---

# 87. Test Review

Before completing a testing task, review:

* Does each test verify meaningful behavior?
* Are names clear?
* Are tests isolated?
* Are tests deterministic?
* Is mocking excessive?
* Are database tests realistic?
* Are security boundaries covered?
* Are edge cases meaningful?
* Did production behavior change?
* Are any tests flaky?

---

# 88. Test Workflow

When this skill is activated:

## Step 1 — Inspect

Read production code and existing tests.

---

## Step 2 — Identify Behavior

Define exactly what must be verified.

---

## Step 3 — Select Test Level

Choose:

```text
Unit
Application
Integration
API
Persistence
Security
Regression
```

---

## Step 4 — Identify Scenarios

Classify:

```text
Happy Path
Validation
Business Rule
Authorization
Not Found
Conflict
Edge Case
Regression
```

Only include relevant scenarios.

---

## Step 5 — Prepare Test Environment

Reuse existing fixtures/infrastructure where possible.

---

## Step 6 — Write Tests

Keep tests readable and behavior-oriented.

---

## Step 7 — Run Targeted Tests

Execute the smallest relevant test set first.

---

## Step 8 — Fix Test Defects

Fix genuine test implementation problems.

Do not modify production behavior unless it is actually incorrect.

---

## Step 9 — Run Broader Tests

Run related/full suites where appropriate.

---

## Step 10 — Review Coverage

Check whether critical behavior remains untested.

Do not chase arbitrary coverage percentages.

---

## Step 11 — Review Regression Risk

Identify what surrounding behavior may have been affected.

---

## Step 12 — Report

Produce global CHANGE REPORT plus testing-specific details.

---

# 89. Test Task Report

Always include:

```text
## Test Summary

Test Type:
- Unit
- Integration
- API
- Persistence
- Security
- Regression

Tests Added:
...

Tests Modified:
...

Tests Removed:
...

Production Code Modified:
Yes / No

Reason:
...

## Verification

Build:
PASS / FAIL / NOT RUN

Targeted Tests:
PASS / FAIL / NOT RUN

Full Test Suite:
PASS / FAIL / NOT RUN
```

---

# 90. Test Result Count

When available, report exact numbers.

Example:

```text
Targeted Tests:
12 PASS
0 FAIL
0 SKIPPED

Full Suite:
146 PASS
2 FAIL
1 SKIPPED
```

Do not say simply:

```text
All good
```

---

# 91. Failure Report

If tests fail:

Report:

```text
Test:
...

Failure:
...

Likely Cause:
...

Related to Current Task:
YES / NO / UNKNOWN

Status:
UNRESOLVED
```

Do not hide failures from the final report.

---

# 92. Regression Report

For bug fixes:

```text
Regression Scenario:
Duplicate email creation.

Before Fix:
FAIL

After Fix:
PASS

Regression Test:
CreateUser_WithExistingEmail_ReturnsConflict
```

---

# 93. Coverage Report

If code coverage tooling was actually executed:

Report:

```text
Coverage:
XX%
```

and identify meaningful uncovered areas when relevant.

Do not invent coverage values.

---

# 94. No False Claims

Never claim:

```text
Fully tested
100% safe
All edge cases covered
Production ready
```

unless supported by an appropriate verification process.

Use precise statements.

Example:

```text
Unit and integration suites passed.
Load testing and production validation were not performed.
```

---

# 95. Definition of Done

A testing task is complete only when:

1. Target behavior is understood.
2. Appropriate test level is selected.
3. Tests verify behavior rather than implementation details.
4. Important success and failure scenarios are covered.
5. Tests are isolated.
6. Tests are deterministic.
7. Security/data boundaries are tested when relevant.
8. Test environment is safe.
9. Actual tests are executed when possible.
10. Failures are reported accurately.
11. No legitimate tests are weakened merely to pass.
12. Test results are included in CHANGE REPORT.

The objective is:

> Build confidence that important backend behavior remains correct as the system evolves.
