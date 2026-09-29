using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Catalog.Application.Categories;
using AdminPlatform.Modules.Catalog.Application.ModifierGroups;
using AdminPlatform.Modules.Catalog.Application.Products;
using AdminPlatform.Modules.Catalog.Application.PublicCatalog;
using AdminPlatform.Modules.Catalog.Application.SalesMenuProducts;
using AdminPlatform.Modules.Catalog.Application.SalesMenus;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class CatalogCrudTests
{
    private readonly AdminPlatformApiFactory _factory;

    public CatalogCrudTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Catalog_can_be_built_edited_and_published_to_the_public_snapshot()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var suffix = Guid.NewGuid().ToString("n")[..8];

        var group = await CreateAsync<CategoryResponse>(client, "/api/v1/catalog/categories", new CreateCategoryRequest($"Món ăn {suffix}", null, 1, true));
        var leaf = await CreateAsync<CategoryResponse>(client, "/api/v1/catalog/categories", new CreateCategoryRequest($"Bánh cuốn {suffix}", group.Id, 1, true));

        var modifierGroup = await CreateAsync<ModifierGroupResponse>(client, "/api/v1/catalog/modifier-groups", new CreateModifierGroupRequest(
            "Nước mắm", "single", true, [new(null, "Cay", 0, true), new(null, "Không cay", 0, false)]));
        Assert.Equal("single", modifierGroup.SelectionType);

        var product = await CreateAsync<ProductResponse>(client, "/api/v1/catalog/products", new CreateProductRequest(
            $"Bánh cuốn thịt {suffix}", null, leaf.Id, 56000, 60000, "Nhân thịt", true, "DISH", 4.8m, 25,
            ["media-banh-cuon-dish"], [modifierGroup.Id]));
        Assert.Equal($"banh-cuon-thit-{suffix}", product.Slug);
        Assert.Equal(["media-banh-cuon-dish"], product.MediaIds);

        var menu = await CreateAsync<SalesMenuResponse>(client, "/api/v1/catalog/sales-menus", new CreateSalesMenuRequest($"menu-{suffix}", "Thực đơn test", true));
        var placement = await CreateAsync<SalesMenuProductResponse>(client, "/api/v1/catalog/sales-menu-products",
            new CreateSalesMenuProductRequest(menu.Id, product.Id, 50000, 1, true));

        // Duplicate placement and duplicate menu code are conflicts.
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/catalog/sales-menu-products",
            new CreateSalesMenuProductRequest(menu.Id, product.Id, null, 2, true))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/catalog/sales-menus",
            new CreateSalesMenuRequest($"menu-{suffix}", "Trùng", true))).StatusCode);

        // Editing options keeps the id of an option that is sent back.
        var keptOptionId = modifierGroup.Options[0].Id;
        var updatedGroup = await (await client.PutAsJsonAsync($"/api/v1/catalog/modifier-groups/{modifierGroup.Id}", new UpdateModifierGroupRequest(
            "Nước mắm", "single", true, [new(keptOptionId, "Cay vừa", 0, true), new(null, "Thêm nước mắm", 5000, false)])))
            .Content.ReadFromJsonAsync<ModifierGroupResponse>();
        Assert.Equal(keptOptionId, updatedGroup!.Options[0].Id);
        Assert.Equal(2, updatedGroup.Options.Count);

        using var anonymous = _factory.CreateClient();
        var snapshot = (await anonymous.GetFromJsonAsync<PublicCatalogResponse>("/api/v1/catalog/public"))!;
        Assert.Contains(snapshot.SalesMenus, m => m.Code == $"menu-{suffix}");
        Assert.Contains(snapshot.SalesMenuProducts, mp => mp.Id == placement.Id && mp.PriceOverride == 50000);
        var publicProduct = Assert.Single(snapshot.Products, p => p.Id == product.Id);
        Assert.Equal([modifierGroup.Id], publicProduct.ModifierGroupIds);

        // Deactivating the product hides it and its placement from the public snapshot.
        var update = new UpdateProductRequest(product.Name, null, leaf.Id, 56000, null, null, false, null, null, null, [], [modifierGroup.Id]);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/catalog/products/{product.Id}", update)).StatusCode);
        snapshot = (await anonymous.GetFromJsonAsync<PublicCatalogResponse>("/api/v1/catalog/public"))!;
        Assert.DoesNotContain(snapshot.Products, p => p.Id == product.Id);
        Assert.DoesNotContain(snapshot.SalesMenuProducts, mp => mp.Id == placement.Id);

        // A category with sub-categories/products cannot be deleted; deleting the product cascades its placement.
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/v1/catalog/categories/{group.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/catalog/products/{product.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/catalog/sales-menu-products/{placement.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/catalog/categories/{leaf.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/catalog/categories/{group.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/catalog/sales-menus/{menu.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/catalog/modifier-groups/{modifierGroup.Id}")).StatusCode);
    }

    [Fact]
    public async Task Invalid_requests_return_400()
    {
        using var client = await AuthTestHelper.CreateAdminClientAsync(_factory);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/catalog/products", new CreateProductRequest(
            "", "Not A Slug", Guid.Empty, -1, null, null, true, null, 9, -1, null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/catalog/products", new CreateProductRequest(
            "Món mồ côi", null, Guid.NewGuid(), 1000, null, null, true, null, null, null, null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/catalog/sales-menus",
            new CreateSalesMenuRequest("Thuc Don", "Sai mã", true))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/catalog/modifier-groups",
            new CreateModifierGroupRequest("Rau", "radio", true, []))).StatusCode);
    }

    [Fact]
    public async Task Admin_endpoints_require_authentication_but_the_public_snapshot_does_not()
    {
        using var anonymous = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/catalog/products")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/v1/catalog/public")).StatusCode);
    }

    private static async Task<T> CreateAsync<T>(HttpClient client, string url, object request)
    {
        var response = await client.PostAsJsonAsync(url, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
