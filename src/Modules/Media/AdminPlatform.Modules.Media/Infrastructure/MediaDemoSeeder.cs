using AdminPlatform.Modules.Media.Application;
using AdminPlatform.Modules.Media.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Media.Infrastructure;

/// <summary>Idempotent DEMO data (Migrator `seed-demo` only): a handful of media-library entries of each
/// kind. URLs point at the reserved `.test` domain — placeholders only, nothing is actually hosted there.
/// Upserted by Url.</summary>
public static class MediaDemoSeeder
{
    private static readonly (string FileName, string Url, MediaKind Kind, string? AltText, int Size)[] DemoMedia =
    [
        ("storefront.jpg", "https://cdn.tayho127.test/demo/storefront.jpg", MediaKind.Image, "Tay Ho 127 storefront", 245_760),
        ("logo.png", "https://cdn.tayho127.test/demo/logo.png", MediaKind.Image, "Tay Ho 127 logo", 18_432),
        ("intro.mp4", "https://cdn.tayho127.test/demo/intro.mp4", MediaKind.Video, null, 10_485_760),
        ("price-list.pdf", "https://cdn.tayho127.test/demo/price-list.pdf", MediaKind.Document, null, 524_288),
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IMediaDbContext>();

        var urls = DemoMedia.Select(m => m.Url).ToList();
        var existingUrls = await db.Media
            .Where(m => urls.Contains(m.Url))
            .Select(m => m.Url)
            .ToListAsync(cancellationToken);

        foreach (var (fileName, url, kind, altText, size) in DemoMedia)
        {
            if (!existingUrls.Contains(url))
            {
                db.Media.Add(Domain.Media.Create(fileName, url, kind, altText, size));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
