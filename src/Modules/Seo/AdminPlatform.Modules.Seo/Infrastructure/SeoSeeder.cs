using AdminPlatform.Modules.Seo.Application;
using AdminPlatform.Modules.Seo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Seo.Infrastructure;

/// <summary>The default SEO settings (title template, description, robots disallow list) taken from the
/// frontend's former mock. Created only while the singleton row does not exist, so it is safe in the
/// every-deploy `seed` step: an admin's edits are never overwritten.</summary>
public static class SeoSeeder
{
    private static readonly SeoSettingsDetails Defaults = new(
        "%s | Bánh Cuốn Tây Hồ 127",
        "Bánh cuốn truyền thống, phục vụ nhanh, hương vị gia đình Bắc giữa Sài Gòn.",
        null,
        null,
        null,
        true,
        true,
        ["/admin", "/checkout", "/gio-hang", "/tai-khoan", "/don-hang", "/payment"]);

    private static readonly SeoMetadataDetails HomepageMetadata = new(
        "Bánh Cuốn Tây Hồ 127 | Bánh cuốn truyền thống giữa Sài Gòn",
        "Bánh cuốn truyền thống, phục vụ nhanh, hương vị gia đình Bắc giữa Sài Gòn.",
        "/",
        true,
        true,
        "Bánh Cuốn Tây Hồ 127",
        "Bánh cuốn truyền thống, phục vụ nhanh, hương vị gia đình Bắc giữa Sài Gòn.",
        null,
        null,
        null,
        null);

    /// <summary>Redirects the website already relied on (the English menu URL people type by habit).</summary>
    private static readonly RedirectDetails[] DefaultRedirects =
    [
        new("/menu", "/thuc-don", RedirectType.Permanent, true),
    ];

    /// <returns>true when anything was created.</returns>
    public static async Task<bool> SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<ISeoDbContext>();
        var seededAnything = false;

        // Each group is added only while its own row is absent, so admin edits and deletions of other rows
        // are untouched and a group added in a later release still reaches an already-seeded database.
        if (!await db.SeoSettings.AnyAsync(cancellationToken))
        {
            db.SeoSettings.Add(SeoSettings.Create(Defaults));
            seededAnything = true;
        }

        if (!await db.SeoMetadata.AnyAsync(metadata => metadata.EntityType == SeoEntityType.Homepage, cancellationToken))
        {
            db.SeoMetadata.Add(SeoMetadata.Create(SeoEntityType.Homepage, null, HomepageMetadata));
            seededAnything = true;
        }

        foreach (var details in DefaultRedirects)
        {
            var sourcePath = Redirect.NormalizeSourcePath(details.SourcePath);
            if (await db.Redirects.AnyAsync(redirect => redirect.SourcePath == sourcePath, cancellationToken))
            {
                continue;
            }

            db.Redirects.Add(Redirect.Create(details));
            seededAnything = true;
        }

        if (seededAnything)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return seededAnything;
    }
}
