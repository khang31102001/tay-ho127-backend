using System.Text.Json;
using AdminPlatform.Modules.Catalog.Application;
using AdminPlatform.Modules.Catalog.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Catalog.Infrastructure;

/// <summary>Initial restaurant catalog (the menu the website shows today: categories, products, sales menus,
/// placements, modifier groups), from the embedded Seed/catalog-seed.json that was exported from the
/// frontend's former mock data. Keys in the file only link rows together; real ids are new GUIDs.
///
/// Runs only while the catalog is completely empty, so it is safe in the every-deploy `seed` step: once
/// there is any catalog data, admins own it and the seed never re-creates something they deleted.</summary>
public static class CatalogSeeder
{
    private const string SeedResourceName = "AdminPlatform.Modules.Catalog.catalog-seed.json";

    public static async Task<bool> SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<ICatalogDbContext>();

        var hasCatalogData = await db.Categories.AnyAsync(cancellationToken)
            || await db.Products.AnyAsync(cancellationToken)
            || await db.SalesMenus.AnyAsync(cancellationToken)
            || await db.ModifierGroups.AnyAsync(cancellationToken);
        if (hasCatalogData)
        {
            return false;
        }

        var seed = LoadSeed();

        // Parents are listed before children in the file, so ids resolve in a single pass.
        var categoryIds = new Dictionary<string, Guid>();
        foreach (var seedCategory in seed.Categories)
        {
            Guid? parentId = seedCategory.ParentKey is null ? null : categoryIds[seedCategory.ParentKey];
            var category = Category.Create(seedCategory.Name, parentId, seedCategory.SortOrder, seedCategory.IsActive);
            categoryIds[seedCategory.Key] = category.Id;
            db.Categories.Add(category);
        }

        var modifierGroupIds = new Dictionary<string, Guid>();
        foreach (var seedGroup in seed.ModifierGroups)
        {
            var selectionType = Enum.Parse<ModifierSelectionType>(seedGroup.SelectionType, ignoreCase: true);
            var options = seedGroup.Options
                .Select(o => new ModifierOptionInput(null, o.Label, o.PriceAdjustment, o.IsDefault))
                .ToList();
            var group = ModifierGroup.Create(seedGroup.Name, selectionType, seedGroup.IsRequired, options);
            modifierGroupIds[seedGroup.Key] = group.Id;
            db.ModifierGroups.Add(group);
        }

        var productIds = new Dictionary<string, Guid>();
        var usedSlugs = new HashSet<string>();
        foreach (var seedProduct in seed.Products)
        {
            var slug = UniqueSlug(Slug.FromText(seedProduct.Name), usedSlugs);
            var details = new ProductDetails(seedProduct.Price, seedProduct.OldPrice, seedProduct.Description, seedProduct.IsActive,
                seedProduct.Badge, seedProduct.Rating, seedProduct.RatingCount);
            var product = Product.Create(seedProduct.Name, slug, categoryIds[seedProduct.CategoryKey], details);
            product.ReplaceMedia(seedProduct.MediaIds);
            product.ReplaceModifierGroups(seedProduct.ModifierGroupKeys.Select(key => modifierGroupIds[key]).ToList());
            productIds[seedProduct.Key] = product.Id;
            db.Products.Add(product);
        }

        var salesMenuIds = new Dictionary<string, Guid>();
        foreach (var seedMenu in seed.SalesMenus)
        {
            var menu = SalesMenu.Create(seedMenu.Code, seedMenu.Name, seedMenu.IsActive);
            salesMenuIds[seedMenu.Key] = menu.Id;
            db.SalesMenus.Add(menu);
        }

        foreach (var seedPlacement in seed.SalesMenuProducts)
        {
            db.SalesMenuProducts.Add(SalesMenuProduct.Create(
                salesMenuIds[seedPlacement.MenuKey],
                productIds[seedPlacement.ProductKey],
                seedPlacement.PriceOverride,
                seedPlacement.SortOrder,
                seedPlacement.IsAvailable));
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string UniqueSlug(string baseSlug, HashSet<string> usedSlugs)
    {
        var slug = baseSlug;
        for (var suffix = 2; !usedSlugs.Add(slug); suffix++)
        {
            slug = $"{baseSlug}-{suffix}";
        }

        return slug;
    }

    private static CatalogSeed LoadSeed()
    {
        using var stream = typeof(CatalogSeeder).Assembly.GetManifestResourceStream(SeedResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{SeedResourceName}' is missing.");
        return JsonSerializer.Deserialize<CatalogSeed>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"Embedded resource '{SeedResourceName}' is empty.");
    }

    private sealed record CatalogSeed(
        IReadOnlyList<SeedCategory> Categories,
        IReadOnlyList<SeedModifierGroup> ModifierGroups,
        IReadOnlyList<SeedProduct> Products,
        IReadOnlyList<SeedSalesMenu> SalesMenus,
        IReadOnlyList<SeedSalesMenuProduct> SalesMenuProducts);

    private sealed record SeedCategory(string Key, string Name, string? ParentKey, int SortOrder, bool IsActive);

    private sealed record SeedModifierGroup(string Key, string Name, string SelectionType, bool IsRequired, IReadOnlyList<SeedModifierOption> Options);

    private sealed record SeedModifierOption(string Label, decimal PriceAdjustment, bool IsDefault);

    private sealed record SeedProduct(
        string Key,
        string Name,
        string CategoryKey,
        decimal Price,
        decimal? OldPrice,
        string? Description,
        bool IsActive,
        string? Badge,
        decimal? Rating,
        int? RatingCount,
        IReadOnlyList<string> MediaIds,
        IReadOnlyList<string> ModifierGroupKeys);

    private sealed record SeedSalesMenu(string Key, string Code, string Name, bool IsActive);

    private sealed record SeedSalesMenuProduct(string MenuKey, string ProductKey, decimal? PriceOverride, int SortOrder, bool IsAvailable);
}
