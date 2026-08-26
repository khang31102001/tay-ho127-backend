# Development Guide

Practical, step-by-step. Every path and command below matches the actual
conventions in this codebase — copy the pattern from the module named, don't
invent a new one.

---

## A. Add a new business module

Worked example: a module called **`Inventory`** owning a `Products` catalog.
Follow [`AdminPlatform.Modules.Platform`](../src/Modules/Platform/AdminPlatform.Modules.Platform)
as your template — it's the simplest existing module (no cross-module ports).

### 1. Create the project

```bash
dotnet new classlib -n AdminPlatform.Modules.Inventory -o src/Modules/Inventory/AdminPlatform.Modules.Inventory
dotnet sln AdminPlatform.sln add src/Modules/Inventory/AdminPlatform.Modules.Inventory/AdminPlatform.Modules.Inventory.csproj
rm src/Modules/Inventory/AdminPlatform.Modules.Inventory/Class1.cs
```

Replace the generated `.csproj` with the same shape every other module uses
(copy [`AdminPlatform.Modules.Platform.csproj`](../src/Modules/Platform/AdminPlatform.Modules.Platform/AdminPlatform.Modules.Platform.csproj)
and rename):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="EFCore.NamingConventions" />
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
  </ItemGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Microsoft.AspNetCore.Http" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\AdminPlatform.SharedKernel\AdminPlatform.SharedKernel.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\AdminPlatform.Common\AdminPlatform.Common.csproj" />
  </ItemGroup>

</Project>
```

No package versions here — they all live in [`Directory.Packages.props`](../Directory.Packages.props)
(Central Package Management). If a package you need isn't listed there
already, add one `<PackageVersion Include="..." Version="..." />` line to it.

### 2–3. Domain / Entity

`src/Modules/Inventory/AdminPlatform.Modules.Inventory/Domain/Product.cs` —
extend `CatalogEntity` (gives you `Id`, `Code`, `Name`, `IsActive`, audit
fields, `xmin` concurrency) if it's catalog-style data; extend
`AuditableEntity` directly for a pure junction/link table. Private
constructor + static factory + explicit `Update`, matching every other
entity in this codebase:

```csharp
using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Inventory.Domain;

public sealed class Product : CatalogEntity
{
    public decimal Price { get; private set; }

    private Product()
    {
        // EF Core
    }

    public static Product Create(string code, string name, decimal price)
    {
        if (price < 0)
        {
            throw new BusinessRuleValidationException("Price cannot be negative.");
        }

        return new Product
        {
            Id = Guid.NewGuid(),
            Code = Guard.NotNullOrWhiteSpace(code, nameof(code)).Trim(),
            Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim(),
            Price = price,
            IsActive = true,
        };
    }

    public void Update(string name, bool isActive, decimal price)
    {
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        IsActive = isActive;
        Price = price;
    }
}
```

### 4. Application (use case)

Three files under `Application/Products/` (see
[`Modules/Platform/Application/FiscalYears`](../src/Modules/Platform/AdminPlatform.Modules.Platform/Application/FiscalYears)
for the exact same shape):

- **`ProductContracts.cs`** — request/response records: `CreateProductRequest`, `UpdateProductRequest`, `ProductResponse`.
- **`IProductService.cs`** — the use-case interface (`ListAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, ...).
- **`ProductService.cs`** — the implementation, depending only on `IInventoryDbContext` (defined next) — never the concrete EF `DbContext`.
- **`ProductValidators.cs`** — `CreateProductRequestValidator : AbstractValidator<CreateProductRequest>`, etc. Picked up automatically by `AddValidatorsFromAssembly` — nothing else to wire.

Also add the module's persistence port, `Application/IInventoryDbContext.cs`:

