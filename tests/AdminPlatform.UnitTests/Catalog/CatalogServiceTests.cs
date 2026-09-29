using AdminPlatform.Modules.Catalog.Application;
using AdminPlatform.Modules.Catalog.Application.Categories;
using AdminPlatform.Modules.Catalog.Application.Products;
using AdminPlatform.Modules.Catalog.Application.SalesMenuProducts;
using AdminPlatform.Modules.Catalog.Application.SalesMenus;
using AdminPlatform.Modules.Catalog.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.UnitTests.Catalog;

public class CatalogServiceTests
{
    private static ICatalogDbContext NewDb() =>
        new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static readonly CancellationToken None = CancellationToken.None;

    [Fact]
    public async Task Category_tree_is_limited_to_three_levels()
    {
        var sut = new CategoryService(NewDb());
        var group = await sut.CreateAsync(new CreateCategoryRequest("Món ăn", null, 1, true), None);
        var category = await sut.CreateAsync(new CreateCategoryRequest("Bánh cuốn", group.Id, 1, true), None);
        var subCategory = await sut.CreateAsync(new CreateCategoryRequest("Bánh cuốn nhân", category.Id, 1, true), None);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.CreateAsync(new CreateCategoryRequest("Too deep", subCategory.Id, 1, true), None));
    }

    [Fact]
    public async Task Moving_a_subtree_must_keep_the_depth_limit_and_cannot_create_a_cycle()
    {
        var sut = new CategoryService(NewDb());
        var group = await sut.CreateAsync(new CreateCategoryRequest("Món ăn", null, 1, true), None);
        var category = await sut.CreateAsync(new CreateCategoryRequest("Bánh cuốn", group.Id, 1, true), None);
        var otherGroup = await sut.CreateAsync(new CreateCategoryRequest("Đồ uống", null, 2, true), None);
        var otherCategory = await sut.CreateAsync(new CreateCategoryRequest("Nước", otherGroup.Id, 1, true), None);

        // "Món ăn" has 2 levels; under "Nước" (depth 2) it would reach depth 4.
        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.UpdateAsync(group.Id, new UpdateCategoryRequest("Món ăn", otherCategory.Id, 1, true), None));

        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.UpdateAsync(group.Id, new UpdateCategoryRequest("Món ăn", category.Id, 1, true), None));

        var moved = await sut.UpdateAsync(category.Id, new UpdateCategoryRequest("Bánh cuốn", otherGroup.Id, 3, false), None);
        Assert.Equal(otherGroup.Id, moved.ParentId);
        Assert.False(moved.IsActive);
    }

    [Fact]
    public async Task A_category_with_children_or_products_cannot_be_deleted()
    {
        var db = NewDb();
        var categories = new CategoryService(db);
        var group = await categories.CreateAsync(new CreateCategoryRequest("Món ăn", null, 1, true), None);
        var leaf = await categories.CreateAsync(new CreateCategoryRequest("Bánh cuốn", group.Id, 1, true), None);
        await new ProductService(db).CreateAsync(ProductRequest("Bánh cuốn thịt", leaf.Id), None);

        await Assert.ThrowsAsync<ConflictException>(() => categories.DeleteAsync(group.Id, None));
        await Assert.ThrowsAsync<ConflictException>(() => categories.DeleteAsync(leaf.Id, None));
    }

    [Fact]
    public async Task Product_slug_is_generated_unique_and_kept_on_rename()
    {
        var db = NewDb();
        var category = await new CategoryService(db).CreateAsync(new CreateCategoryRequest("Bánh cuốn", null, 1, true), None);
        var sut = new ProductService(db);

        var first = await sut.CreateAsync(ProductRequest("Bánh cuốn thịt", category.Id), None);
        var second = await sut.CreateAsync(ProductRequest("Bánh cuốn thịt", category.Id), None);
        Assert.Equal("banh-cuon-thit", first.Slug);
        Assert.Equal("banh-cuon-thit-2", second.Slug);

        var renamed = await sut.UpdateAsync(first.Id, UpdateRequest("Bánh cuốn thịt đặc biệt", category.Id, slug: null), None);
        Assert.Equal("banh-cuon-thit", renamed.Slug);

        await Assert.ThrowsAsync<ConflictException>(
            () => sut.UpdateAsync(first.Id, UpdateRequest("Bánh cuốn", category.Id, slug: "banh-cuon-thit-2"), None));
    }

    [Fact]
    public async Task Product_requires_an_existing_category_and_modifier_groups()
    {
        var db = NewDb();
        var category = await new CategoryService(db).CreateAsync(new CreateCategoryRequest("Bánh cuốn", null, 1, true), None);
        var sut = new ProductService(db);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(ProductRequest("X", Guid.NewGuid()), None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.CreateAsync(ProductRequest("X", category.Id) with { ModifierGroupIds = [Guid.NewGuid()] }, None));
    }

    [Fact]
    public async Task A_product_can_be_placed_on_a_menu_only_once()
    {
        var db = NewDb();
        var category = await new CategoryService(db).CreateAsync(new CreateCategoryRequest("Bánh cuốn", null, 1, true), None);
        var product = await new ProductService(db).CreateAsync(ProductRequest("Bánh cuốn thịt", category.Id), None);
        var menus = new SalesMenuService(db);
        var menu = await menus.CreateAsync(new CreateSalesMenuRequest("thuc-don-chinh", "Thực đơn chính", true), None);
        var sut = new SalesMenuProductService(db);

        await sut.CreateAsync(new CreateSalesMenuProductRequest(menu.Id, product.Id, null, 1, true), None);

        await Assert.ThrowsAsync<ConflictException>(
            () => sut.CreateAsync(new CreateSalesMenuProductRequest(menu.Id, product.Id, 10000, 2, true), None));
        await Assert.ThrowsAsync<ConflictException>(
            () => menus.CreateAsync(new CreateSalesMenuRequest("thuc-don-chinh", "Trùng mã", true), None));
    }

    private static CreateProductRequest ProductRequest(string name, Guid categoryId) =>
        new(name, null, categoryId, 50000, null, null, true, null, null, null, ["media-banh-cuon-dish"], []);

    private static UpdateProductRequest UpdateRequest(string name, Guid categoryId, string? slug) =>
        new(name, slug, categoryId, 55000, 60000, "Mô tả", true, "DISH", 4.5m, 10, [], []);
}
