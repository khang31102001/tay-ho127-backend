using AdminPlatform.Modules.Seo.Application;
using AdminPlatform.Modules.Seo.Application.Schemas;
using AdminPlatform.Modules.Seo.Domain;
using AdminPlatform.Modules.Seo.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.UnitTests.Seo;

public class SeoSchemaTests
{
    private static readonly CancellationToken None = CancellationToken.None;
    private const string ValidJsonLd = """{"@context":"https://schema.org","@type":"Product","name":"Bánh cuốn"}""";

    private static ISeoDbContext NewDb() =>
        new SeoDbContext(new DbContextOptionsBuilder<SeoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static SeoSchemaDetails Details(
        Dictionary<string, string>? config = null, string? json = null, bool custom = false, bool active = true) =>
        new(config, json, custom, active);

    private static UpsertSeoSchemaRequest Request(
        string type = "product", string? id = "p-1", string schema = "Product", Dictionary<string, string>? config = null,
        string? json = null, bool custom = false, bool active = true) =>
        new(type, id, schema, config, json, custom, active);

    // ---------- Domain ----------

    [Fact]
    public void Create_RequiresAnEntityIdExceptForTheHomepage()
    {
        Assert.ThrowsAny<Exception>(() => SeoSchema.Create(SeoEntityType.Product, " ", SeoSchemaType.Product, Details()));
        Assert.Null(SeoSchema.Create(SeoEntityType.Homepage, "ignored", SeoSchemaType.WebSite, Details()).EntityId);
    }

    [Theory]
    [InlineData("""{"@type":"Product"}""", true)]
    [InlineData("""[{"@type":"Product"},{"@type":"Offer"}]""", true)]
    [InlineData("{ not json", false)]
    [InlineData("\"just a string\"", false)]
    [InlineData("42", false)]
    public void CustomJsonLd_MustBeAJsonObjectOrArray(string json, bool valid)
    {
        if (valid)
        {
            Assert.Equal(json, SeoSchema.Create(SeoEntityType.Product, "p", SeoSchemaType.Product, Details(json: json)).CustomJsonLd);
        }
        else
        {
            Assert.Throws<BusinessRuleValidationException>(() =>
                SeoSchema.Create(SeoEntityType.Product, "p", SeoSchemaType.Product, Details(json: json)));
        }
    }

    [Fact]
    public void AdvancedMode_NeedsAJsonLd()
    {
        Assert.Throws<BusinessRuleValidationException>(() =>
            SeoSchema.Create(SeoEntityType.Product, "p", SeoSchemaType.Product, Details(custom: true)));
        Assert.Throws<BusinessRuleValidationException>(() =>
            SeoSchema.Create(SeoEntityType.Product, "p", SeoSchemaType.Product, Details(json: "  ", custom: true)));
        Assert.True(SeoSchema.Create(SeoEntityType.Product, "p", SeoSchemaType.Product, Details(json: ValidJsonLd, custom: true)).IsCustomOverride);
    }

    [Fact]
    public void Config_DropsBlanksTrimsAndRoundTrips()
    {
        var schema = SeoSchema.Create(SeoEntityType.Product, "p", SeoSchemaType.Product,
            Details(new Dictionary<string, string> { [" sku "] = " BC-001 ", ["brand"] = "  ", [" "] = "x" }));

        Assert.Equal(new Dictionary<string, string> { ["sku"] = "BC-001" }, schema.Config);
    }

    [Fact]
    public void Config_IsNullWhenEmptyAndRejectsTooManyOrTooLongEntries()
    {
        Assert.Null(SeoSchema.Create(SeoEntityType.Product, "p", SeoSchemaType.Product, Details(new Dictionary<string, string>())).ConfigJson);

        var tooMany = Enumerable.Range(0, SeoSchema.MaxConfigEntries + 1).ToDictionary(i => $"k{i}", _ => "v");
        Assert.Throws<BusinessRuleValidationException>(() => SeoSchema.Create(SeoEntityType.Product, "p", SeoSchemaType.Product, Details(tooMany)));

        var tooLong = new Dictionary<string, string> { ["sku"] = new string('a', SeoSchema.MaxConfigValueLength + 1) };
        Assert.Throws<BusinessRuleValidationException>(() => SeoSchema.Create(SeoEntityType.Product, "p", SeoSchemaType.Product, Details(tooLong)));
    }

    [Theory]
    [InlineData("Product", true)]
    [InlineData("faqpage", true)]
    [InlineData(" Article ", true)]
    [InlineData("1", false)]
    [InlineData("Movie", false)]
    [InlineData(null, false)]
    public void SchemaTypeWire_ParsesOnlyKnownNames(string? value, bool valid)
    {
        Assert.Equal(valid, SeoSchemaTypeWire.TryParse(value, out _));
    }

    // ---------- Service ----------

    [Fact]
    public async Task Upsert_CreatesThenReplacesWithoutAddingARow()
    {
        var db = NewDb();
        var service = new SeoSchemaService(db);

        var created = await service.UpsertAsync(Request(config: new() { ["sku"] = "A" }), None);
        var updated = await service.UpsertAsync(Request(config: new() { ["sku"] = "B" }, json: ValidJsonLd, custom: true), None);

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("B", updated.Config!["sku"]);
        Assert.True(updated.IsCustomOverride);
        Assert.Equal("Product", updated.SchemaType);
        Assert.Equal("product", updated.EntityType);
        Assert.Equal(1, await db.SeoSchemas.CountAsync(None));
    }

    [Fact]
    public async Task Upsert_KeepsOneRowPerEntityAndSchemaType()
    {
        var db = NewDb();
        var service = new SeoSchemaService(db);

        await service.UpsertAsync(Request(id: "p-1", schema: "Product"), None);
        await service.UpsertAsync(Request(id: "p-1", schema: "FAQPage"), None);
        await service.UpsertAsync(Request(id: "p-2", schema: "Product"), None);
        await service.UpsertAsync(Request(type: "homepage", id: null, schema: "WebSite"), None);
        await service.UpsertAsync(Request(type: "homepage", id: "whatever", schema: "WebSite"), None);

        Assert.Equal(4, await db.SeoSchemas.CountAsync(None));
    }

    [Fact]
    public async Task Find_ReturnsNullWithoutARowAndRejectsUnknownTypes()
    {
        var service = new SeoSchemaService(NewDb());
        await service.UpsertAsync(Request(), None);

        Assert.NotNull(await service.FindAsync("product", "p-1", "Product", None));
        Assert.Null(await service.FindAsync("product", "p-1", "Article", None));
        Assert.Null(await service.FindAsync("product", "other", "Product", None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => service.FindAsync("nope", "p-1", "Product", None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => service.FindAsync("product", "p-1", "Movie", None));
    }

    [Fact]
    public async Task FindActive_IgnoresRowsThatAreSwitchedOff()
    {
        var service = new SeoSchemaService(NewDb());
        await service.UpsertAsync(Request(active: false), None);

        Assert.Null(await service.FindActiveAsync("product", "p-1", "Product", None));
        Assert.NotNull(await service.FindAsync("product", "p-1", "Product", None));

        await service.UpsertAsync(Request(active: true), None);
        Assert.NotNull(await service.FindActiveAsync("product", "p-1", "Product", None));
    }

    [Fact]
    public async Task Reset_RemovesTheRowAndIsIdempotent()
    {
        var db = NewDb();
        var service = new SeoSchemaService(db);
        await service.UpsertAsync(Request(), None);

        await service.ResetAsync("product", "p-1", "Product", None);
        await service.ResetAsync("product", "p-1", "Product", None);

        Assert.Equal(0, await db.SeoSchemas.CountAsync(None));
    }

    // ---------- Validator ----------

    [Fact]
    public void Validator_RequiresKnownTypesAnIdExceptForTheHomepageAndBoundsTheJson()
    {
        var validator = new UpsertSeoSchemaRequestValidator();

        Assert.True(validator.Validate(Request()).IsValid);
        Assert.True(validator.Validate(Request(type: "homepage", id: null, schema: "WebSite")).IsValid);
        Assert.False(validator.Validate(Request(id: null)).IsValid);
        Assert.False(validator.Validate(Request(type: "nope")).IsValid);
        Assert.False(validator.Validate(Request(schema: "Movie")).IsValid);
        Assert.False(validator.Validate(Request(json: new string('a', SeoSchema.MaxCustomJsonLdLength + 1))).IsValid);
    }
}
