using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Seo.Application;
using AdminPlatform.Modules.Seo.Application.Metadata;
using AdminPlatform.Modules.Seo.Domain;
using AdminPlatform.Modules.Seo.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.UnitTests.Seo;

public class SeoMetadataTests
{
    private static readonly CancellationToken None = CancellationToken.None;

    private static ISeoDbContext NewDb() =>
        new SeoDbContext(new DbContextOptionsBuilder<SeoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static SeoMetadataDetails Details(string? title = "Tiêu đề", string? canonical = null, bool index = true) =>
        new(title, "Mô tả", canonical, index, true, null, null, null, null, null, null);

    private static UpsertSeoMetadataRequest Request(
        string type = "product", string? id = "p-1", string? title = "Tiêu đề", string? canonical = null, bool index = true) =>
        new(type, id, title, "Mô tả", canonical, index, true, " og ", null, " media-1 ", null, null, null);

    // ---------- Domain ----------

    [Fact]
    public void Create_RequiresAnEntityIdExceptForTheHomepage()
    {
        Assert.ThrowsAny<Exception>(() => SeoMetadata.Create(SeoEntityType.Product, " ", Details()));

        var homepage = SeoMetadata.Create(SeoEntityType.Homepage, "ignored", Details());
        Assert.Null(homepage.EntityId);

        Assert.Equal("p-1", SeoMetadata.Create(SeoEntityType.Product, " p-1 ", Details()).EntityId);
    }

    [Theory]
    [InlineData("/thuc-don/banh-cuon", true)]
    [InlineData("https://tayho127.vn/x", true)]
    [InlineData("//evil.com", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:text/html,x", false)]
    [InlineData("not a url", false)]
    public void CanonicalUrl_MustBeASitePathOrHttpUrl(string url, bool valid)
    {
        if (valid)
        {
            Assert.Equal(url, SeoMetadata.Create(SeoEntityType.Page, "x", Details(canonical: url)).CanonicalUrl);
        }
        else
        {
            Assert.Throws<BusinessRuleValidationException>(() => SeoMetadata.Create(SeoEntityType.Page, "x", Details(canonical: url)));
        }
    }

    [Fact]
    public void Update_CleansBlankFieldsToNull()
    {
        var metadata = SeoMetadata.Create(SeoEntityType.Article, "a", Details(title: "  ", canonical: ""));

        Assert.Null(metadata.MetaTitle);
        Assert.Null(metadata.CanonicalUrl);
    }

    [Theory]
    [InlineData("product", true)]
    [InlineData("HOMEPAGE", true)]
    [InlineData(" article ", true)]
    [InlineData("1", false)]
    [InlineData("order", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void EntityTypeWire_ParsesOnlyKnownNames(string? value, bool valid)
    {
        Assert.Equal(valid, SeoEntityTypeWire.TryParse(value, out _));
    }

    // ---------- Service ----------

    [Fact]
    public async Task Upsert_CreatesThenReplacesWithoutAddingARow()
    {
        var db = NewDb();
        var service = new SeoMetadataService(db);

        var created = await service.UpsertAsync(Request(), None);
        var updated = await service.UpsertAsync(Request(title: "Mới", index: false), None);

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Mới", updated.MetaTitle);
        Assert.False(updated.RobotsIndex);
        Assert.Equal("og", updated.OgTitle);
        Assert.Equal("media-1", updated.OgImageMediaId);
        Assert.Equal("product", updated.EntityType);
        Assert.Equal(1, await db.SeoMetadata.CountAsync(None));
    }

    [Fact]
    public async Task Upsert_KeepsOneOverridePerEntityAndTreatsTheHomepageAsASingleton()
    {
        var db = NewDb();
        var service = new SeoMetadataService(db);

        await service.UpsertAsync(Request(id: "p-1"), None);
        await service.UpsertAsync(Request(id: "p-2"), None);
        await service.UpsertAsync(Request(type: "article", id: "p-1"), None);
        await service.UpsertAsync(Request(type: "homepage", id: null), None);
        await service.UpsertAsync(Request(type: "homepage", id: "whatever"), None);

        Assert.Equal(4, await db.SeoMetadata.CountAsync(None));
    }

    [Fact]
    public async Task Find_ReturnsNullWhenThereIsNoOverrideAndFindsTheHomepageWithoutAnId()
    {
        var service = new SeoMetadataService(NewDb());
        await service.UpsertAsync(Request(type: "homepage", id: null, title: "Trang chủ"), None);

        Assert.Null(await service.FindAsync("product", "missing", None));
        Assert.Equal("Trang chủ", (await service.FindAsync("homepage", null, None))!.MetaTitle);
        Assert.Throws<BusinessRuleValidationException>(() => service.FindAsync("nope", "x", None).GetAwaiter().GetResult());
    }

    [Fact]
    public async Task Reset_RemovesTheOverrideAndIsIdempotent()
    {
        var db = NewDb();
        var service = new SeoMetadataService(db);
        await service.UpsertAsync(Request(), None);

        await service.ResetAsync("product", "p-1", None);
        await service.ResetAsync("product", "p-1", None);

        Assert.Equal(0, await db.SeoMetadata.CountAsync(None));
    }

    [Fact]
    public async Task List_FiltersByEntityTypeAndRejectsAnUnknownOne()
    {
        var service = new SeoMetadataService(NewDb());
        await service.UpsertAsync(Request(id: "p-1"), None);
        await service.UpsertAsync(Request(type: "article", id: "a-1"), None);

        var products = await service.ListAsync(new PagedRequest(), "product", None);
        var all = await service.ListAsync(new PagedRequest(), null, None);

        Assert.Single(products.Items);
        Assert.Equal(2, all.TotalItems);
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => service.ListAsync(new PagedRequest(), "nope", None));
    }

    [Fact]
    public async Task ListNoIndex_ReturnsOnlyEntitiesWithIndexSwitchedOff()
    {
        var service = new SeoMetadataService(NewDb());
        await service.UpsertAsync(Request(id: "p-1", index: false), None);
        await service.UpsertAsync(Request(id: "p-2", index: true), None);
        await service.UpsertAsync(Request(type: "homepage", id: null, index: false), None);

        var noIndex = await service.ListNoIndexAsync(None);

        Assert.Equal(2, noIndex.Count);
        Assert.Contains(noIndex, e => e is { EntityType: "product", EntityId: "p-1" });
        Assert.Contains(noIndex, e => e is { EntityType: "homepage", EntityId: null });
    }

    // ---------- Validator ----------

    [Fact]
    public void Validator_RequiresAKnownTypeAndAnIdExceptForTheHomepage()
    {
        var validator = new UpsertSeoMetadataRequestValidator();

        Assert.True(validator.Validate(Request()).IsValid);
        Assert.True(validator.Validate(Request(type: "homepage", id: null)).IsValid);
        Assert.False(validator.Validate(Request(type: "product", id: null)).IsValid);
        Assert.False(validator.Validate(Request(type: "nope")).IsValid);
        Assert.False(validator.Validate(Request(title: new string('a', SeoMetadata.MaxTitleLength + 1))).IsValid);
        Assert.False(validator.Validate(Request(id: new string('a', SeoMetadata.MaxEntityIdLength + 1))).IsValid);
    }
}
