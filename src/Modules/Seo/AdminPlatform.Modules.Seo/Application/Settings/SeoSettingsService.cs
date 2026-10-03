using AdminPlatform.Modules.Seo.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Seo.Application.Settings;

public sealed class SeoSettingsService : ISeoSettingsService
{
    private readonly ISeoDbContext _db;

    public SeoSettingsService(ISeoDbContext db)
    {
        _db = db;
    }

    public async Task<SeoSettingsResponse?> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.SeoSettings.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        return settings is null ? null : ToResponse(settings);
    }

    public async Task<SeoSettingsResponse> UpdateAsync(UpdateSeoSettingsRequest request, CancellationToken cancellationToken)
    {
        var details = new SeoSettingsDetails(
            request.DefaultTitleTemplate, request.DefaultDescription, request.DefaultOgImageMediaId, request.TwitterSite,
            request.TwitterCreator, request.DefaultRobotsIndex, request.DefaultRobotsFollow, request.RobotsDisallowPaths);

        var settings = await _db.SeoSettings.SingleOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = SeoSettings.Create(details);
            _db.SeoSettings.Add(settings);
        }
        else
        {
            settings.Update(details);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(settings);
    }

    internal static SeoSettingsResponse ToResponse(SeoSettings settings) => new(
        settings.DefaultTitleTemplate, settings.DefaultDescription, settings.DefaultOgImageMediaId, settings.TwitterSite,
        settings.TwitterCreator, settings.DefaultRobotsIndex, settings.DefaultRobotsFollow, settings.RobotsDisallowPaths,
        settings.UpdatedAtUtc ?? settings.CreatedAtUtc);
}
