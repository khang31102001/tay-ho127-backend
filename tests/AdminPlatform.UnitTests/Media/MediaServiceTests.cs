using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Media.Application;
using AdminPlatform.Modules.Media.Application.Media;
using AdminPlatform.Modules.Media.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AdminPlatform.UnitTests.Media;

public class MediaServiceTests
{
    private static IMediaDbContext NewDb() =>
        new MediaDbContext(new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CreateMediaRequest ValidCreateRequest(string fileName = "photo.jpg") =>
        new(fileName, "https://cdn.example.com/" + fileName, "Image", "A photo", 1024);

    private sealed class InMemoryFileStorage : IFileStorage
    {
        public Dictionary<string, byte[]> Files { get; } = new();

        public async Task<string> SaveAsync(string storedFileName, Stream content, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            Files[storedFileName] = buffer.ToArray();
            return "/uploads/" + storedFileName;
        }
    }

    private static MediaService NewService(IMediaDbContext db, IFileStorage? storage = null, MediaStorageOptions? options = null) =>
        new(db, storage ?? new InMemoryFileStorage(), Options.Create(options ?? new MediaStorageOptions()));

    private static MediaUploadFile PngFile(string name = "photo.png", int extraBytes = 32)
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.Concat(new byte[extraBytes]).ToArray();
        return new MediaUploadFile(new MemoryStream(bytes), name, bytes.Length);
    }

    [Fact]
    public async Task UploadAsync_stores_file_and_creates_media_with_absolute_url()
    {
        var storage = new InMemoryFileStorage();
        var sut = NewService(NewDb(), storage);

        var created = await sut.UploadAsync(PngFile(), "Alt", "https://api.example.com", CancellationToken.None);

        Assert.Equal("Image", created.Type);
        Assert.Equal("photo.png", created.FileName);
        Assert.Equal(40, created.Size);
        Assert.StartsWith("https://api.example.com/uploads/", created.Url);
        Assert.EndsWith(".png", created.Url);
        Assert.Single(storage.Files);
    }

    [Fact]
    public async Task UploadAsync_prefers_configured_public_base_url()
    {
        var sut = NewService(NewDb(), options: new MediaStorageOptions { PublicBaseUrl = "https://cdn.example.com/" });

        var created = await sut.UploadAsync(PngFile(), null, "https://api.example.com", CancellationToken.None);

        Assert.StartsWith("https://cdn.example.com/uploads/", created.Url);
    }

    [Fact]
    public async Task UploadAsync_rejects_disallowed_extension()
    {
        var sut = NewService(NewDb());
        var file = new MediaUploadFile(new MemoryStream(new byte[] { 1, 2, 3 }), "malware.exe", 3);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.UploadAsync(file, null, "https://api.example.com", CancellationToken.None));
    }

    [Fact]
    public async Task UploadAsync_rejects_content_that_does_not_match_extension()
    {
        var sut = NewService(NewDb());
        var file = new MediaUploadFile(new MemoryStream(new byte[] { 0x4D, 0x5A, 0x90, 0x00, 1, 2, 3, 4 }), "fake.png", 8);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.UploadAsync(file, null, "https://api.example.com", CancellationToken.None));
    }

    [Fact]
    public async Task UploadAsync_rejects_file_over_size_limit()
    {
        var sut = NewService(NewDb(), options: new MediaStorageOptions { MaxFileBytes = 16 });

        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.UploadAsync(PngFile(), null, "https://api.example.com", CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_persists_media_and_maps_type_to_kind()
    {
        var db = NewDb();
        var sut = NewService(db);

        var created = await sut.CreateAsync(ValidCreateRequest(), CancellationToken.None);

        Assert.Equal("Image", created.Type);
        Assert.Equal("Active", created.Status);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task CreateAsync_rejects_an_invalid_type()
    {
        var db = NewDb();
        var sut = NewService(db);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.CreateAsync(ValidCreateRequest() with { Type = "not-a-kind" }, CancellationToken.None));
    }

    [Fact]
    public async Task GetByIdAsync_throws_not_found_for_unknown_id()
    {
        var db = NewDb();
        var sut = NewService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_changes_metadata_but_keeps_id()
    {
        var db = NewDb();
        var sut = NewService(db);
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
        var sut = NewService(db);
        var created = await sut.CreateAsync(ValidCreateRequest(), CancellationToken.None);

        await sut.DeleteAsync(created.Id, CancellationToken.None);

        var fetched = await sut.GetByIdAsync(created.Id, CancellationToken.None);
        Assert.Equal("Inactive", fetched.Status);
    }

    [Fact]
    public async Task ListAsync_filters_by_kind_and_status()
    {
        var db = NewDb();
        var sut = NewService(db);
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
