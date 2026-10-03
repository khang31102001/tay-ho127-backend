using AdminPlatform.Modules.Seo.Application;
using AdminPlatform.Modules.Seo.Application.Settings;
using AdminPlatform.Modules.Seo.Domain;
using AdminPlatform.Modules.Seo.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.UnitTests.Seo;

public class SeoSettingsTests
{
    private static readonly CancellationToken None = CancellationToken.None;

    private static ISeoDbContext NewDb() =>
        new SeoDbContext(new DbContextOptionsBuilder<SeoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static SeoSettingsDetails Details(
        string template = "%s | Tây Hồ 127", string description = "Mô tả", string[]? paths = null) =>
        new(template, description, null, null, null, true, true, paths ?? ["/admin"]);

    private static UpdateSeoSettingsRequest Request(
        string template = "%s | Tây Hồ 127", string description = "Mô tả", string[]? paths = null) =>
        new(template, description, " media-1 ", "@tayho127", null, true, false, paths ?? ["/admin"]);

    // ---------- Domain ----------

    [Fact]
    public void Create_UsesTheWellKnownSingletonId()
    {
        Assert.Equal(SeoSettings.SingletonId, SeoSettings.Create(Details()).Id);
    }

    [Theory]
    [InlineData("Tây Hồ 127")]
    [InlineData("  ")]
    public void Update_RejectsATitleTemplateWithoutThePlaceholder(string template)
    {
        Assert.ThrowsAny<Exception>(() => SeoSettings.Create(Details(template: template)));
    }

    [Fact]
    public void Update_NormalizesPathsByTrimmingDroppingBlanksAndDuplicates()
    {
        var settings = SeoSettings.Create(Details(paths: [" /admin ", "", "/checkout", "/admin", "  "]));

        Assert.Equal(["/admin", "/checkout"], settings.RobotsDisallowPaths);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("//evil.com")]
    [InlineData("https://tayho127.vn/x")]
    [InlineData("/has space")]
    public void Update_RejectsPathsThatAreNotSitePaths(string path)
    {
        Assert.Throws<BusinessRuleValidationException>(() => SeoSettings.Create(Details(paths: [path])));
    }

    [Fact]
    public void Update_CleansOptionalFields()
    {
        var settings = SeoSettings.Create(new SeoSettingsDetails("%s", "D", "  ", " @site ", "", false, false, []));

        Assert.Null(settings.DefaultOgImageMediaId);
        Assert.Equal("@site", settings.TwitterSite);
        Assert.Null(settings.TwitterCreator);
        Assert.False(settings.DefaultRobotsIndex);
        Assert.False(settings.DefaultRobotsFollow);
        Assert.Empty(settings.RobotsDisallowPaths);
    }

    // ---------- Service ----------

    [Fact]
    public async Task Get_ReturnsNullBeforeAnythingIsSaved()
    {
        var service = new SeoSettingsService(NewDb());

        Assert.Null(await service.GetAsync(None));
    }

    [Fact]
    public async Task Update_CreatesTheSettingsWhenAbsentThenReplacesThemWithoutAddingARow()
    {
        var db = NewDb();
        var service = new SeoSettingsService(db);

        var created = await service.UpdateAsync(Request(), None);
        var updated = await service.UpdateAsync(Request(description: "Mô tả mới", paths: ["/gio-hang"]), None);

        Assert.Equal("media-1", created.DefaultOgImageMediaId);
        Assert.Equal("Mô tả mới", updated.DefaultDescription);
        Assert.Equal(["/gio-hang"], updated.RobotsDisallowPaths);
        Assert.False(updated.DefaultRobotsFollow);
        Assert.Equal(1, await db.SeoSettings.CountAsync(None));
        Assert.Equal("Mô tả mới", (await service.GetAsync(None))!.DefaultDescription);
    }

    // ---------- Validator ----------

    [Fact]
    public void Validator_AcceptsAValidRequest()
    {
        Assert.True(new UpdateSeoSettingsRequestValidator().Validate(Request()).IsValid);
    }

    [Fact]
    public void Validator_RejectsMissingPlaceholderEmptyDescriptionAndTooLongValues()
    {
        var validator = new UpdateSeoSettingsRequestValidator();

        Assert.False(validator.Validate(Request(template: "Tây Hồ")).IsValid);
        Assert.False(validator.Validate(Request(description: "")).IsValid);
        Assert.False(validator.Validate(Request(description: new string('a', SeoSettings.MaxDescriptionLength + 1))).IsValid);
        Assert.False(validator.Validate(Request(paths: [new string('a', SeoSettings.MaxDisallowPathLength + 1)])).IsValid);
        Assert.False(validator.Validate(Request(paths: Enumerable.Range(0, SeoSettings.MaxDisallowPaths + 1).Select(i => $"/p{i}").ToArray())).IsValid);
    }

    // ---------- Seeder ----------

    [Fact]
    public async Task Seeder_CreatesTheDefaultsOnceAndNeverOverwritesAnAdminsEdit()
    {
        var db = NewDb();
        var services = new ServiceCollection().AddSingleton(db).BuildServiceProvider();

        Assert.True(await SeoSeeder.SeedAsync(services, None));
        var seeded = (await new SeoSettingsService(db).GetAsync(None))!;
        Assert.Contains("%s", seeded.DefaultTitleTemplate);
        Assert.Contains("/admin", seeded.RobotsDisallowPaths);

        await new SeoSettingsService(db).UpdateAsync(Request(description: "Admin sửa"), None);

        Assert.False(await SeoSeeder.SeedAsync(services, None));
        Assert.Equal("Admin sửa", (await new SeoSettingsService(db).GetAsync(None))!.DefaultDescription);
    }
}
