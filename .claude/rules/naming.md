# Naming Convention Rules

## 1. Purpose

These rules define naming conventions for the ASP.NET Core backend.

All names must be:

* Clear.
* Consistent.
* Concise.
* Meaningful.
* Easy to search.
* Easy to understand without opening the implementation.

Prefer clarity over clever abbreviations.

---

# 2. General Naming Principles

A name must describe:

> What the code represents or what it does.

Avoid names based on implementation details when business intent is more meaningful.

Prefer:

```csharp
CreateOrderAsync()
CalculateInvoiceTotal()
FindUserByEmailAsync()
```

Avoid:

```csharp
ProcessData()
HandleLogic()
ExecuteTask()
DoSomething()
Run()
```

---

# 3. Language

Use English for all source-code identifiers.

This includes:

* Files.
* Classes.
* Methods.
* Properties.
* Variables.
* Interfaces.
* Enums.
* Constants.
* DTOs.
* Endpoints.
* Database objects where project convention permits.

Do not mix Vietnamese and English identifiers.

---

# 4. PascalCase

Use `PascalCase` for:

* Classes.
* Records.
* Structs.
* Interfaces.
* Enums.
* Enum members.
* Public methods.
* Public properties.
* Public constants.

Example:

```csharp
public class UserService
{
    public async Task<UserDto> GetUserAsync()
    {
    }
}
```

---

# 5. camelCase

Use `camelCase` for:

* Local variables.
* Parameters.
* Private method parameters.
* Lambda variables.

Example:

```csharp
var activeUsers = users.Where(user => user.IsActive);
```

---

# 6. Private Fields

Use underscore-prefixed camelCase:

```csharp
private readonly IUserRepository _userRepository;
private readonly ILogger<UserService> _logger;
```

Do not use:

```csharp
m_userRepository
user_repository
UserRepositoryField
```

---

# 7. Interfaces

Use the `I` prefix for interfaces.

Example:

```csharp
IUserRepository
IEmailSender
ICurrentUser
IPasswordHasher
```

Do not create an interface only because a class exists.

The interface must represent a meaningful abstraction or contract.

---

# 8. Async Methods

Methods performing asynchronous work must use the `Async` suffix.

Example:

```csharp
GetUserAsync()
CreateOrderAsync()
SaveChangesAsync()
SendEmailAsync()
```

Avoid:

```csharp
GetUser()
```

when the implementation returns `Task`.

---

# 9. Boolean Naming

Boolean names should clearly indicate true/false meaning.

Prefer prefixes such as:

```text
Is
Has
Can
Should
Requires
Exists
Supports
```

Examples:

```csharp
IsActive
HasPermission
CanApprove
ShouldRetry
RequiresApproval
Exists
```

Avoid:

```csharp
Status
Flag
Check
Value
```

for boolean variables.

---

# 10. Collection Naming

Collections should use plural nouns.

Prefer:

```csharp
users
orders
permissions
roleIds
productItems
```

Avoid:

```csharp
userList
orderArray
dataCollection
```

unless the collection type itself is important to the behavior.

---

# 11. Identifier Naming

Use entity-specific identifier names.

Prefer:

```csharp
userId
orderId
productId
roleId
```

Avoid:

```csharp
id
entityId
objectId
itemId
```

when multiple identifiers exist in the same scope.

A plain `id` is acceptable only when the context is unambiguous.

---

# 12. Method Naming

Methods should use verbs or verb phrases.

Examples:

```csharp
CreateUserAsync
UpdateProfileAsync
DeleteOrderAsync
CalculateTotal
ValidateRequest
AssignRoleAsync
GenerateToken
```

Avoid noun-only method names.

---

# 13. Query Method Naming

Read operations should clearly describe what they retrieve.

Prefer:

```csharp
GetUserByIdAsync
FindUserByEmailAsync
GetActiveUsersAsync
ListOrdersAsync
ExistsByEmailAsync
```

Use naming consistently:

* `Get` when an expected resource should exist.
* `Find` when absence is valid.
* `List` when returning multiple items.
* `Exists` when returning a boolean.

Do not mix these meanings arbitrarily.

---

# 14. Command Naming

Commands and write operations should clearly describe intent.

Examples:

```text
CreateUser
UpdateUserProfile
DeleteUser
AssignRole
ResetPassword
ApproveOrder
CancelOrder
```

Avoid:

```text
UserAction
UserProcess
UserOperation
UserHandler
```

without meaningful context.

---

# 15. Class Naming

Classes should be nouns or noun phrases representing their responsibility.

Good:

```text
UserRepository
OrderValidator
InvoiceCalculator
TokenGenerator
EmailSender
PasswordHasher
```

Avoid:

```text
UserHelper
CommonHelper
UtilityManager
GeneralService
BaseProcessor
DataHandler
```

---

# 16. Service Naming

Use `Service` only when the class represents a cohesive application or domain capability.

Acceptable:

```text
PaymentService
PricingService
AuthenticationService
```

