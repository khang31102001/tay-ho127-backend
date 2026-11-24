using AdminPlatform.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Organization.Application;

/// <summary>Thông tin thương hiệu dùng media ở logo (thường/tối/sáng), favicon và ảnh chia sẻ (OG).</summary>
public sealed class OrganizationMediaUsageSource : IMediaUsageSource, IMediaUsageDetacher
{
    private readonly IOrganizationDbContext _db;

    public OrganizationMediaUsageSource(IOrganizationDbContext db)
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

        var profiles = await _db.BrandProfiles.AsNoTracking()
            .Where(b => (b.LogoMediaId != null && idTexts.Contains(b.LogoMediaId))
                || (b.LogoDarkMediaId != null && idTexts.Contains(b.LogoDarkMediaId))
                || (b.LogoLightMediaId != null && idTexts.Contains(b.LogoLightMediaId))
                || (b.FaviconMediaId != null && idTexts.Contains(b.FaviconMediaId))
                || (b.OgImageMediaId != null && idTexts.Contains(b.OgImageMediaId)))
            .ToListAsync(cancellationToken);

        var usages = new List<MediaUsage>();
        foreach (var profile in profiles)
        {
            foreach (var (mediaId, field) in new[]
            {
                (profile.LogoMediaId, "Logo"),
                (profile.LogoDarkMediaId, "Logo nền tối"),
                (profile.LogoLightMediaId, "Logo nền sáng"),
                (profile.FaviconMediaId, "Favicon"),
                (profile.OgImageMediaId, "Ảnh chia sẻ (OG)"),
            })
            {
                if (mediaId is not null && byIdText.TryGetValue(mediaId, out var id))
                {
                    usages.Add(new MediaUsage(id, "Thông tin thương hiệu", profile.Id.ToString(), profile.Name, field));
                }
            }
        }

        return usages;
    }

    public async Task DetachAsync(IReadOnlyCollection<MediaUsageTarget> targets, CancellationToken cancellationToken)
    {
        var idTexts = targets.Select(t => t.IdText).ToList();
        if (idTexts.Count == 0)
        {
            return;
        }

        await _db.BrandProfiles.Where(b => b.LogoMediaId != null && idTexts.Contains(b.LogoMediaId))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.LogoMediaId, (string?)null), cancellationToken);
        await _db.BrandProfiles.Where(b => b.LogoDarkMediaId != null && idTexts.Contains(b.LogoDarkMediaId))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.LogoDarkMediaId, (string?)null), cancellationToken);
        await _db.BrandProfiles.Where(b => b.LogoLightMediaId != null && idTexts.Contains(b.LogoLightMediaId))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.LogoLightMediaId, (string?)null), cancellationToken);
        await _db.BrandProfiles.Where(b => b.FaviconMediaId != null && idTexts.Contains(b.FaviconMediaId))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.FaviconMediaId, (string?)null), cancellationToken);
        await _db.BrandProfiles.Where(b => b.OgImageMediaId != null && idTexts.Contains(b.OgImageMediaId))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.OgImageMediaId, (string?)null), cancellationToken);
    }
}
