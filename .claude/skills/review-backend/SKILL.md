---

name: review-backend
description: Review an ASP.NET Core backend codebase or module before modification, identify architectural, code quality, database, security, testing, performance, and maintainability issues, prioritize them by risk and impact, and produce an actionable improvement plan without changing code unless explicitly requested.
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

# Review Backend

## 1. Purpose

Use this skill when the task requires:

* Reviewing an entire backend codebase.
* Reviewing a module before refactoring.
* Performing technical debt assessment.
* Reviewing architecture quality.
* Reviewing maintainability.
* Reviewing code quality.
* Reviewing scalability readiness.
* Reviewing database usage.
* Reviewing security risks.
* Reviewing test coverage and testability.
* Reviewing performance risks.
* Preparing a refactoring roadmap.
* Assessing whether existing code follows project standards.

Default behavior:

> Review first. Do not modify code unless the task explicitly asks for implementation.

---

# 2. Required Rules

Always evaluate the codebase against:

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

Do not mechanically report every deviation as a defect.

Existing architecture may intentionally differ from preferred conventions.

---

# 3. Primary Objective

Produce a review that helps a developer understand:

```text
What is good?
What is risky?
What is inconsistent?
What should be fixed first?
What can wait?
What should not be changed?
```

The review must be actionable.

Avoid generic comments such as:

```text
Code could be cleaner.
Architecture could be improved.
More tests are recommended.
```

Every meaningful finding should identify:

* Location.
* Problem.
* Impact.
* Priority.
* Recommended action.

---

# 4. Review Modes

Determine the review mode first.

## Mode A — Full Codebase Review

Review:

```text
Architecture
Code Quality
API
Database
Security
Testing
Performance
Dependencies
Configuration
Technical Debt
```

Use when evaluating the backend broadly.

---

## Mode B — Module Review

Focus on a specific module.

Example:

```text
Users
Orders
Identity
Reporting
```

Do not report unrelated repository-wide issues unless they materially affect the module.

---

## Mode C — Architecture Review

Focus on:

* Layer boundaries.
* Dependency direction.
* Modules.
* Coupling.
* Abstraction.
* Scalability.

---

## Mode D — Code Quality Review

Focus on:

* Responsibilities.
* Naming.
* Duplication.
* Complexity.
* Dead code.
* Maintainability.

---

## Mode E — Pre-Refactor Review

Use before major refactoring.

Goal:

> Establish current state, risk and recommended refactor sequence.

---

## Mode F — Production Readiness Review

Focus on:

* Security.
* Error handling.
* Configuration.
* Logging.
* Database safety.
* Tests.
* Deployment concerns.
* Operational risks.

Do not call a system "production ready" based solely on source review.

---

# 5. Inspect Repository Structure

Start by understanding the repository.

Inspect:

```text
Solution
Projects
Folders
Modules
Tests
Configuration
Migrations
Build files
Deployment files
```

Determine:

* Architectural style.
* Project responsibilities.
* Dependency direction.
* Main entry point.
* Core business modules.
* Infrastructure components.

Do not review files in isolation without understanding the surrounding structure.

---

# 6. Identify Technology Stack

Record relevant stack.

Example:

```text
.NET:
ASP.NET Core:
EF Core:
Database:
Authentication:
Validation:
Testing:
Caching:
Messaging:
Background Jobs:
```

Only include components actually present.

---

# 7. Understand Existing Architecture

Before judging architecture, determine what architecture already exists.

Possible examples:

```text
Simple Layered Architecture
Clean Architecture
Vertical Slice
Modular Monolith
MVC Service Repository
CQRS
Hybrid
```

Do not force a preferred architecture onto the repository.

---

# 8. Architecture Review

Evaluate:

* Responsibility boundaries.
* Layer dependencies.
* Circular dependencies.
* Infrastructure leakage.
* Business logic location.
* Module boundaries.
* Dependency inversion.
* Excessive abstraction.
* Missing abstraction where needed.

Good architecture should make change safer.

---

# 9. Dependency Direction

Inspect project references.

Preferred conceptual direction:

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

Flag harmful dependencies such as:

```text
Domain → Infrastructure
Domain → API
Application → API
```

only when they actually exist.

---

# 10. Circular Dependencies

Look for:

