using AdminPlatform.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Seo.Application;

/// <summary>SEO dùng media ở ảnh OG/Twitter của từng trang và ảnh OG mặc định của site.</summary>
public sealed class SeoMediaUsageSource : IMediaUsageSource, IMediaUsageDetacher
{
    private readonly ISeoDbContext _db;

    public SeoMediaUsageSource(ISeoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MediaUsage>> FindUsagesAsync(IReadOnlyCollection<MediaUsageTarget> targets, CancellationToken cancellationToken)
    {
        var byIdText = targets.ToDictionary(t => t.IdText, t => t.MediaId, StringComparer.OrdinalIgnoreCase);
        var idTexts = byIdText.Keys.ToList();
        if (idTexts.Count == 0)
        {
            return [];
        }

        var usages = new List<MediaUsage>();

        var metadata = await _db.SeoMetadata.AsNoTracking()
            .Where(m => (m.OgImageMediaId != null && idTexts.Contains(m.OgImageMediaId))
                || (m.TwitterImageMediaId != null && idTexts.Contains(m.TwitterImageMediaId)))
            .Select(m => new { m.Id, m.EntityType, m.EntityId, m.OgImageMediaId, m.TwitterImageMediaId })
            .ToListAsync(cancellationToken);
        foreach (var row in metadata)
        {
            var name = row.EntityId is null ? row.EntityType.ToString() : $"{row.EntityType} {row.EntityId}";
            if (row.OgImageMediaId is { } og && byIdText.TryGetValue(og, out var ogId))
            {
                usages.Add(new MediaUsage(ogId, "SEO", row.Id.ToString(), name, "Ảnh OG"));
            }

            if (row.TwitterImageMediaId is { } tw && byIdText.TryGetValue(tw, out var twId))
            {
                usages.Add(new MediaUsage(twId, "SEO", row.Id.ToString(), name, "Ảnh Twitter"));
            }
        }

        var settings = await _db.SeoSettings.AsNoTracking()
            .Where(s => s.DefaultOgImageMediaId != null && idTexts.Contains(s.DefaultOgImageMediaId))
            .Select(s => new { s.Id, s.DefaultOgImageMediaId })
            .ToListAsync(cancellationToken);
        usages.AddRange(settings.Select(s => new MediaUsage(byIdText[s.DefaultOgImageMediaId!], "SEO", s.Id.ToString(), "Cài đặt SEO", "Ảnh OG mặc định")));

        return usages;
    }

    public async Task DetachAsync(IReadOnlyCollection<MediaUsageTarget> targets, CancellationToken cancellationToken)
    {
        var idTexts = targets.Select(t => t.IdText).ToList();
        if (idTexts.Count == 0)
        {
            return;
        }

        await _db.SeoMetadata.Where(m => m.OgImageMediaId != null && idTexts.Contains(m.OgImageMediaId))
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.OgImageMediaId, (string?)null), cancellationToken);
        await _db.SeoMetadata.Where(m => m.TwitterImageMediaId != null && idTexts.Contains(m.TwitterImageMediaId))
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.TwitterImageMediaId, (string?)null), cancellationToken);
        await _db.SeoSettings.Where(x => x.DefaultOgImageMediaId != null && idTexts.Contains(x.DefaultOgImageMediaId))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.DefaultOgImageMediaId, (string?)null), cancellationToken);
    }
}
