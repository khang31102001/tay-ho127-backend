using AdminPlatform.Common.Abstractions;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Media.Application;
using AdminPlatform.Modules.Media.Application.Media;
using AdminPlatform.Modules.Media.Domain;
using AdminPlatform.Modules.Media.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MediaEntity = AdminPlatform.Modules.Media.Domain.Media;

namespace AdminPlatform.UnitTests.Media;

/// <summary>Nhóm kho ảnh (media_categories): media được gán nhóm lúc upload/tạo, lọc theo nhóm, đổi nhóm, và
/// gán nhóm hàng loạt cho media có sẵn theo nơi đang dùng.</summary>
public class MediaCategoryTests
{
    private static async Task<MediaDbContext> NewDbWithCategoriesAsync()
    {
        var db = new MediaDbContext(new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        db.MediaCategories.AddRange(
            MediaCategory.Create(MediaCategoryCodes.Products, "Sản phẩm", 10),
            MediaCategory.Create(MediaCategoryCodes.Articles, "Bài viết", 20),
            MediaCategory.Create(MediaCategoryCodes.General, "Chung", 90));
        await db.SaveChangesAsync();
        return db;
    }

    private sealed class FakeStorage : IFileStorage
    {
        public MediaStorageProvider Provider => MediaStorageProvider.Local;

        public Task<string> SaveAsync(string storedFileName, Stream content, CancellationToken cancellationToken) =>
            Task.FromResult("/uploads/" + storedFileName);

        public Task DeleteAsync(string url, MediaKind kind, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static MediaService NewService(IMediaDbContext db) =>
        new(db, [new FakeStorage()], Options.Create(new MediaStorageOptions()));

    private static MediaUploadFile PngFile()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.Concat(new byte[32]).ToArray();
        return new MediaUploadFile(new MemoryStream(bytes), "photo.png", bytes.Length);
    }

    [Fact]
    public async Task UploadAsync_assigns_the_requested_category()
    {
        var sut = NewService(await NewDbWithCategoriesAsync());

        var created = await sut.UploadAsync(PngFile(), null, null, "Products", "https://api.example.com", CancellationToken.None);

        Assert.Equal("products", created.CategoryCode);
        Assert.Equal("Sản phẩm", created.CategoryName);
        Assert.NotNull(created.CategoryId);
    }

    [Fact]
    public async Task UploadAsync_defaults_to_the_general_category()
    {
        var sut = NewService(await NewDbWithCategoriesAsync());

        var created = await sut.UploadAsync(PngFile(), null, null, null, "https://api.example.com", CancellationToken.None);

        Assert.Equal("general", created.CategoryCode);
    }

    [Fact]
    public async Task UploadAsync_rejects_an_unknown_category_without_saving_anything()
    {
        var db = await NewDbWithCategoriesAsync();
        var sut = NewService(db);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.UploadAsync(PngFile(), null, null, "nowhere", "https://api.example.com", CancellationToken.None));

        Assert.Empty(db.Media);
    }

    [Fact]
    public async Task CreateAsync_for_an_external_link_uses_the_given_category()
    {
        var sut = NewService(await NewDbWithCategoriesAsync());

        var created = await sut.CreateAsync(
            new CreateMediaRequest("a.jpg", "https://cdn.example.com/a.jpg", "Image", null, 10, "articles"), CancellationToken.None);

        Assert.Equal("articles", created.CategoryCode);
    }

    [Fact]
    public async Task ListAsync_filters_by_category()
    {
        var sut = NewService(await NewDbWithCategoriesAsync());
        await sut.UploadAsync(PngFile(), null, null, "products", "https://api.example.com", CancellationToken.None);
        await sut.UploadAsync(PngFile(), null, null, "articles", "https://api.example.com", CancellationToken.None);

        var products = await sut.ListAsync(new PagedRequest(), null, null, null, "products", CancellationToken.None);

        Assert.Equal("products", Assert.Single(products.Items).CategoryCode);
        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.ListAsync(new PagedRequest(), null, null, null, "nowhere", CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_changes_category_only_when_one_is_given()
    {
        var sut = NewService(await NewDbWithCategoriesAsync());
        var created = await sut.UploadAsync(PngFile(), null, null, "products", "https://api.example.com", CancellationToken.None);

        var kept = await sut.UpdateAsync(created.Id, new UpdateMediaRequest("photo.png", created.Url, "Image", "alt"), CancellationToken.None);
        Assert.Equal("products", kept.CategoryCode);

        var moved = await sut.UpdateAsync(created.Id, new UpdateMediaRequest("photo.png", created.Url, "Image", "alt", "articles"), CancellationToken.None);
        Assert.Equal("articles", moved.CategoryCode);
    }

    [Fact]
    public async Task ListCategoriesAsync_returns_categories_in_display_order()
    {
        var sut = NewService(await NewDbWithCategoriesAsync());

        var categories = await sut.ListCategoriesAsync(CancellationToken.None);

        Assert.Equal(["products", "articles", "general"], categories.Select(c => c.Code));
    }

    private sealed class FakeUsageSource(Dictionary<Guid, string> moduleByMedia) : IMediaUsageSource
    {
        public Task<IReadOnlyList<MediaUsage>> FindUsagesAsync(IReadOnlyCollection<MediaUsageTarget> targets, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MediaUsage>>(targets
                .Where(t => moduleByMedia.ContainsKey(t.MediaId))
                .Select(t => new MediaUsage(t.MediaId, moduleByMedia[t.MediaId], "e", "Entity", "Field"))
                .ToList());
    }

    [Fact]
    public async Task Backfill_assigns_by_usage_falls_back_to_general_and_keeps_existing_categories()
    {
        var db = await NewDbWithCategoriesAsync();
        var articlesId = await db.MediaCategories.Where(c => c.Code == "articles").Select(c => c.Id).SingleAsync();

        var usedByProduct = MediaEntity.Create("p.png", "https://x.test/p.png", MediaKind.Image, null, 1, MediaStorageProvider.External);
        var unused = MediaEntity.Create("u.png", "https://x.test/u.png", MediaKind.Image, null, 1, MediaStorageProvider.External);
        var unknownModule = MediaEntity.Create("k.png", "https://x.test/k.png", MediaKind.Image, null, 1, MediaStorageProvider.External);
        var alreadyCategorized = MediaEntity.Create("a.png", "https://x.test/a.png", MediaKind.Image, null, 1, MediaStorageProvider.External, articlesId);
        db.Media.AddRange(usedByProduct, unused, unknownModule, alreadyCategorized);
        await db.SaveChangesAsync();

        var services = new ServiceCollection()
            .AddSingleton<IMediaDbContext>(db)
            .AddSingleton<IMediaUsageSource>(new FakeUsageSource(new Dictionary<Guid, string>
            {
                [usedByProduct.Id] = "Sản phẩm",
                [unknownModule.Id] = "Module lạ",
                [alreadyCategorized.Id] = "Sản phẩm",
            }))
            .BuildServiceProvider();

        var assigned = await MediaCategoryBackfill.RunAsync(services, CancellationToken.None);

        var codes = await db.Media.Join(db.MediaCategories, m => m.CategoryId, c => c.Id, (m, c) => new { m.FileName, c.Code })
            .ToDictionaryAsync(x => x.FileName, x => x.Code);
        Assert.Equal(3, assigned);
        Assert.Equal("products", codes["p.png"]);
        Assert.Equal("general", codes["u.png"]);
        Assert.Equal("general", codes["k.png"]);
        Assert.Equal("articles", codes["a.png"]);
    }
}