* Circular project references.
* Circular service dependencies.
* Modules directly depending on each other in both directions.

Do not solve circularity by recommending a generic `Common` dumping ground.

Identify the correct ownership boundary.

---

# 11. Module Boundaries

Evaluate whether business capabilities have clear ownership.

Example:

```text
Identity
Users
Organizations
Orders
Reporting
```

Look for:

* Cross-module database manipulation.
* Shared business rules copied between modules.
* Internal classes used externally.
* Tight coupling.

---

# 12. Fat Controllers

Identify controllers/endpoints containing:

* EF Core queries.
* Business calculations.
* Workflow logic.
* Large validation blocks.
* External integrations.

Recommended direction:

```text
Endpoint
  ↓
Application Use Case
```

Do not flag simple HTTP-specific logic unnecessarily.

---

# 13. Large Services

Review services with:

* Many unrelated methods.
* Many constructor dependencies.
* Multiple business capabilities.
* Mixed infrastructure/business concerns.

Potential indicators:

```text
UserService with 30 methods
SystemService
CommonService
ManagementService
```

Do not split services solely based on file length.

---

# 14. Responsibility Review

For each significant class ask:

> Does this class have one cohesive reason to change?

Identify classes combining:

```text
Validation
Persistence
Mapping
Email
Business rules
Logging
```

when those responsibilities are unrelated.

---

# 15. Code Complexity

Look for:

* Deep nesting.
* Large methods.
* Complex boolean expressions.
* Repeated branching.
* Hidden side effects.
* Excessive parameter count.

Report complexity where it meaningfully affects maintainability or correctness.

---

# 16. Duplication

Differentiate between:

```text
Duplicate Code
```

and:

```text
Duplicate Business Knowledge
```

Prioritize duplicated business rules more highly.

Do not recommend abstraction solely to remove a few repeated lines.

---

# 17. Naming Review

Evaluate significant names against project conventions.

Look for unclear names:

```text
Helper
Common
Util
Manager
Data
Info
Process
Handle
DoWork
```

Only report names that meaningfully reduce understanding.

Do not generate a massive list of minor variable renames unless requested.

---

# 18. Dead Code Review

Look for possible:

* Unused classes.
* Unused methods.
* Unused packages.
* Commented-out code.
* Duplicate implementations.
* Legacy endpoints.

Mark as:

```text
POTENTIALLY UNUSED
```

until usage has been verified.

Do not recommend deletion solely from visual inspection.

---

# 19. Abstraction Review

Identify over-engineering such as:

```text
IGenericRepository<T>
GenericService<T>
BaseController<T>
BaseHandler<T>
CommonManager
```

when abstraction adds complexity without meaningful business value.

Also identify missing abstractions around real external boundaries when applicable.

---

# 20. Design Pattern Review

Do not judge quality by number of design patterns.

Evaluate whether patterns solve actual problems.

Possible patterns:

```text
Strategy
Factory
Adapter
Decorator
Repository
Mediator
Specification
```

Report:

```text
Useful
Unnecessary
Missing where clearly beneficial
```

Do not recommend patterns merely because they are common.

---

# 21. SOLID Review

Apply SOLID pragmatically.

Focus primarily on:

```text
SRP
Dependency direction
Interface usefulness
Extension safety
```

Avoid academic pattern scoring.

---

# 22. API Review

Review:

* Routes.
* HTTP methods.
* DTO boundaries.
* Response consistency.
* Error handling.
* Pagination.
* Filtering.
* Authentication.
* Authorization.

Identify inconsistent public contracts.

---

# 23. API Entity Exposure

Check whether EF Core entities are returned directly.

Potential risks:

* Sensitive data exposure.
* Persistence coupling.
* Serialization problems.
* Breaking contracts during schema changes.

Recommend explicit response contracts where appropriate.

---

# 24. API Error Handling

Review:

* Central exception handling.
* ProblemDetails.
* Validation errors.
* Status codes.
* Internal error leakage.

Flag duplicated controller-level `try/catch` when centralized handling would be safer.

---

# 25. Pagination Review

Check collection endpoints that may grow.

Potential issue:

```text
GET /api/users
→ SELECT all users
```

Recommend pagination only where dataset growth makes it relevant.

---

# 26. Authentication Review

