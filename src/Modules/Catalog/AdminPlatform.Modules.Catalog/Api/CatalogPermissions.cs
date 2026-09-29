namespace AdminPlatform.Modules.Catalog.Api;

/// <summary>Sales-menu permissions also cover the menu ↔ product placements (sales-menu-products).</summary>
public static class CatalogPermissions
{
    public const string CategoriesView = "categories.view";
    public const string CategoriesCreate = "categories.create";
    public const string CategoriesUpdate = "categories.update";
    public const string CategoriesDelete = "categories.delete";

    public const string ProductsView = "products.view";
    public const string ProductsCreate = "products.create";
    public const string ProductsUpdate = "products.update";
    public const string ProductsDelete = "products.delete";

    public const string SalesMenusView = "sales-menus.view";
    public const string SalesMenusCreate = "sales-menus.create";
    public const string SalesMenusUpdate = "sales-menus.update";
    public const string SalesMenusDelete = "sales-menus.delete";

    public const string ModifierGroupsView = "modifier-groups.view";
    public const string ModifierGroupsCreate = "modifier-groups.create";
    public const string ModifierGroupsUpdate = "modifier-groups.update";
    public const string ModifierGroupsDelete = "modifier-groups.delete";

    public static IReadOnlyList<(string Code, string Description)> All { get; } =
    [
        (CategoriesView, "View catalog categories"),
        (CategoriesCreate, "Create catalog categories"),
        (CategoriesUpdate, "Update catalog categories"),
        (CategoriesDelete, "Delete catalog categories"),
        (ProductsView, "View products"),
        (ProductsCreate, "Create products"),
        (ProductsUpdate, "Update products"),
        (ProductsDelete, "Delete products"),
        (SalesMenusView, "View sales menus and their products"),
        (SalesMenusCreate, "Create sales menus and add products to them"),
        (SalesMenusUpdate, "Update sales menus and their products"),
        (SalesMenusDelete, "Delete sales menus and remove products from them"),
        (ModifierGroupsView, "View modifier groups"),
        (ModifierGroupsCreate, "Create modifier groups"),
        (ModifierGroupsUpdate, "Update modifier groups"),
        (ModifierGroupsDelete, "Delete modifier groups"),
    ];
}