```csharp
using AdminPlatform.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Inventory.Application;

public interface IInventoryDbContext
{
    DbSet<Product> Products { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### 5. Infrastructure / persistence

- **`Infrastructure/InventoryDbContext.cs`** — implements `IInventoryDbContext`; sets `public const string Schema = "inventory";` and calls `modelBuilder.HasDefaultSchema(Schema)` + `ApplyConfigurationsFromAssembly(...)` in `OnModelCreating` (copy [`PlatformDbContext.cs`](../src/Modules/Platform/AdminPlatform.Modules.Platform/Infrastructure/PlatformDbContext.cs)).
- **`Infrastructure/Configurations/ProductConfiguration.cs`** — `IEntityTypeConfiguration<Product>`: `ToTable("products")`, `HasKey`, `Property(p => p.RowVersion).IsRowVersion()`, unique index on `Code`.
- **`Infrastructure/InventoryDbContextFactory.cs`** — `IDesignTimeDbContextFactory<InventoryDbContext>`, copy [`PlatformDbContextFactory.cs`](../src/Modules/Platform/AdminPlatform.Modules.Platform/Infrastructure/PlatformDbContextFactory.cs) verbatim and rename the type. This is what lets `dotnet ef` build your DbContext without running the Migrator's `Program.cs`.
- If this module needs sample data, `Infrastructure/InventorySeeder.cs` — see step 11.

### 6. API

`Api/ProductsController.cs`:

```csharp
using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Inventory.Application.Products;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Inventory.Api;

[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService) => _productService = productService;

    [HttpGet]
    [RequirePermission(InventoryPermissions.ProductsView)]
    public async Task<ActionResult<PagedResult<ProductResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        => Ok(await _productService.ListAsync(request, cancellationToken));

    [HttpPost]
    [RequirePermission(InventoryPermissions.ProductsCreate)]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var created = await _productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(InventoryPermissions.ProductsView)]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _productService.GetByIdAsync(id, cancellationToken));
}
```

### 7. Register dependency injection

`InventoryModule.cs` at the project root — copy
[`PlatformModule.cs`](../src/Modules/Platform/AdminPlatform.Modules.Platform/PlatformModule.cs)
and rename every `Platform` → `Inventory`:

```csharp
public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        services.AddDbContext<InventoryDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", InventoryDbContext.Schema));
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                sp.GetRequiredService<AuditLogSinkInterceptor>());
        });
        services.AddScoped<IInventoryDbContext>(sp => sp.GetRequiredService<InventoryDbContext>());

        services.AddScoped<IProductService, ProductService>();
        services.AddValidatorsFromAssembly(typeof(InventoryModule).Assembly);
        services.AddControllers().AddApplicationPart(typeof(InventoryModule).Assembly);

        return services;
    }
}
```

Then wire it into **two** places, in the same "Modules" block, same order,
in each:

- [`src/Host/AdminPlatform.Api/Program.cs`](../src/Host/AdminPlatform.Api/Program.cs) — add `using AdminPlatform.Modules.Inventory;` and `builder.Services.AddInventoryModule(builder.Configuration);` next to the other four.
- [`src/Tools/AdminPlatform.Migrator/Program.cs`](../src/Tools/AdminPlatform.Migrator/Program.cs) — same `using` + `appBuilder.Services.AddInventoryModule(appBuilder.Configuration);`, plus a `MigrateAsync`/`SeedAsync` line (step 10–11).

If `Inventory` needs data from another module (e.g. it needs to know a
`Brand` exists), **do not** add a `ProjectReference`. Follow the port/adapter
pattern in [`ARCHITECTURE.md`](ARCHITECTURE.md#how-modules-communicate), or —
if it's just "does this id exist" — store the id as a plain `Guid` column and
add the real FK by hand in your migration's SQL (step 10).

### 8. Create permissions

`Api/InventoryPermissions.cs` — copy the shape of
[`PlatformPermissions.cs`](../src/Modules/Platform/AdminPlatform.Modules.Platform/Api/PlatformPermissions.cs):

```csharp
namespace AdminPlatform.Modules.Inventory.Api;

public static class InventoryPermissions
{
    public const string ProductsView = "products.view";
    public const string ProductsCreate = "products.create";
    public const string ProductsUpdate = "products.update";