Review high-level authentication architecture.

Check:

* Standard framework usage.
* Token validation.
* Password handling.
* Session lifecycle.

Use the dedicated security skill for deep security audits.

---

# 27. Authorization Review

Look for:

* Missing endpoint authorization.
* Hard-coded role checks.
* Inconsistent permissions.
* Ownership gaps.
* Tenant gaps.

Prioritize access-control problems highly.

---

# 28. Database Review

Inspect:

* Entities.
* Relationships.
* Constraints.
* Indexes.
* Query behavior.
* Transactions.
* Migrations.
* Concurrency.

Do not redesign the schema during a review unless explicitly asked.

---

# 29. Entity Review

Check whether entities contain:

* Correct required/optional fields.
* Meaningful relationships.
* Appropriate financial types.
* Clear status modeling.

Avoid purely stylistic schema criticism.

---

# 30. Financial Fields

Check financial data uses appropriate:

```text
decimal
precision
scale
```

Flag use of:

```text
float
double
```

for money when found.

---

# 31. Query Review

Look for:

* N+1 queries.
* Premature materialization.
* Unbounded queries.
* Excessive Includes.
* Missing AsNoTracking for heavy read paths.
* Repeated database calls.

Do not report theoretical performance problems without context where possible.

---

# 32. Index Review

Identify likely missing index opportunities only when query patterns support the recommendation.

Do not recommend indexes for every filterable column.

---

# 33. Migration Review

Look for risky migration patterns:

```text
Drop
Alter
Rename
Required-column addition
```

Review whether migrations appear controlled and incremental.

---

# 34. Transaction Review

Identify multi-write workflows that may leave partial state.

Do not recommend explicit transactions for every `SaveChanges`.

---

# 35. Concurrency Review

Identify modules where concurrent operations could create correctness issues.

Examples:

* Inventory.
* Payments.
* Approvals.
* Duplicate operations.

Do not introduce concurrency complexity without a realistic race condition.

---

# 36. Security Review

Perform a baseline security scan for obvious issues:

* Hard-coded secrets.
* SQL injection.
* Missing authorization.
* Sensitive logging.
* Unsafe file paths.
* Direct entity exposure.
* Weak tenant isolation.

Use `review-security` for comprehensive security audits.

---

# 37. Secret Review

Look for suspicious configuration patterns.

Do not include actual secret values in reports.

Report:

```text
File
Configuration key
Risk
```

only.

---

# 38. Input Validation

Review whether external inputs have appropriate validation.

Separate:

```text
Input validation
```

from:

```text
Business validation
```

Do not recommend duplicate validation in every layer.

---

# 39. File Handling

If present, inspect:

* Upload limits.
* Client file names.
* Storage paths.
* Authorization.
* Download ownership.

Prioritize arbitrary file-access risks.

---

# 40. Testing Review

Inspect:

* Unit tests.
* Integration tests.
* API tests.
* Regression tests.
* Security tests.

Evaluate coverage quality rather than percentage alone.

---

# 41. Critical Test Gaps

Prioritize missing tests around:

```text
Business calculations
Permissions
State transitions
Database constraints
Authentication
Critical workflows
Bug-prone code
```

Do not recommend tests for every getter and setter.

---

# 42. Test Quality

Look for:

* Implementation-coupled tests.
* Excessive mocks.
* Flaky timing.
* Shared mutable state.
* Weak assertions.
* Disabled tests.

Report only meaningful test-quality problems.

---

# 43. Regression Readiness

Ask:

> Can this codebase be refactored safely?

If critical modules have no automated behavioral protection, report increased refactoring risk.

---

# 44. Performance Review

Review obvious high-impact areas:

* Database queries.
* External calls.
* Blocking async.
* Unbounded datasets.
* Large payloads.
* Memory-heavy imports.

Do not perform micro-optimization review unless requested.

---

# 45. Async Review

Search for patterns such as:

```text
.Result
.Wait()
async void
Task.Run
```

inside ASP.NET Core request flows.

Evaluate context before reporting.

---

# 46. CancellationToken Review

Check whether cancellation is propagated through important async I/O.

Treat it as maintainability/scalability improvement, not always a severe defect.

---

# 47. External Integrations

Review:

