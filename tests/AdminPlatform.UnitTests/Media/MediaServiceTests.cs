using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Media.Application;
using AdminPlatform.Modules.Media.Application.Media;
using AdminPlatform.Modules.Media.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.UnitTests.Media;

public class MediaServiceTests
{
    private static IMediaDbContext NewDb() =>
        new MediaDbContext(new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CreateMediaRequest ValidCreateRequest(string fileName = "photo.jpg") =>
        new(fileName, "https://cdn.example.com/" + fileName, "Image", "A photo", 1024);

    [Fact]
    public async Task CreateAsync_persists_media_and_maps_type_to_kind()
    {
        var db = NewDb();
        var sut = new MediaService(db);

        var created = await sut.CreateAsync(ValidCreateRequest(), CancellationToken.None);

        Assert.Equal("Image", created.Type);
        Assert.Equal("Active", created.Status);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task CreateAsync_rejects_an_invalid_type()
    {
        var db = NewDb();
        var sut = new MediaService(db);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.CreateAsync(ValidCreateRequest() with { Type = "not-a-kind" }, CancellationToken.None));
    }

    [Fact]
    public async Task GetByIdAsync_throws_not_found_for_unknown_id()
    {
        var db = NewDb();
        var sut = new MediaService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_changes_metadata_but_keeps_id()
    {
        var db = NewDb();
        var sut = new MediaService(db);
        var created = await sut.CreateAsync(ValidCreateRequest(), CancellationToken.None);

        var updated = await sut.UpdateAsync(
            created.Id, new UpdateMediaRequest("clip.mp4", "https://cdn.example.com/clip.mp4", "Video", "A clip"), CancellationToken.None);

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("clip.mp4", updated.FileName);
        Assert.Equal("Video", updated.Type);
    }

    [Fact]
    public async Task DeleteAsync_deactivates_instead_of_removing_the_row()
    {
        var db = NewDb();
        var sut = new MediaService(db);
        var created = await sut.CreateAsync(ValidCreateRequest(), CancellationToken.None);

        await sut.DeleteAsync(created.Id, CancellationToken.None);

        var fetched = await sut.GetByIdAsync(created.Id, CancellationToken.None);
        Assert.Equal("Inactive", fetched.Status);
    }

    [Fact]
    public async Task ListAsync_filters_by_kind_and_status()
    {
        var db = NewDb();
        var sut = new MediaService(db);
        var image = await sut.CreateAsync(ValidCreateRequest("photo.jpg"), CancellationToken.None);
        await sut.CreateAsync(ValidCreateRequest("clip.mp4") with { Type = "Video" }, CancellationToken.None);
        await sut.DeleteAsync(image.Id, CancellationToken.None);

        var videos = await sut.ListAsync(new PagedRequest(), "Video", null, CancellationToken.None);
        Assert.Single(videos.Items);
        Assert.Equal("Video", videos.Items[0].Type);

        var active = await sut.ListAsync(new PagedRequest(), null, "Active", CancellationToken.None);
        Assert.DoesNotContain(active.Items, m => m.Id == image.Id);
    }

    // Search-by-fileName uses EF.Functions.ILike (same convention as FiscalYearService/SystemSettingService),
    // which only the Npgsql provider can translate — EF Core's InMemory provider cannot execute it, so this
    // path is exercised through the real Postgres provider (integration tests / manual verification) instead.
}