    public static IReadOnlyList<(string Code, string Description)> All { get; } =
    [
        (ProductsView, "View products"),
        (ProductsCreate, "Create products"),
        (ProductsUpdate, "Update products"),
    ];
}
```

Then add one line to
[`src/Tools/AdminPlatform.Migrator/PermissionCatalog.cs`](../src/Tools/AdminPlatform.Migrator/PermissionCatalog.cs):

```csharp
.. AdminPlatform.Modules.Inventory.Api.InventoryPermissions.All,
```

That's the only place the cross-module permission catalog is assembled —
nothing else needs to know this module exists for permissions to be seeded
and grantable through `/api/v1/roles/{id}/permissions`.

### 9. Create a menu entry (only if the UI needs one)

Add an entry to the `Menus` array in
[`NavigationSeeder.cs`](../src/Modules/Navigation/AdminPlatform.Modules.Navigation/Infrastructure/NavigationSeeder.cs):

```csharp
new("admin.products", "Products", "admin", "/admin/products", "package", 110, "products.view"),
```

The `PermissionCode` there is a plain string, checked against the caller's
JWT `permission` claims at read time — Navigation never needs to reference
Inventory's `InventoryPermissions` class or project.

### 10. Create the migration

```bash
dotnet ef migrations add InitialCreate \
  --project src/Modules/Inventory/AdminPlatform.Modules.Inventory/AdminPlatform.Modules.Inventory.csproj \
  --context AdminPlatform.Modules.Inventory.Infrastructure.InventoryDbContext \
  --output-dir Infrastructure/Migrations
```

If a column references another module's table by id (see step 7), open the
generated migration and add, at the end of `Up()`:

```csharp
migrationBuilder.Sql(
    "ALTER TABLE inventory.products " +
    "ADD CONSTRAINT fk_products_organization_organizations_organization_id " +
    "FOREIGN KEY (organization_id) REFERENCES organization.organizations (id) ON DELETE RESTRICT;");
```

Then add the migrate call to `AdminPlatform.Migrator/Program.cs`'s `MigrateAsync`:

```csharp
logger.LogInformation("Applying Inventory module migrations...");
await services.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
```

**Order matters** if you added a cross-module FK: your module's migration
must run *after* the module it references. Put the line accordingly.

### 11. Seed data (only if you need sample rows)

`Infrastructure/InventorySeeder.cs`, upserted by `Code` — copy
[`OrganizationSeeder.cs`](../src/Modules/Organization/AdminPlatform.Modules.Organization/Infrastructure/OrganizationSeeder.cs)'s
shape. Then call it from `Migrator/Program.cs`'s `SeedAsync`, in the same
position you put the migration call:

```csharp
logger.LogInformation("Seeding Inventory module (sample products)...");
await InventorySeeder.SeedAsync(services, cancellationToken);
```

### 12. Write tests

- `tests/AdminPlatform.UnitTests/Inventory/ProductTests.cs` — domain rules (e.g. `Product.Create` rejects negative price), no DB.
- `tests/AdminPlatform.UnitTests/Inventory/ProductServiceTests.cs` — service logic against `Microsoft.EntityFrameworkCore.InMemory` (copy [`RoleServiceTests.cs`](../tests/AdminPlatform.UnitTests/AccessControl/RoleServiceTests.cs)'s pattern: `new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)`).
- Architecture tests need no changes — `LayeringTests` and `ModuleBoundaryTests` already iterate every module assembly via `MemberData`; add `typeof(Modules.Inventory.InventoryModule).Assembly` to that list in [`LayeringTests.cs`](../tests/AdminPlatform.ArchitectureTests/LayeringTests.cs) and [`ModuleBoundaryTests.cs`](../tests/AdminPlatform.ArchitectureTests/ModuleBoundaryTests.cs).
- Integration tests, if the flow is important enough: add a test class to `tests/AdminPlatform.IntegrationTests/`, same `[Collection("Api")]` pattern as [`UsersCrudTests.cs`](../tests/AdminPlatform.IntegrationTests/UsersCrudTests.cs).

### 13. Build and verify

```bash
dotnet build AdminPlatform.sln
dotnet test tests/AdminPlatform.UnitTests
dotnet test tests/AdminPlatform.ArchitectureTests
dotnet ef migrations has-pending-model-changes \
  --project src/Modules/Inventory/AdminPlatform.Modules.Inventory/AdminPlatform.Modules.Inventory.csproj \
  --context AdminPlatform.Modules.Inventory.Infrastructure.InventoryDbContext
