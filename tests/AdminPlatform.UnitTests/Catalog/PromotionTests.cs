using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Catalog.Application;
using AdminPlatform.Modules.Catalog.Application.Categories;
using AdminPlatform.Modules.Catalog.Application.Products;
using AdminPlatform.Modules.Catalog.Application.Promotions;
using AdminPlatform.Modules.Catalog.Domain;
using AdminPlatform.Modules.Catalog.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.UnitTests.Catalog;

public class PromotionTests
{
    private static readonly CancellationToken None = CancellationToken.None;
    private static readonly DateTime Now = new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FixedClock(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private static ICatalogDbContext NewDb() =>
        new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static PromotionDetails Details(
        string code = "welcome20", PromotionType type = PromotionType.Percentage, decimal value = 20,
        decimal? max = null, decimal? minimum = null, DateTime? start = null, DateTime? end = null,
        int? usageLimit = null, PromotionStatus status = PromotionStatus.Active) =>
        new(code, "Khuyến mãi", null, type, value, max, minimum, start, end, usageLimit, status);

    private static CreatePromotionRequest Request(
        string code = "WELCOME20", string type = "percentage", decimal value = 20, decimal? max = null, decimal? minimum = null,
        IReadOnlyList<Guid>? productIds = null, IReadOnlyList<Guid>? categoryIds = null, string status = "active",
        DateTime? start = null, DateTime? end = null, int? usageLimit = null) =>
        new(code, "Khuyến mãi", null, type, value, max, minimum, start, end, usageLimit, productIds, categoryIds, status);

    private static ValidatePromotionRequest Cart(string code, decimal subtotal, decimal shipping = 0, params (Guid Id, decimal Total)[] lines) =>
        new(code, subtotal, shipping, lines.Select(l => new ValidatePromotionItem(l.Id.ToString(), l.Total)).ToList());

    // ---------- Domain ----------

    [Fact]
    public void Code_is_normalized_to_upper_case_and_restricted_to_safe_characters()
    {
        Assert.Equal("WELCOME-20", Promotion.Create(Details(code: "  welcome-20 ")).Code);
        Assert.False(Promotion.IsValidCode("có dấu"));
        Assert.False(Promotion.IsValidCode("a b"));
        Assert.False(Promotion.IsValidCode(""));
    }

    [Theory]
    [InlineData(PromotionType.Percentage, 0)]
    [InlineData(PromotionType.Percentage, 101)]
    [InlineData(PromotionType.FreeShipping, 150)]
    [InlineData(PromotionType.ProductDiscount, -5)]
    [InlineData(PromotionType.FixedAmount, 0)]
    public void Value_must_fit_the_promotion_type(PromotionType type, decimal value)
    {
        Assert.Throws<BusinessRuleValidationException>(() => Promotion.Create(Details(type: type, value: value)));
    }

    [Fact]
    public void A_fixed_amount_may_exceed_100_but_a_percentage_may_not()
    {
        Assert.Equal(30000, Promotion.Create(Details(type: PromotionType.FixedAmount, value: 30000)).Value);
    }

    [Fact]
    public void End_date_must_be_after_start_date_and_usage_limit_positive()
    {
        Assert.Throws<BusinessRuleValidationException>(() => Promotion.Create(Details(start: Now, end: Now)));
        Assert.Throws<BusinessRuleValidationException>(() => Promotion.Create(Details(usageLimit: 0)));
    }

    [Fact]
    public void Unspecified_dates_are_stored_as_utc()
    {
        var promotion = Promotion.Create(Details(end: new DateTime(2026, 12, 31, 23, 59, 0, DateTimeKind.Unspecified)));
        Assert.Equal(DateTimeKind.Utc, promotion.EndAtUtc!.Value.Kind);
    }

    [Fact]
    public void Replacing_scope_keeps_existing_links_and_dedupes()
    {
        var promotion = Promotion.Create(Details(type: PromotionType.ProductDiscount, value: 10));
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        promotion.ReplaceProducts([a, b]);
        var linkA = promotion.Products.Single(l => l.ProductId == a);

        promotion.ReplaceProducts([a, a]);

        Assert.Single(promotion.Products);
        Assert.Same(linkA, promotion.Products.Single());
    }

    // ---------- CRUD service ----------

    [Fact]
    public async Task Code_is_unique_regardless_of_case_and_can_be_kept_on_update()
    {
        var sut = new PromotionService(NewDb());
        var created = await sut.CreateAsync(Request("welcome20"), None);
        Assert.Equal("WELCOME20", created.Code);

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(Request("WELCOME20"), None));

        var updated = await sut.UpdateAsync(created.Id, new UpdatePromotionRequest("welcome20", "Đổi tên", null, "percentage", 25, null, null, null, null, null, null, null, "inactive"), None);
        Assert.Equal("inactive", updated.Status);
        Assert.Equal(25, updated.Value);
    }

