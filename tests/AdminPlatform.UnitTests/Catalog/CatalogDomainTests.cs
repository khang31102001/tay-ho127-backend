using AdminPlatform.Modules.Catalog.Domain;
using AdminPlatform.SharedKernel;

namespace AdminPlatform.UnitTests.Catalog;

public class CatalogDomainTests
{
    [Theory]
    [InlineData("Bánh cuốn nhân thịt đặc biệt", "banh-cuon-nhan-thit-dac-biet")]
    [InlineData("  Nem/Chả  ", "nem-cha")]
    [InlineData("Đồ uống 7-Up", "do-uong-7-up")]
    [InlineData("Phở Bò Tái (lớn)", "pho-bo-tai-lon")]
    [InlineData("!!!", "")]
    public void Slug_from_text_transliterates_Vietnamese(string text, string expected)
    {
        Assert.Equal(expected, Slug.FromText(text));
    }

    [Theory]
    [InlineData("banh-cuon", true)]
    [InlineData("banh--cuon", false)]
    [InlineData("-banh", false)]
    [InlineData("Banh", false)]
    [InlineData("", false)]
    public void Slug_validation(string slug, bool isValid)
    {
        Assert.Equal(isValid, Slug.IsValid(slug));
    }

    [Fact]
    public void Product_replace_media_keeps_order_dedupes_and_keeps_existing_links()
    {
        var product = NewProduct();
        product.ReplaceMedia(["a", "b", "c"]);
        var linkB = product.Media.Single(m => m.MediaId == "b");

        product.ReplaceMedia(["c", "b", "b", " "]);

        Assert.Equal(["c", "b"], product.Media.OrderBy(m => m.SortOrder).Select(m => m.MediaId));
        Assert.Same(linkB, product.Media.Single(m => m.MediaId == "b"));
    }

    [Fact]
    public void Product_rejects_negative_price_and_out_of_range_rating()
    {
        Assert.Throws<BusinessRuleValidationException>(() => NewProduct(price: -1));
        Assert.Throws<BusinessRuleValidationException>(() => NewProduct(rating: 5.5m));
    }

    [Fact]
    public void Modifier_group_keeps_ids_of_options_sent_back_and_drops_the_rest()
    {
        var group = ModifierGroup.Create("Rau", ModifierSelectionType.Single, true,
        [
            new(null, "Tiêu chuẩn", 0, true),
            new(null, "Thêm rau", 5000, false),
        ]);
        var keptId = group.Options[1].Id;

        group.Update("Rau", ModifierSelectionType.Single, true,
        [
            new(keptId, "Thêm nhiều rau", 7000, false),
            new(null, "Không rau", 0, true),
        ]);

        Assert.Equal(2, group.Options.Count);
        var kept = group.Options.Single(o => o.Id == keptId);
        Assert.Equal("Thêm nhiều rau", kept.Label);
        Assert.Equal(0, kept.SortOrder);
        Assert.DoesNotContain(group.Options, o => o.Label == "Tiêu chuẩn");
    }

    [Fact]
    public void Single_choice_group_allows_at_most_one_default()
    {
        Assert.Throws<BusinessRuleValidationException>(() => ModifierGroup.Create("Nước mắm", ModifierSelectionType.Single, true,
            [new(null, "Cay", 0, true), new(null, "Không cay", 0, true)]));

        var multiple = ModifierGroup.Create("Topping", ModifierSelectionType.Multiple, false,
            [new(null, "Chả", 10000, true), new(null, "Nem", 10000, true)]);
        Assert.Equal(2, multiple.Options.Count(o => o.IsDefault));
    }

    [Fact]
    public void Modifier_group_rejects_empty_options_and_foreign_option_ids()
    {
        Assert.Throws<BusinessRuleValidationException>(() => ModifierGroup.Create("Rau", ModifierSelectionType.Single, true, []));

        var group = ModifierGroup.Create("Rau", ModifierSelectionType.Single, true, [new(null, "Tiêu chuẩn", 0, true)]);
        Assert.Throws<BusinessRuleValidationException>(() =>
            group.Update("Rau", ModifierSelectionType.Single, true, [new(Guid.NewGuid(), "Lạ", 0, false)]));
    }

    [Fact]
    public void Sales_menu_code_must_be_kebab_case()
    {
        Assert.True(SalesMenu.IsValidCode("thuc-don-chinh"));
        Assert.False(SalesMenu.IsValidCode("Thuc Don"));
        Assert.Throws<BusinessRuleValidationException>(() => SalesMenu.Create("Thuc Don", "Thực đơn", true));
    }

    private static Product NewProduct(decimal price = 50000, decimal? rating = null) =>
        Product.Create("Bánh cuốn", "banh-cuon", Guid.NewGuid(),
            new ProductDetails(price, null, null, true, null, rating, null));
}
