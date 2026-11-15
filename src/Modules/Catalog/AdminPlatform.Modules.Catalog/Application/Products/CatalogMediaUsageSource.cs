using AdminPlatform.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Catalog.Application.Products;

/// <summary>Sản phẩm dùng media ở: ảnh đại diện, danh sách ảnh, ảnh chèn trong mô tả chi tiết (HTML).</summary>
public sealed class CatalogMediaUsageSource : IMediaUsageSource
{
    private const string ModuleName = "Sản phẩm";

    private readonly ICatalogDbContext _db;

    public CatalogMediaUsageSource(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MediaUsage>> FindUsagesAsync(IReadOnlyCollection<MediaUsageTarget> targets, CancellationToken cancellationToken)
    {
        var usages = new List<MediaUsage>();
        if (targets.Count == 0)
        {
            return usages;
        }

        var ids = targets.Select(t => t.MediaId).ToList();
        var idTexts = targets.Select(t => t.IdText).ToList();

        var thumbnails = await _db.Products.AsNoTracking()
            .Where(p => p.ThumbnailMediaId != null && ids.Contains(p.ThumbnailMediaId.Value))
            .Select(p => new { p.Id, p.Name, MediaId = p.ThumbnailMediaId!.Value })
            .ToListAsync(cancellationToken);
        usages.AddRange(thumbnails.Select(p => new MediaUsage(p.MediaId, ModuleName, p.Id.ToString(), p.Name, "Ảnh đại diện")));

        var galleries = await _db.Products.AsNoTracking()
            .SelectMany(p => p.Media.Where(m => idTexts.Contains(m.MediaId)).Select(m => new { p.Id, p.Name, m.MediaId }))
            .ToListAsync(cancellationToken);
        usages.AddRange(galleries.Select(p => new MediaUsage(Guid.Parse(p.MediaId), ModuleName, p.Id.ToString(), p.Name, "Danh sách ảnh")));

        foreach (var target in targets.Where(t => !string.IsNullOrWhiteSpace(t.Url)))
        {
            var inContent = await _db.Products.AsNoTracking()
                .Where(p => p.Content != null && p.Content.Contains(target.Url))
                .Select(p => new { p.Id, p.Name })
                .ToListAsync(cancellationToken);
            usages.AddRange(inContent.Select(p => new MediaUsage(target.MediaId, ModuleName, p.Id.ToString(), p.Name, "Mô tả chi tiết")));
        }

        return usages;
    }
}
