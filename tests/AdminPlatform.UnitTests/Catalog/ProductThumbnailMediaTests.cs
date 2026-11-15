using AdminPlatform.Modules.Catalog.Application;
using AdminPlatform.Modules.Catalog.Application.Categories;
using AdminPlatform.Modules.Catalog.Application.Ports;
using AdminPlatform.Modules.Catalog.Application.Products;
using AdminPlatform.Modules.Catalog.Infrastructure;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.UnitTests.Catalog;

/// <summary>Media giả cho test: chỉ các id đã Add mới "tồn tại".</summary>
internal sealed class FakeCatalogMediaLookup : ICatalogMediaLookup
{
    private readonly Dictionary<Guid, CatalogMediaInfo> _media = [];

    public Guid Add(bool isImage = true, bool isActive = true)
    {
        var id = Guid.NewGuid();
        _media[id] = new CatalogMediaInfo(id, $"https://cdn.test/{id}.png", isImage, isActive);
        return id;
    }

    public void Deactivate(Guid id) => _media[id] = _media[id] with { IsActive = false };

    public Task<IReadOnlyDictionary<Guid, CatalogMediaInfo>> FindAsync(IReadOnlyCollection<Guid> mediaIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, CatalogMediaInfo>>(
            mediaIds.Where(_media.ContainsKey).Distinct().ToDictionary(id => id, id => _media[id]));
}

public class ProductThumbnailMediaTests
{
    private static readonly CancellationToken None = CancellationToken.None;

    private static ICatalogDbContext NewDb() =>
        new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<Guid> LeafCategoryAsync(ICatalogDbContext db)
    {
        var categories = new CategoryService(db);
        var group = await categories.CreateAsync(new CreateCategoryRequest("Món ăn", null, 1, true), None);
        return (await categories.CreateAsync(new CreateCategoryRequest("Bánh cuốn", group.Id, 1, true), None)).Id;
    }

    private static CreateProductRequest Create(Guid categoryId, Guid? thumbnail, params string[] mediaIds) =>
        new("Bánh cuốn thịt", null, categoryId, 45000, null, null, null, true, null, null, null, mediaIds, [], thumbnail);

    private static UpdateProductRequest Update(Guid categoryId, Guid? thumbnail, params string[] mediaIds) =>
        new("Bánh cuốn thịt", null, categoryId, 45000, null, null, null, true, null, null, null, mediaIds, [], thumbnail);

    [Fact]
    public async Task Thumbnail_is_stored_as_media_id_and_url_is_resolved_from_media()
    {
        var db = NewDb();
        var media = new FakeCatalogMediaLookup();
        var thumbnail = media.Add();
        var gallery1 = media.Add();
        var gallery2 = media.Add();
        var sut = new ProductService(db, new ProductContentSanitizer(), media);

        var created = await sut.CreateAsync(Create(await LeafCategoryAsync(db), thumbnail, gallery2.ToString(), gallery1.ToString()), None);

        Assert.Equal(thumbnail, created.ThumbnailMediaId);
        Assert.Equal($"https://cdn.test/{thumbnail}.png", created.ThumbnailUrl);
        Assert.Equal([gallery2.ToString(), gallery1.ToString()], created.MediaIds);

        var stored = await db.Products.AsNoTracking().Include(p => p.Media).SingleAsync();
        Assert.Equal(thumbnail, stored.ThumbnailMediaId);
        Assert.Equal(0, stored.Media.Single(m => m.MediaId == gallery2.ToString()).SortOrder);

        var listed = (await sut.ListAsync(new AdminPlatform.Common.Pagination.PagedRequest(), null, null, None)).Items.Single();
        Assert.Equal(created.ThumbnailUrl, listed.ThumbnailUrl);
    }

    [Fact]
    public async Task Thumbnail_can_be_changed_and_cleared()
    {
        var db = NewDb();
        var media = new FakeCatalogMediaLookup();
        var first = media.Add();
        var second = media.Add();
        var sut = new ProductService(db, new ProductContentSanitizer(), media);
        var categoryId = await LeafCategoryAsync(db);
        var product = await sut.CreateAsync(Create(categoryId, first), None);

        Assert.Equal(second, (await sut.UpdateAsync(product.Id, Update(categoryId, second), None)).ThumbnailMediaId);

        var cleared = await sut.UpdateAsync(product.Id, Update(categoryId, null), None);
        Assert.Null(cleared.ThumbnailMediaId);
        Assert.Null(cleared.ThumbnailUrl);
    }

    [Fact]
    public async Task Missing_or_non_image_or_inactive_media_is_rejected()
    {
        var db = NewDb();
        var media = new FakeCatalogMediaLookup();
        var video = media.Add(isImage: false);
        var hidden = media.Add(isActive: false);
        var sut = new ProductService(db, new ProductContentSanitizer(), media);
        var categoryId = await LeafCategoryAsync(db);

        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(Create(categoryId, Guid.NewGuid()), None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(Create(categoryId, video), None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(Create(categoryId, hidden), None));
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => sut.CreateAsync(Create(categoryId, null, Guid.NewGuid().ToString()), None));
        Assert.Empty(db.Products);
    }

    [Fact]
    public async Task Already_linked_media_that_was_hidden_later_can_be_kept()
    {
        var db = NewDb();
        var media = new FakeCatalogMediaLookup();
        var thumbnail = media.Add();
        var gallery = media.Add();
        var sut = new ProductService(db, new ProductContentSanitizer(), media);
        var categoryId = await LeafCategoryAsync(db);
        var product = await sut.CreateAsync(Create(categoryId, thumbnail, gallery.ToString()), None);

        media.Deactivate(thumbnail);
        media.Deactivate(gallery);

        var updated = await sut.UpdateAsync(product.Id, Update(categoryId, thumbnail, gallery.ToString()), None);
        Assert.Equal(thumbnail, updated.ThumbnailMediaId);

        var newHidden = media.Add(isActive: false);
        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => sut.UpdateAsync(product.Id, Update(categoryId, thumbnail, gallery.ToString(), newHidden.ToString()), None));
    }
}
