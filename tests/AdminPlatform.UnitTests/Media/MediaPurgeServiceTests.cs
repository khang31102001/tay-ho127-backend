using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Media.Application;
using AdminPlatform.Modules.Media.Application.Media;
using AdminPlatform.Modules.Media.Domain;
using AdminPlatform.Modules.Media.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MediaEntity = AdminPlatform.Modules.Media.Domain.Media;

namespace AdminPlatform.UnitTests.Media;

public class MediaPurgeServiceTests
{
    private static readonly CancellationToken None = CancellationToken.None;

    private static IMediaDbContext NewDb() =>
        new MediaDbContext(new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class RecordingStorage(MediaStorageProvider provider, bool fails = false) : IFileStorage
    {
        public MediaStorageProvider Provider => provider;

        public List<string> DeletedUrls { get; } = [];

        public Task<string> SaveAsync(string storedFileName, Stream content, CancellationToken cancellationToken) =>
            Task.FromResult("/uploads/" + storedFileName);

        public Task DeleteAsync(string url, MediaKind kind, CancellationToken cancellationToken)
        {
            if (fails)
            {
                throw new InvalidOperationException("storage down");
            }

            DeletedUrls.Add(url);
            return Task.CompletedTask;
        }
    }

    /// <summary>Giả lập một module đang dùng các media id cho trước.</summary>
    private sealed class FixedUsageSource(params Guid[] usedIds) : IMediaUsageSource
    {
        public Task<IReadOnlyList<MediaUsage>> FindUsagesAsync(IReadOnlyCollection<MediaUsageTarget> targets, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MediaUsage>>(targets
                .Where(t => usedIds.Contains(t.MediaId))
                .Select(t => new MediaUsage(t.MediaId, "Sản phẩm", "p-1", "Bánh cuốn", "Ảnh đại diện"))
                .ToList());
    }

    private static async Task<MediaEntity> AddAsync(IMediaDbContext db, MediaStorageProvider provider, string name = "a.png")
    {
        var media = MediaEntity.Create(name, $"https://tayho127.test/uploads/{Guid.NewGuid():N}.png", MediaKind.Image, null, 10, provider);
        db.Media.Add(media);
        await db.SaveChangesAsync(None);
        return media;
    }

    private static MediaPurgeService NewService(IMediaDbContext db, IFileStorage storage, params IMediaUsageSource[] sources) =>
        new(db, [storage], sources, NullLogger<MediaPurgeService>.Instance);

    [Fact]
    public async Task Unused_media_is_deleted_from_storage_and_database()
    {
        var db = NewDb();
        var storage = new RecordingStorage(MediaStorageProvider.Local);
        var media = await AddAsync(db, MediaStorageProvider.Local);

        var response = await NewService(db, storage, new FixedUsageSource()).PurgeAsync(new PurgeMediaRequest([media.Id]), None);

        Assert.Equal(1, response.DeletedCount);
        Assert.Equal(PurgeMediaStatus.Deleted, Assert.Single(response.Results).Status);
        Assert.Equal([media.Url], storage.DeletedUrls);
        Assert.False(await db.Media.AnyAsync(m => m.Id == media.Id));
    }

    [Fact]
    public async Task Media_in_use_is_skipped_with_its_usages()
    {
        var db = NewDb();
        var storage = new RecordingStorage(MediaStorageProvider.Local);
        var used = await AddAsync(db, MediaStorageProvider.Local, "used.png");
        var free = await AddAsync(db, MediaStorageProvider.Local, "free.png");

        var response = await NewService(db, storage, new FixedUsageSource(used.Id)).PurgeAsync(new PurgeMediaRequest([used.Id, free.Id]), None);

        Assert.Equal(1, response.DeletedCount);
        var skipped = response.Results.Single(r => r.Id == used.Id);
        Assert.Equal(PurgeMediaStatus.InUse, skipped.Status);
        Assert.Equal("Ảnh đại diện", Assert.Single(skipped.Usages).Field);
        Assert.True(await db.Media.AnyAsync(m => m.Id == used.Id));
        Assert.DoesNotContain(used.Url, storage.DeletedUrls);
    }

    [Fact]
    public async Task Storage_failure_keeps_the_record_so_it_can_be_retried()
    {
        var db = NewDb();
        var media = await AddAsync(db, MediaStorageProvider.Local);

        var response = await NewService(db, new RecordingStorage(MediaStorageProvider.Local, fails: true)).PurgeAsync(new PurgeMediaRequest([media.Id]), None);

        Assert.Equal(PurgeMediaStatus.Failed, Assert.Single(response.Results).Status);
        Assert.True(await db.Media.AnyAsync(m => m.Id == media.Id));
    }

    [Fact]
    public async Task External_media_only_loses_its_record_and_unknown_ids_are_reported()
    {
        var db = NewDb();
        var storage = new RecordingStorage(MediaStorageProvider.Local);
        var external = await AddAsync(db, MediaStorageProvider.External);
        var missing = Guid.NewGuid();

        var response = await NewService(db, storage).PurgeAsync(new PurgeMediaRequest([external.Id, missing]), None);

        Assert.Equal(PurgeMediaStatus.Deleted, response.Results.Single(r => r.Id == external.Id).Status);
        Assert.Equal(PurgeMediaStatus.NotFound, response.Results.Single(r => r.Id == missing).Status);
        Assert.Empty(storage.DeletedUrls);
    }

    [Fact]
    public async Task Media_on_a_storage_that_is_not_configured_fails_without_deleting()
    {
        var db = NewDb();
        var media = await AddAsync(db, MediaStorageProvider.Cloudinary);

        var response = await NewService(db, new RecordingStorage(MediaStorageProvider.Local)).PurgeAsync(new PurgeMediaRequest([media.Id]), None);

        Assert.Equal(PurgeMediaStatus.Failed, Assert.Single(response.Results).Status);
        Assert.True(await db.Media.AnyAsync(m => m.Id == media.Id));
    }

    [Fact]
    public async Task Usages_of_a_missing_media_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => NewService(NewDb(), new RecordingStorage(MediaStorageProvider.Local)).GetUsagesAsync(Guid.NewGuid(), None));
    }