* Typed clients.
* HttpClient usage.
* Timeout.
* Retry.
* Error handling.
* Credentials.

Identify integrations tightly coupled to application logic.

---

# 48. Configuration Review

Inspect:

* Strongly typed options.
* Environment-specific values.
* Hard-coded configuration.
* Secret handling.
* Missing validation.

Do not recommend abstractions for every simple configuration value.

---

# 49. Logging Review

Evaluate whether logs are:

* Structured.
* Useful.
* Safe.
* Excessive.

Flag:

* Sensitive data logging.
* Missing useful failure context.
* `Console.WriteLine` in backend code.

---

# 50. Exception Handling

Look for:

```text
catch (Exception)
{
}
```

and unnecessary repeated `try/catch`.

Distinguish between:

* Expected business errors.
* Infrastructure errors.
* Unexpected failures.

---

# 51. Dependency Review

Inspect NuGet dependencies.

Identify:

* Duplicate packages.
* Unused packages.
* Overlapping functionality.
* Major infrastructure dependencies.
* Deprecated libraries where evident.

Do not recommend package upgrades merely because newer versions may exist unless the task includes upgrade analysis.

---

# 52. Framework Reuse

Identify custom implementations of functionality already provided safely by ASP.NET Core/.NET.

Examples may include:

* Password hashing.
* Dependency injection.
* HTTP clients.
* ProblemDetails.
* Rate limiting.

Do not recommend replacing legitimate domain-specific implementations with framework APIs that solve different problems.

---

# 53. Maintainability Review

Evaluate whether a developer can easily answer:

```text
Where is this feature?
Where is the business rule?
Where is validation?
Where is persistence?
Where is authorization?
Where is its test?
```

If responsibilities are difficult to locate, explain why.

---

# 54. Changeability Review

Ask:

> If a business rule changes tomorrow, how many unrelated files/modules must be modified?

High change dispersion may indicate poor cohesion or duplicated business knowledge.

---

# 55. Debuggability Review

Evaluate:

* Clear exceptions.
* Structured logs.
* Correlation support where needed.
* Hidden side effects.
* Complex magic behavior.

Do not recommend heavy observability infrastructure unless project scale warrants it.

---

# 56. Scalability Readiness

Evaluate basic scalability foundations:

```text
Stateless request handling
Bounded queries
Async I/O
Database efficiency
Clear module boundaries
Externalized configuration
```

Do not judge absence of microservices as a scalability defect.

---

# 57. Over-Engineering Review

Explicitly identify unnecessary complexity.

Examples:

* Excessive projects.
* Interfaces for every class.
* Generic repositories.
* Pattern stacking.
* Premature messaging.
* Premature distributed cache.

Technical sophistication is not automatically good architecture.

---

# 58. Under-Engineering Review

Also identify insufficient structure.

Examples:

```text
Huge Controllers
Huge Services
Business logic everywhere
No validation boundary
No database constraints
No tests around critical rules
```

The goal is balance.

---

# 59. Technical Debt Classification

Classify technical debt by type:

```text
ARCHITECTURE
CODE_QUALITY
NAMING
API
DATABASE
SECURITY
TESTING
PERFORMANCE
DEPENDENCY
CONFIGURATION
OBSERVABILITY
```

This makes remediation easier to organize.

---

# 60. Severity

Classify each finding:

```text
CRITICAL
HIGH
MEDIUM
LOW
INFO
```

Use severity for risk, not implementation effort.

Example:

```text
CRITICAL
Cross-tenant data access.

LOW
Inconsistent private variable naming.
```

---

# 61. Priority

Also classify implementation priority:

```text
P0 — Fix immediately
P1 — High priority
P2 — Planned improvement
P3 — Optional cleanup
```

Severity and priority may differ.

---

# 62. Effort

Estimate relative effort when useful:

```text
S
M
L
XL
```

Do not provide false exact hour estimates unless requested.

---

# 63. Confidence

For findings that are not fully verified, include:

```text
HIGH
MEDIUM
LOW
```

Example:

```text
Confidence: MEDIUM
```

because runtime usage could not be confirmed.

---

# 64. Finding Format

Every important finding should contain:

```text
ID
Category
Severity
Priority
Location
Problem
Impact
Recommendation
Confidence
```

Example:

