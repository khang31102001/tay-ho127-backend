using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Catalog.Application.Promotions;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class PromotionCrudTests
{
    private readonly AdminPlatformApiFactory _factory;

    public PromotionCrudTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Promotion_can_be_managed_by_an_admin_and_validated_anonymously()
    {
        using var admin = await AuthTestHelper.CreateAdminClientAsync(_factory);
        var code = $"TEST{Guid.NewGuid().ToString("n")[..8]}".ToUpperInvariant();

        var created = await admin.PostAsJsonAsync("/api/v1/catalog/promotions",
            new CreatePromotionRequest(code.ToLowerInvariant(), "Giảm 20%", null, "percentage", 20, 50000, 150000, null, null, 5, null, null, "active"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var promotion = (await created.Content.ReadFromJsonAsync<PromotionResponse>())!;
        Assert.Equal(code, promotion.Code);
        Assert.Equal(0, promotion.UsageCount);

        // Duplicate code is a conflict; a product discount without a scope is a 400.
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/v1/catalog/promotions",
            new CreatePromotionRequest(code, "Trùng", null, "percentage", 10, null, null, null, null, null, null, null, "active"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/v1/catalog/promotions",
            new CreatePromotionRequest($"{code}X", "Thiếu phạm vi", null, "product_discount", 10, null, null, null, null, null, null, null, "active"))).StatusCode);

        // Guest checkout validates without logging in.
        using var anonymous = _factory.CreateClient();
        var validated = await anonymous.PostAsJsonAsync("/api/v1/catalog/promotions/validate",
            new ValidatePromotionRequest(code, 500000, 20000, []));
        Assert.Equal(HttpStatusCode.OK, validated.StatusCode);
        var result = (await validated.Content.ReadFromJsonAsync<ValidatePromotionResponse>())!;
        Assert.True(result.IsValid);
        Assert.Equal(50000, result.DiscountAmount);

        // Managing promotions is not anonymous.
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/catalog/promotions")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/catalog/promotions/{promotion.Id}")).StatusCode);
        var afterDelete = (await (await anonymous.PostAsJsonAsync("/api/v1/catalog/promotions/validate",
            new ValidatePromotionRequest(code, 500000, 0, []))).Content.ReadFromJsonAsync<ValidatePromotionResponse>())!;
        Assert.False(afterDelete.IsValid);
    }
}
