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

    /// <returns>true when the settings were created.</returns>
    public static async Task<bool> SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<ISeoDbContext>();
        if (await db.SeoSettings.AnyAsync(cancellationToken))
        {
            return false;
        }

        db.SeoSettings.Add(SeoSettings.Create(Defaults));
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