```text
ID:
ARCH-03

Category:
ARCHITECTURE

Severity:
MEDIUM

Priority:
P1

Location:
UsersController

Problem:
Controller directly performs EF Core queries and business validation.

Impact:
Business rules are tightly coupled to HTTP layer and difficult to test.

Recommendation:
Move use-case logic into Application layer while preserving API contract.

Confidence:
HIGH
```

---

# 65. Do Not Flood the Report

Do not return hundreds of low-value findings.

Group repetitive problems.

Example:

Instead of:

```text
42 separate unused using statements
```

report:

```text
LOW — Multiple files contain unused imports.
```

unless detailed file-by-file cleanup was requested.

---

# 66. Positive Findings

A good review should also identify what should remain.

Example:

```text
KEEP:
Centralized ProblemDetails implementation is consistent and should be preserved.
```

This prevents unnecessary rewrites.

---

# 67. Preserve Good Existing Design

Do not recommend replacing working components only because another design is personally preferred.

Explicitly identify stable components that should not be touched during refactoring.

---

# 68. Root Problems vs Symptoms

Group related findings around root causes.

Example:

```text
Symptoms:
- Huge controller
- Duplicate validation
- Difficult testing

Root issue:
Missing application use-case boundary.
```

Prioritize fixing the root architecture problem.

---

# 69. Refactoring Candidate Identification

Identify safe refactor candidates.

Possible:

```text
Large service
Duplicated business rule
Misplaced logic
Dead code
Ambiguous naming
High coupling
```

Separate low-risk cleanup from high-risk architecture changes.

---

# 70. Do Not Modify During Review

Default behavior:

```text
READ
ANALYZE
REPORT
```

not:

```text
READ
REWRITE
REPORT
```

Only modify code if user explicitly asks for review + implementation.

---

# 71. Review Before Full Refactor

If user asks:

> Refactor entire codebase.

Use this review process first.

Recommended flow:

```text
Audit
 ↓
Prioritize
 ↓
Create Refactor Plan
 ↓
Refactor Module-by-Module
```

Do not rewrite entire repository in one pass.

---

# 72. Refactor Roadmap

When broad technical debt exists, produce stages.

Example:

```text
Phase 1 — Critical Safety
- Authorization
- Data integrity
- Runtime defects

Phase 2 — Architecture
- Controller responsibilities
- Module boundaries

Phase 3 — Code Quality
- Duplication
- Naming
- Dead code

Phase 4 — Testing
- Critical regression coverage

Phase 5 — Performance
- Measured bottlenecks
```

Prioritize correctness and risk first.

---

# 73. Quick Wins

Identify low-risk/high-value improvements separately.

Example:

```text
QUICK WIN
- Remove confirmed unused dependency.
- Propagate CancellationToken in existing query.
- Replace repeated magic status with existing enum.
```

Do not confuse quick wins with architectural priorities.

---

# 74. High-Risk Changes

Mark changes that should not be mixed casually with other work.

Examples:

* Authentication redesign.
* Database key changes.
* Module restructuring.
* Public API rename.
* Migration history rewrite.

Recommend dedicated tasks.

---

# 75. Recommended Skill Mapping

For each remediation category, indicate the appropriate workflow where useful.

Example:

```text
ARCH-01
→ refactor-backend

DB-02
→ design-database

SEC-01
→ review-security

PERF-01
→ optimize-performance

UPG-01
→ upgrade-backend
```

This allows the codebase review to become an execution roadmap.

---

# 76. Review Workflow

When this skill is activated:

## Step 1 — Determine Scope

Identify repository/module review boundary.

---

## Step 2 — Inspect Structure

Understand projects, modules and dependencies.

---

## Step 3 — Understand Architecture

Identify existing architectural approach.

---

## Step 4 — Review Architecture

Check responsibilities and dependency direction.

---

## Step 5 — Review Code Quality

Check complexity, duplication and naming.

---

## Step 6 — Review API

Check contracts and HTTP concerns.

---

## Step 7 — Review Database

Check persistence and data integrity.

---

## Step 8 — Review Security

Perform baseline security assessment.

---

## Step 9 — Review Testing

Evaluate regression protection.

---

## Step 10 — Review Performance

Identify obvious high-impact risks.

---