    [Fact]
    public async Task A_product_discount_needs_a_scope_that_exists_and_other_types_drop_it()
    {
        var db = NewDb();
        var categories = new CategoryService(db);
        var category = await categories.CreateAsync(new CreateCategoryRequest("Bánh cuốn", null, 1, true), None);
        var sut = new PromotionService(db);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(Request("SCOPE", "product_discount", 10), None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.CreateAsync(Request("SCOPE", "product_discount", 10, categoryIds: [Guid.NewGuid()]), None));

        var created = await sut.CreateAsync(Request("SCOPE", "product_discount", 10, categoryIds: [category.Id]), None);
        Assert.Equal([category.Id], created.ApplicableCategoryIds);

        var switched = await sut.UpdateAsync(created.Id,
            new UpdatePromotionRequest("SCOPE", "Khuyến mãi", null, "percentage", 10, null, null, null, null, null, [Guid.NewGuid()], [category.Id], "active"), None);
        Assert.Empty(switched.ApplicableCategoryIds);
        Assert.Empty(switched.ApplicableProductIds);
    }

    [Fact]
    public async Task Delete_removes_the_promotion()
    {
        var sut = new PromotionService(NewDb());
        var created = await sut.CreateAsync(Request(), None);

        await sut.DeleteAsync(created.Id, None);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByIdAsync(created.Id, None));
    }

    // ---------- Validation (discount calculation) ----------

    private static PromotionValidationService Validator(ICatalogDbContext db, DateTime? now = null) =>
        new(db, new FixedClock(now ?? Now));

    [Fact]
    public async Task Percentage_is_capped_by_max_discount_and_never_exceeds_the_subtotal()
    {
        var db = NewDb();
        await new PromotionService(db).CreateAsync(Request("WELCOME20", "percentage", 20, max: 50000, minimum: 150000), None);

        var result = await Validator(db).ValidateAsync(Cart("welcome20", 500000), None);
        Assert.True(result.IsValid);
        Assert.Equal(50000, result.DiscountAmount);

        var small = await Validator(db).ValidateAsync(Cart("WELCOME20", 200000), None);
        Assert.Equal(40000, small.DiscountAmount);
    }

    [Fact]
    public async Task Minimum_order_start_date_expiry_status_and_usage_limit_are_enforced()
    {
        var db = NewDb();
        var service = new PromotionService(db);
        await service.CreateAsync(Request("MIN", minimum: 150000), None);
        await service.CreateAsync(Request("FUTURE", start: Now.AddDays(1)), None);
        await service.CreateAsync(Request("OLD", end: Now.AddDays(-1)), None);
        await service.CreateAsync(Request("DRAFT", status: "draft"), None);
        var limited = await service.CreateAsync(Request("LIMITED", usageLimit: 1), None);

        var validator = Validator(db);
        Assert.Contains("tối thiểu 150.000đ", (await validator.ValidateAsync(Cart("MIN", 100000), None)).Message);
        Assert.Equal("Mã giảm giá chưa đến ngày áp dụng.", (await validator.ValidateAsync(Cart("FUTURE", 500000), None)).Message);
        Assert.Equal("Mã giảm giá đã hết hạn.", (await validator.ValidateAsync(Cart("OLD", 500000), None)).Message);
        Assert.Equal("Mã giảm giá hiện không khả dụng.", (await validator.ValidateAsync(Cart("DRAFT", 500000), None)).Message);
        Assert.Equal("Mã giảm giá không tồn tại hoặc đã bị xóa.", (await validator.ValidateAsync(Cart("NOPE", 500000), None)).Message);
        Assert.Equal("Vui lòng nhập mã giảm giá.", (await validator.ValidateAsync(Cart("  ", 500000), None)).Message);

        // UsageCount is only ever raised by Orders (not built yet), so exhaust it directly.
        var entity = await db.Promotions.SingleAsync(p => p.Id == limited.Id);
        typeof(Promotion).GetProperty(nameof(Promotion.UsageCount))!.SetValue(entity, 1);
        await db.SaveChangesAsync(None);
        Assert.Equal("Mã giảm giá đã hết lượt sử dụng.", (await validator.ValidateAsync(Cart("LIMITED", 500000), None)).Message);
    }

    [Fact]
    public async Task Fixed_amount_and_free_shipping_are_computed_and_clamped()
    {
        var db = NewDb();
        var service = new PromotionService(db);
        await service.CreateAsync(Request("GIAM30K", "fixed_amount", 30000), None);
        await service.CreateAsync(Request("FREESHIP", "free_shipping", 100), None);
        await service.CreateAsync(Request("HALFSHIP", "free_shipping", 50), None);

        var validator = Validator(db);
        Assert.Equal(20000, (await validator.ValidateAsync(Cart("GIAM30K", 20000), None)).DiscountAmount);

        var free = await validator.ValidateAsync(Cart("FREESHIP", 200000, shipping: 25000), None);
        Assert.Equal(0, free.DiscountAmount);
        Assert.Equal(25000, free.ShippingDiscount);
        Assert.Equal(12500, (await validator.ValidateAsync(Cart("HALFSHIP", 200000, shipping: 25000), None)).ShippingDiscount);
    }

    [Fact]
    public async Task Product_discount_only_counts_lines_in_scope_including_descendant_categories()
    {
        var db = NewDb();
        var categories = new CategoryService(db);
        var group = await categories.CreateAsync(new CreateCategoryRequest("Món ăn", null, 1, true), None);
        var banhCuon = await categories.CreateAsync(new CreateCategoryRequest("Bánh cuốn", group.Id, 1, true), None);
        var leaf = await categories.CreateAsync(new CreateCategoryRequest("Bánh cuốn nhân", banhCuon.Id, 1, true), None);
        var drinks = await categories.CreateAsync(new CreateCategoryRequest("Đồ uống", null, 2, true), None);

        var products = new ProductService(db);
        ProductDetailsRequest(out var make);
        var inScope = await products.CreateAsync(make("Bánh cuốn thịt", leaf.Id), None);
        var outOfScope = await products.CreateAsync(make("Trà đá", drinks.Id), None);
        var directProduct = await products.CreateAsync(make("Chả quế", drinks.Id), None);

        var service = new PromotionService(db);
        await service.CreateAsync(Request("BANHCUON10", "product_discount", 10, max: 20000, categoryIds: [banhCuon.Id]), None);
        await service.CreateAsync(Request("CHAQUE", "product_discount", 50, productIds: [directProduct.Id]), None);

        var validator = Validator(db);
        var result = await validator.ValidateAsync(Cart("BANHCUON10", 160000, 0, (inScope.Id, 100000), (outOfScope.Id, 60000)), None);
        Assert.True(result.IsValid);
        Assert.Equal(10000, result.DiscountAmount);

        var capped = await validator.ValidateAsync(Cart("BANHCUON10", 500000, 0, (inScope.Id, 500000)), None);
        Assert.Equal(20000, capped.DiscountAmount);

        var viaProduct = await validator.ValidateAsync(Cart("CHAQUE", 100000, 0, (directProduct.Id, 40000), (inScope.Id, 60000)), None);
        Assert.Equal(20000, viaProduct.DiscountAmount);

        var none = await validator.ValidateAsync(Cart("BANHCUON10", 60000, 0, (outOfScope.Id, 60000)), None);
        Assert.False(none.IsValid);

        // A legacy (non-GUID) product id from an old cart is simply out of scope, not an error.
        var legacy = await validator.ValidateAsync(new ValidatePromotionRequest("BANHCUON10", 50000, 0, [new ValidatePromotionItem("prod-old", 50000)]), None);
        Assert.False(legacy.IsValid);
    }

    private static void ProductDetailsRequest(out Func<string, Guid, CreateProductRequest> make) =>
        make = (name, categoryId) => new CreateProductRequest(name, null, categoryId, 50000, null, null, true, null, null, null, [], []);
}