Avoid creating:

```text
CommonService
GeneralService
UtilityService
SystemService
DataService
```

as dumping grounds.

When possible, prefer responsibility-specific names.

---

# 17. Repository Naming

If repositories are justified, use:

```text
IUserRepository
UserRepository
IOrderRepository
OrderRepository
```

Repository names must correspond to meaningful aggregate/domain concepts.

Avoid:

```text
GenericRepository
BaseRepository
CommonRepository
DataRepository
```

unless explicitly required.

---

# 18. Controller Naming

Use resource-oriented names.

Examples:

```text
UsersController
OrdersController
ProductsController
AuthenticationController
```

Avoid:

```text
UserManagementController
UserOperationsController
UserApiController
```

unless the distinction is necessary.

---

# 19. Endpoint Naming

Endpoints should represent business actions clearly.

Examples:

```text
CreateUser
GetUser
UpdateUser
DeleteUser
AssignRole
ResetPassword
```

Avoid implementation-oriented names such as:

```text
ProcessUser
HandleUserRequest
ExecuteUserAction
```

---

# 20. DTO Naming

DTO names should reveal their role.

Prefer:

```text
CreateUserRequest
UpdateUserRequest
UserResponse
UserSummaryResponse
UserDetailsResponse
```

Avoid:

```text
UserDto1
UserDto2
UserModel
UserData
UserInfo
```

unless the meaning is truly appropriate.

---

# 21. Request and Response Models

Use clear suffixes:

```text
Request
Response
```

Examples:

```text
LoginRequest
LoginResponse
CreateOrderRequest
OrderDetailsResponse
```

Do not use the same DTO for unrelated request and response purposes.

---

# 22. Command and Query Naming

When using CQRS-style patterns:

Commands:

```text
CreateUserCommand
UpdateUserCommand
DeleteUserCommand
AssignRoleCommand
```

Queries:

```text
GetUserQuery
GetUserByEmailQuery
ListUsersQuery
```

Handlers:

```text
CreateUserCommandHandler
GetUserQueryHandler
```

Do not abbreviate to unclear forms such as:

```text
CUCmd
GUQry
UsrCmdHdlr
```

---

# 23. Validator Naming

Use the model or command name plus `Validator`.

Examples:

```text
CreateUserRequestValidator
CreateUserCommandValidator
LoginRequestValidator
```

Avoid:

```text
UserValidator
CommonValidator
InputValidator
```

when the validation scope is ambiguous.

---

# 24. Mapping Naming

Use clear mapper names when mapping logic requires a dedicated component.

Examples:

```text
UserMapper
OrderMapper
ProductMapper
```

Mapping methods may use:

```text
ToEntity
ToDto
ToResponse
ToDomain
```

Do not hide business logic inside mapping methods.

---

# 25. Exception Naming

Custom exceptions should end with `Exception`.

Examples:

```text
DomainException
BusinessRuleException
NotFoundException
ConflictException
```

Avoid unnecessary custom exceptions when built-in exceptions are sufficient.

---

# 26. Enum Naming

Enum type names should be singular.

Good:

```csharp
OrderStatus
UserStatus
PaymentMethod
```

Avoid:

```csharp
OrderStatuses
UserStatuses
PaymentMethods
```

Enum members should use PascalCase:

```csharp
Pending
Approved
Rejected
Cancelled
```

---

# 27. Constant Naming

Use PascalCase for constants.

Example:

```csharp
public const int DefaultPageSize = 20;
public const string AdminRole = "Admin";
```

Avoid:

```csharp
DEFAULT_PAGE_SIZE
ADMIN_ROLE
```

unless the existing project explicitly follows another convention.

---

# 28. Configuration Naming

Configuration classes should use meaningful names.

Examples:

```text
JwtOptions
DatabaseOptions
EmailOptions
StorageOptions
```

Avoid:

```text
Config
Settings
AppConfig
GeneralSettings
```

when the configuration represents a specific concern.

---

# 29. File Naming

Normally, one primary type should correspond to one file.

Use the same name for file and primary type.

Example:

```text
UserService.cs
UserRepository.cs
CreateUserRequest.cs
UserController.cs
```

Do not use:

```text
user-service.cs
user_service.cs
UserServices.cs
```

for a class named `UserService`.

---

# 30. Folder Naming

Use PascalCase for source folders unless the existing repository has a different established convention.

Examples:

```text
Domain
Application
Infrastructure
Controllers
Features
Modules
Common
```

Business module names should also use PascalCase:

```text
Users
Identity
Orders
Products
Reporting
```

---

# 31. Namespace Naming

Namespaces should reflect project structure.

Example:

```text
Company.Project.Domain.Users
Company.Project.Application.Users
Company.Project.Infrastructure.Persistence
```

Avoid excessively deep namespace hierarchies.

Do not include meaningless technical folder names solely because the physical directory exists.

---

# 32. Abbreviation Rules

Avoid abbreviations unless they are widely understood in the domain or technology.