## Step 11 — Review Dependencies / Configuration

Check packages and environment handling.

---

## Step 12 — Consolidate Findings

Group duplicate symptoms.

---

## Step 13 — Prioritize

Assign:

```text
Severity
Priority
Effort
Confidence
```

where useful.

---

## Step 14 — Identify What Should Stay

Protect good existing design.

---

## Step 15 — Build Roadmap

Order remediation safely.

---

## Step 16 — Report

Produce structured Backend Review Report.

---

# 77. Review Summary Format

Use:

```text
## Backend Review Summary

Scope:
...

Architecture:
...

Overall Health:
GOOD / ACCEPTABLE / NEEDS IMPROVEMENT / HIGH RISK

Critical Findings:
X

High Findings:
X

Medium Findings:
X

Low Findings:
X
```

Only use actual reviewed findings.

---

# 78. Executive Summary

Provide a short summary answering:

```text
1. Is the codebase currently maintainable?
2. What are the highest risks?
3. What should be fixed first?
4. Is a full rewrite necessary?
```

Avoid unnecessary technical detail in this section.

---

# 79. Finding Table

Recommended format:

```text
ID       Category       Severity   Priority   Effort
ARCH-01  Architecture   HIGH       P1         M
SEC-01   Security       HIGH       P0         S
DB-01    Database       MEDIUM     P1         M
CODE-01  Code Quality   LOW        P2         S
```

Then explain important findings below.

---

# 80. Strengths Section

Include:

```text
## What Should Be Preserved
```

Examples:

* Good module boundary.
* Consistent API contract.
* Effective centralized exception handling.
* Good integration-test infrastructure.

This prevents destructive over-refactoring.

---

# 81. Technical Debt Summary

Group findings:

```text
Architecture
Code Quality
Database
Security
Testing
Performance
```

Include counts or major themes.

---

# 82. Refactor Recommendation

Conclude with one of:

```text
NO REFACTOR REQUIRED

TARGETED REFACTOR

MODULE-LEVEL REFACTOR

INCREMENTAL CODEBASE REFACTOR

MAJOR ARCHITECTURE MIGRATION
```

Do not recommend full rewrite unless evidence clearly supports it.

---

# 83. Rewrite Decision

A full rewrite is usually NOT recommended.

Consider it only when:

* Architecture fundamentally blocks requirements.
* System is unmaintainable.
* Security/integrity cannot be repaired incrementally.
* Technology is unsupported and migration cost exceeds replacement.
* Existing code has insufficient business value to preserve.

Even then, explain migration risk.

---

# 84. Recommended Execution Order

Example:

```text
1. Fix security risks.
2. Protect critical behavior with tests.
3. Correct architecture boundaries.
4. Remove duplicated business logic.
5. Normalize naming.
6. Remove dead code.
7. Optimize measured performance problems.
8. Upgrade dependencies.
```

Adapt to actual findings.

---

# 85. Review Report Must Not Claim Verification

Do not say:

```text
Everything works.
No security issues.
No bugs.
Production ready.
```

based only on source inspection.

Use precise language.

Example:

```text
No critical issue was identified in the reviewed source scope.
Runtime penetration testing was not performed.
```

---

# 86. Review-Only Change Report

If no source code is changed, explicitly report:

```text
Source Code Changes:
NONE

Database Changes:
NONE

API Changes:
NONE

Security Changes:
NONE
```

This makes it clear the task was analysis only.

---

# 87. Implementation Mode

If the user explicitly asks to review AND fix:

Do not fix every finding automatically.

Prioritize:

```text
Critical
High
Explicit scope
```

For large findings, recommend separate tasks and appropriate skills.

---

# 88. Definition of Done

A backend review is complete only when:

1. Review scope is defined.
2. Architecture is understood before judgment.
3. Major responsibilities are inspected.
4. Important API/database/security/testing risks are evaluated.
5. Findings are evidence-based.
6. Findings are prioritized.
7. Duplicate symptoms are consolidated.
8. Existing good architecture is identified.
9. High-risk changes are separated from low-risk cleanup.
10. A practical remediation roadmap is provided.
11. No unrequested code changes are made.
12. Review limitations are stated honestly.

The objective is:

> Understand the codebase first, identify what matters, and create a safe path for improvement before changing it.
