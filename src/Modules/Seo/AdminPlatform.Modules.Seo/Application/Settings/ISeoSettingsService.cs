namespace AdminPlatform.Modules.Seo.Application.Settings;

public interface ISeoSettingsService
{
    /// <summary>The settings, or null when the seed has not created them yet.</summary>
    Task<SeoSettingsResponse?> GetAsync(CancellationToken cancellationToken);

    /// <summary>Replaces the settings; creates them when absent (so an Admin can save even before the seed ran).</summary>
    Task<SeoSettingsResponse> UpdateAsync(UpdateSeoSettingsRequest request, CancellationToken cancellationToken);
}