    [Theory]
    [InlineData("https://res.cloudinary.com/demo/image/upload/v1712345678/tayho127/abc.png", false, "tayho127/abc")]
    [InlineData("https://res.cloudinary.com/demo/image/upload/tayho127/abc.png", false, "tayho127/abc")]
    [InlineData("https://res.cloudinary.com/demo/raw/upload/v1/tayho127/abc.pdf", true, "tayho127/abc.pdf")]
    public void Cloudinary_public_id_is_read_from_the_url(string url, bool keepExtension, string expected)
    {
        Assert.Equal(expected, CloudinaryFileStorage.PublicIdFromUrl(url, keepExtension));
    }

    [Theory]
    [InlineData("https://tayho127.com/uploads/abc.png", "abc.png")]
    [InlineData("/uploads/abc.png?x=1", "abc.png")]
    [InlineData("/uploads/abc.png#top", "abc.png")]
    [InlineData("https://tayho127.com/uploads/abc.png?v=2", "abc.png")]
    public void Stored_file_name_is_the_last_url_segment(string url, string expected)
    {
        Assert.Equal(expected, StoredFileUrl.LastSegment(url));
    }

    [Theory]
    [InlineData("https://tayho127.com/uploads/")]
    [InlineData("https://tayho127.com/uploads/..%2F..%2Fetc%2Fpasswd")]
    public void Urls_that_could_escape_the_storage_folder_are_rejected(string url)
    {
        Assert.Throws<ArgumentException>(() => StoredFileUrl.LastSegment(url));
    }
}
