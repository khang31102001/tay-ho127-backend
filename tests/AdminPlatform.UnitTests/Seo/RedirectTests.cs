using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Seo.Application;
using AdminPlatform.Modules.Seo.Application.Redirects;
using AdminPlatform.Modules.Seo.Domain;
using AdminPlatform.Modules.Seo.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.UnitTests.Seo;

public class RedirectTests
{
    private static readonly CancellationToken None = CancellationToken.None;

    private static ISeoDbContext NewDb() =>
        new SeoDbContext(new DbContextOptionsBuilder<SeoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CreateRedirectRequest Request(string source = "/thuc-don/mon-cu", string destination = "/thuc-don/mon-moi", int type = 301, bool active = true) =>
        new(source, destination, type, active);

    private static RedirectDetails Details(string source = "/a", string destination = "/b", bool active = true) =>
        new(source, destination, RedirectType.Permanent, active);

    // ---------- Domain ----------

    [Theory]
    [InlineData("/thuc-don/mon-cu", "/thuc-don/mon-cu")]
    [InlineData("thuc-don/mon-cu", "/thuc-don/mon-cu")]
    [InlineData("  /thuc-don/mon-cu/  ", "/thuc-don/mon-cu")]
    [InlineData("/Mon-Cu", "/Mon-Cu")]
    public void NormalizeSourcePath_AddsLeadingSlashDropsTrailingSlashAndKeepsCase(string input, string expected)
    {
        Assert.Equal(expected, Redirect.NormalizeSourcePath(input));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("")]
    [InlineData("/a?x=1")]
    [InlineData("/a#top")]
    [InlineData("/has space")]
    [InlineData("//evil.com/x")]
    [InlineData("/a\\b")]
    [InlineData("/admin")]
    [InlineData("/Admin/users")]
    [InlineData("/api/v1/x")]
    [InlineData("/_next/static")]
    public void NormalizeSourcePath_RejectsUnsafeOrReservedPaths(string input)
    {
        Assert.ThrowsAny<Exception>(() => Redirect.NormalizeSourcePath(input));
    }

    [Fact]
    public void NormalizeSourcePath_AllowsPathsThatOnlyStartWithAReservedWord()
    {
        Assert.Equal("/administrator", Redirect.NormalizeSourcePath("/administrator"));
        Assert.Equal("/apple", Redirect.NormalizeSourcePath("/apple"));
    }

    [Theory]
    [InlineData("/thuc-don/x", true)]
    [InlineData("/thuc-don/x?ref=old#top", true)]
    [InlineData("https://tayho127.vn/x", true)]
    [InlineData("//evil.com", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:text/html,x", false)]
    [InlineData("khong-co-gach", false)]
    public void Destination_MustBeASitePathOrHttpUrl(string destination, bool valid)
    {
        if (valid)
        {
            Assert.Equal(destination, Redirect.Create(Details(destination: destination)).DestinationUrl);
        }
        else
        {
            Assert.Throws<BusinessRuleValidationException>(() => Redirect.Create(Details(destination: destination)));
        }
    }

    [Theory]
    [InlineData("/a", "/a")]
    [InlineData("/a", "/a/")]
    [InlineData("a/", "/a?x=1")]
    public void Update_RejectsRedirectingAPathToItself(string source, string destination)
    {
        Assert.Throws<BusinessRuleValidationException>(() => Redirect.Create(Details(source, destination)));
    }

    [Fact]
    public void Update_RejectsAnUnknownRedirectType()
    {
        Assert.Throws<BusinessRuleValidationException>(() =>
            Redirect.Create(new RedirectDetails("/a", "/b", (RedirectType)307, true)));
    }

    // ---------- Service ----------

    [Fact]
    public async Task Create_NormalizesTheSourceAndRoundTripsThroughGet()
    {
        var service = new RedirectService(NewDb());

        var created = await service.CreateAsync(Request(source: "thuc-don/mon-cu/", type: 302), None);
        var read = await service.GetByIdAsync(created.Id, None);

        Assert.Equal("/thuc-don/mon-cu", read.SourcePath);
        Assert.Equal(302, read.RedirectType);
        Assert.True(read.IsActive);
    }

    [Fact]
    public async Task Create_RejectsADuplicateSourceEvenWhenWrittenDifferently()
    {
        var service = new RedirectService(NewDb());
        await service.CreateAsync(Request(source: "/a", destination: "/b"), None);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(Request(source: "a/", destination: "/c"), None));
    }

    [Fact]
    public async Task Update_KeepsItsOwnSourceButCannotTakeAnothersAndReplacesTheFields()
    {
        var service = new RedirectService(NewDb());
        var first = await service.CreateAsync(Request(source: "/a", destination: "/b"), None);
        await service.CreateAsync(Request(source: "/c", destination: "/d"), None);

        var updated = await service.UpdateAsync(first.Id, new UpdateRedirectRequest("/a", "/z", 302, false), None);
        Assert.Equal("/z", updated.DestinationUrl);
        Assert.Equal(302, updated.RedirectType);
        Assert.False(updated.IsActive);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(first.Id, new UpdateRedirectRequest("/c", "/z", 301, true), None));
    }

    [Fact]
    public async Task Create_RejectsALoopDirectlyAndThroughAChainOfActiveRedirects()
    {
        var service = new RedirectService(NewDb());
        await service.CreateAsync(Request(source: "/a", destination: "/b"), None);
        await service.CreateAsync(Request(source: "/b", destination: "/c"), None);

        // /c -> /a would close the loop a -> b -> c -> a.
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => service.CreateAsync(Request(source: "/c", destination: "/a"), None));
        // A redirect that is switched off cannot loop, so it is allowed.
        await service.CreateAsync(Request(source: "/c", destination: "/a", active: false), None);
    }