```

---

## B. Add a feature to an existing module

Worked example: module `Inventory` already exists (from Part A). Add
**`CreateProduct`** — wait, that already exists above; here's a genuinely new
feature on top of it: **archiving a product** (a business action, not a raw
CRUD field flip — see [`api-design.md`](../.claude/rules/api-design.md) §6).

| Layer | File | Change |
|---|---|---|
| Domain | `Domain/Product.cs` | Add a method: `public void Archive() => IsActive = false;` (or a dedicated `IsArchived` flag if "archived" must be distinct from "inactive") |
| Application | `Application/Products/IProductService.cs` | Add `Task ArchiveAsync(Guid id, CancellationToken cancellationToken);` |
| Application | `Application/Products/ProductService.cs` | Implement it: load via existing `FindOrThrowAsync` helper, call `product.Archive()`, `await _db.SaveChangesAsync(...)` |
| Application | *(no new contract needed — no request body)* | — |
| Infrastructure | *(none)* | No schema change — reuses the existing `IsActive` column |
| Api | `Api/ProductsController.cs` | Add an action route, matching the business-action pattern used by `UsersController.ResetPassword`: `[HttpPost("{id:guid}/archive")] [RequirePermission(InventoryPermissions.ProductsArchive)]` |
| Permission | `Api/InventoryPermissions.cs` | Add `ProductsArchive = "products.archive"` to the class and to `All` |
| Permission catalog | `Migrator/PermissionCatalog.cs` | Nothing — it already pulls `InventoryPermissions.All` |
| Test | `tests/AdminPlatform.UnitTests/Inventory/ProductServiceTests.cs` | Add `ArchiveAsync_sets_the_product_inactive` |
| Test | `tests/AdminPlatform.IntegrationTests/` | Optional: an end-to-end `POST /api/v1/products/{id}/archive` test if the flow is important |

Full example of the two changed methods:

```csharp
// Application/Products/ProductService.cs
public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
{
    var product = await FindOrThrowAsync(id, cancellationToken);
    product.Archive();
    await _db.SaveChangesAsync(cancellationToken);
}
```

```csharp
// Api/ProductsController.cs
[HttpPost("{id:guid}/archive")]
[RequirePermission(InventoryPermissions.ProductsArchive)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
{
    await _productService.ArchiveAsync(id, cancellationToken);
    return NoContent();
}
```

No migration needed here because no column changed. If your feature *does*
add/rename a column, follow step 10 in Part A.

---

## C. Add an Entity / API / Permission / Menu — quick checklist

```
Entity (Domain/)
  → EF Configuration (Infrastructure/Configurations/<Entity>Configuration.cs)
  → Migration (dotnet ef migrations add ...)
  → Application (Contracts + I<X>Service + <X>Service + Validators)
  → API (Controller action, [RequirePermission])
  → Permission (<Module>Permissions.cs + Migrator/PermissionCatalog.cs)
  → Menu (NavigationSeeder.cs — only if it needs a sidebar entry)
  → Test (Unit: domain + service; Architecture: none, already generic; Integration: optional)
```

| Step | File(s) | Notes |
|---|---|---|
| Entity | `Domain/<Entity>.cs` | Extend `CatalogEntity` (has Code/Name/IsActive) or `AuditableEntity` (junction table) or `Entity` (append-only log, like `AuditLog`) |
| EF Configuration | `Infrastructure/Configurations/<Entity>Configuration.cs` | `IEntityTypeConfiguration<T>`, discovered automatically by `ApplyConfigurationsFromAssembly` — no manual registration |
| Migration | `dotnet ef migrations add <Name> --project ... --context ... --output-dir Infrastructure/Migrations` | Add raw-SQL FKs by hand for any cross-module id column |
| Application | `Application/<Feature>/` — Contracts, `I<X>Service`, `<X>Service`, Validators | Depend on `I<Module>DbContext`, never the concrete `DbContext` |
| API | `Api/<X>Controller.cs` | `[ApiController]`, `[Route("api/v1/...")]`, one `[RequirePermission(...)]` per action |
| Permission | `Api/<Module>Permissions.cs` + one line in `Migrator/PermissionCatalog.cs` | Codes look like `resource.action`, e.g. `products.view` |
| Menu | `NavigationSeeder.cs` `Menus` array | Only for entities with an admin UI screen; `PermissionCode` gates visibility |
| Test | `tests/AdminPlatform.UnitTests/<Module>/` | Domain rules + service logic (EF InMemory) at minimum |

After any of the above, always finish with:

```bash
dotnet build AdminPlatform.sln
dotnet test tests/AdminPlatform.UnitTests
dotnet test tests/AdminPlatform.ArchitectureTests
```