Acceptable examples:

```text
Id
Dto
Api
Url
Http
Jwt
Sql
Csv
Pdf
```

Avoid:

```text
Usr
Mgr
Svc
RepoImpl
Proc
Hdlr
Cfg
Util
```

if the full name is clearer.

---

# 33. Acronym Casing

Follow .NET naming conventions.

Prefer:

```text
ApiClient
HttpClient
JwtToken
UrlBuilder
SqlQuery
```

Avoid:

```text
APIClient
HTTPClient
JWTToken
URLBuilder
SQLQuery
```

unless matching an existing external contract.

---

# 34. Avoid Redundant Context

Do not repeat unnecessary context already provided by the containing type or namespace.

Example inside:

```csharp
class UserService
```

Prefer:

```csharp
CreateAsync()
UpdateAsync()
DeactivateAsync()
```

over:

```csharp
CreateUserAsync()
UpdateUserAsync()
DeactivateUserAsync()
```

when the shorter name remains completely clear.

However, do not shorten names to the point of ambiguity.

---

# 35. Avoid Type Encoding

Do not encode data types in names.

Avoid:

```text
strName
intAge
lstUsers
dictValues
boolActive
```

Prefer:

```text
name
age
users
values
isActive
```

---

# 36. Avoid Generic Names

Avoid generic names such as:

```text
data
item
object
value
temp
result
info
model
entity
manager
processor
handler
helper
```

unless the context makes the meaning obvious.

Prefer domain-specific names.

Example:

Instead of:

```csharp
var data = await repository.GetAsync();
```

prefer:

```csharp
var activeUsers = await repository.GetActiveUsersAsync();
```

---

# 37. Result Naming

Use `result` only when the returned value has no more meaningful semantic name.

Prefer:

```csharp
var user = ...
var order = ...
var token = ...
var validationResult = ...
```

instead of:

```csharp
var result = ...
```

for every operation.

---

# 38. Temporary Variables

Temporary variables should still reveal intent.

Avoid:

```csharp
var temp = ...
var x = ...
var val = ...
```

Prefer:

```csharp
var normalizedEmail = ...
var calculatedTotal = ...
var existingUser = ...
```

Short names such as `i`, `j`, `x` are acceptable only in small, conventional scopes where meaning is obvious.

---

# 39. Naming Business Concepts

Use terminology consistent with the business domain.

If the project uses:

```text
Customer
```

do not arbitrarily introduce:

```text
Client
Buyer
Consumer
```

for the same concept.

Maintain a consistent ubiquitous language across:

* Code.
* APIs.
* Database.
* Documentation.

When terminology is unclear, inspect existing project usage before introducing a new term.

---

# 40. Rename Safety

Before renaming:

* Files.
* Classes.
* Methods.
* Properties.
* DTOs.
* Endpoints.
* Database fields.

check usages and impact.

Search for:

* Compile-time references.
* Reflection.
* Serialization.
* Dependency injection.
* Configuration.
* API consumers.
* Tests.
* Database mappings.

Do not assume rename is safe only because the compiler succeeds.

---

# 41. Public Contract Renaming

Renaming internal code is generally safe when references are updated.

Renaming public contract fields may be breaking.

Examples:

```text
JSON property names
Route paths
Query parameters
Headers
Public DTO fields
Message contract fields
```

Do not rename public contracts during a naming cleanup unless explicitly requested.

---

# 42. Database Naming

Database-specific naming rules are defined in `database.md`.

General principle:

Database names must remain:

* Stable.
* Predictable.
* Consistent.
* Migration-safe.

Do not rename tables or columns merely to improve code style without evaluating migration impact.

---

# 43. Refactoring Existing Names

When performing naming cleanup:

1. Identify unclear names.
2. Determine their real responsibility.
3. Rename to business-oriented terminology.
4. Update all references.
5. Check serialization/API/database impact.
6. Build.
7. Run relevant tests.
8. Report every significant rename.

Do not perform mass renaming unrelated to the requested task.

---

# 44. Naming Consistency

When multiple valid names exist, prefer the naming already established by the project.

Consistency is more important than personal preference.

Do not introduce:

```text
UserRepository
OrderStore
ProductDataProvider
CustomerGateway
```

for identical responsibilities without a meaningful architectural distinction.

---

# 45. Naming Review Checklist

Before completing a task, check:

* Are names understandable without opening the implementation?
* Do methods describe actions?
* Do classes describe responsibilities?
* Are boolean names explicit?
* Are collections plural?
* Are identifiers specific?
* Are abbreviations necessary?
* Are generic names avoidable?
* Are business terms consistent?
* Do async methods use `Async`?
* Do files match primary types?
* Did any rename affect a public contract?

---

# 46. Final Naming Principle

Prefer:

> Short enough to read, specific enough to understand.

Do not optimize for the shortest possible name.

Do not optimize for excessively descriptive names either.

Good naming should reduce the amount of code a developer must inspect to understand the system.