    [Fact]
    public async Task Update_RejectsActivatingARedirectThatClosesALoopButAllowsAChainThatEndsOutsideTheSite()
    {
        var service = new RedirectService(NewDb());
        await service.CreateAsync(Request(source: "/a", destination: "/b"), None);
        var back = await service.CreateAsync(Request(source: "/b", destination: "/a", active: false), None);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            service.UpdateAsync(back.Id, new UpdateRedirectRequest("/b", "/a", 301, true), None));

        // Pointing at an external URL ends the chain.
        var ok = await service.UpdateAsync(back.Id, new UpdateRedirectRequest("/b", "https://example.com/x", 301, true), None);
        Assert.True(ok.IsActive);
    }

    // The search box uses Postgres ILike, which the in-memory provider cannot run — covered by the integration test.
    [Fact]
    public async Task List_FiltersByActive()
    {
        var service = new RedirectService(NewDb());
        await service.CreateAsync(Request(source: "/mon-cu", destination: "/mon-moi"), None);
        await service.CreateAsync(Request(source: "/bai-cu", destination: "/bai-moi", active: false), None);

        var active = await service.ListAsync(new PagedRequest(), true, None);

        Assert.Single(active.Items);
        Assert.Equal("/mon-cu", active.Items[0].SourcePath);
    }

    [Fact]
    public async Task ListActive_ReturnsOnlyActiveRedirectsWithTheirStatusCode()
    {
        var service = new RedirectService(NewDb());
        await service.CreateAsync(Request(source: "/a", destination: "/b", type: 302), None);
        await service.CreateAsync(Request(source: "/c", destination: "/d", active: false), None);

        var live = await service.ListActiveAsync(None);

        var only = Assert.Single(live);
        Assert.Equal(new PublicRedirectResponse("/a", "/b", 302), only);
    }

    [Fact]
    public async Task Delete_RemovesTheRedirectAndAMissingOneIsNotFound()
    {
        var service = new RedirectService(NewDb());
        var created = await service.CreateAsync(Request(), None);

        await service.DeleteAsync(created.Id, None);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(created.Id, None));
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(created.Id, None));
    }

    // ---------- Validator ----------

    [Fact]
    public void Validator_RequiresPathsAndAKnownRedirectType()
    {
        var validator = new CreateRedirectRequestValidator();

        Assert.True(validator.Validate(Request()).IsValid);
        Assert.False(validator.Validate(Request(source: "")).IsValid);
        Assert.False(validator.Validate(Request(destination: "")).IsValid);
        Assert.False(validator.Validate(Request(type: 307)).IsValid);
        Assert.False(validator.Validate(Request(source: "/" + new string('a', Redirect.MaxPathLength))).IsValid);
    }
}
