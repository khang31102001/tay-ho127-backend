using AdminPlatform.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Content.Application;

/// <summary>Nội dung dùng media ở: ảnh bài viết + ảnh chèn trong nội dung bài, banner (desktop/mobile),
/// section trang (ảnh chính, gallery và ảnh từng item trong settings).</summary>
public sealed class ContentMediaUsageSource : IMediaUsageSource
{
    private readonly IContentDbContext _db;

    public ContentMediaUsageSource(IContentDbContext db)
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

        var byIdText = targets.ToDictionary(t => t.IdText, t => t.MediaId, StringComparer.OrdinalIgnoreCase);
        var idTexts = byIdText.Keys.ToList();

        var articles = await _db.Articles.AsNoTracking()
            .Where(a => a.FeaturedMediaId != null && idTexts.Contains(a.FeaturedMediaId))
            .Select(a => new { a.Id, a.Title, a.FeaturedMediaId })
            .ToListAsync(cancellationToken);
        usages.AddRange(articles.Select(a => new MediaUsage(byIdText[a.FeaturedMediaId!], "Bài viết", a.Id.ToString(), a.Title, "Ảnh đại diện")));

        foreach (var target in targets.Where(t => !string.IsNullOrWhiteSpace(t.Url)))
        {
            var inContent = await _db.Articles.AsNoTracking()
                .Where(a => a.Content.Contains(target.Url))
                .Select(a => new { a.Id, a.Title })
                .ToListAsync(cancellationToken);
            usages.AddRange(inContent.Select(a => new MediaUsage(target.MediaId, "Bài viết", a.Id.ToString(), a.Title, "Nội dung bài")));
        }

        var banners = await _db.Banners.AsNoTracking()
            .Where(b => (b.DesktopMediaId != null && idTexts.Contains(b.DesktopMediaId))
                || (b.MobileMediaId != null && idTexts.Contains(b.MobileMediaId)))
            .Select(b => new { b.Id, b.Name, b.DesktopMediaId, b.MobileMediaId })
            .ToListAsync(cancellationToken);
        foreach (var banner in banners)
        {
            if (banner.DesktopMediaId is { } desktop && byIdText.TryGetValue(desktop, out var desktopId))
            {
                usages.Add(new MediaUsage(desktopId, "Banner", banner.Id.ToString(), banner.Name, "Ảnh desktop"));
            }

            if (banner.MobileMediaId is { } mobile && byIdText.TryGetValue(mobile, out var mobileId))
            {
                usages.Add(new MediaUsage(mobileId, "Banner", banner.Id.ToString(), banner.Name, "Ảnh mobile"));
            }
        }

        // Settings là JSON (gallery + item) — bảng section nhỏ (vài chục dòng) nên lọc trong bộ nhớ.
        var sections = await _db.PageSections.AsNoTracking()
            .Join(_db.Pages.AsNoTracking(), s => s.PageId, p => p.Id, (s, p) => new { Section = s, PageName = p.Name })
            .ToListAsync(cancellationToken);
        foreach (var row in sections)
        {
            var section = row.Section;
            var name = $"{row.PageName} › {section.Heading ?? section.SectionKey ?? section.Kind.ToString()}";

            if (section.MediaId is { } mediaId && byIdText.TryGetValue(mediaId, out var mainId))
            {
                usages.Add(new MediaUsage(mainId, "Section trang", section.Id.ToString(), name, "Ảnh section"));
            }

            foreach (var galleryId in section.Settings?.GalleryMediaIds ?? [])
            {
                if (byIdText.TryGetValue(galleryId, out var id))
                {
                    usages.Add(new MediaUsage(id, "Section trang", section.Id.ToString(), name, "Gallery"));
                }
            }

            foreach (var item in section.Settings?.Items ?? [])
            {
                if (item.MediaId is { } itemMediaId && byIdText.TryGetValue(itemMediaId, out var id))
                {
                    usages.Add(new MediaUsage(id, "Section trang", section.Id.ToString(), name, $"Ảnh mục \"{item.Title ?? item.Label}\""));
                }
            }
        }

        return usages;
    }
}
