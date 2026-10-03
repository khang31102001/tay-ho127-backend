using AdminPlatform.Modules.Catalog.Application;
using AdminPlatform.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Catalog.Infrastructure;

/// <summary>Demo discount codes (the four examples the frontend's mock used) for test/dev databases —
/// run by the Migrator's seed-demo command, never by the every-deploy `seed`. Idempotent: a code that
/// already exists (even if an admin edited it) is left alone. Run it after `seed`, since BANHCUON10 is
/// scoped to the seeded "Bánh cuốn" category, looked up by name (ids are generated GUIDs).</summary>
public static class PromotionDemoSeeder
{
    private const string BanhCuonCategoryName = "Bánh cuốn";

    /// <returns>The number of promotions created.</returns>
    public static async Task<int> SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<ICatalogDbContext>();

        var banhCuonCategoryId = await db.Categories
            .Where(c => c.Name == BanhCuonCategoryName)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var created = 0;
        foreach (var (details, categoryIds) in BuildDemoPromotions(banhCuonCategoryId))
        {
            var code = Promotion.NormalizeCode(details.Code);
            if (await db.Promotions.AnyAsync(p => p.Code == code, cancellationToken))
            {
                continue;
            }

            var promotion = Promotion.Create(details);
            promotion.ReplaceCategories(categoryIds);
            db.Promotions.Add(promotion);
            created++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return created;
    }

    private static IEnumerable<(PromotionDetails Details, Guid[] CategoryIds)> BuildDemoPromotions(Guid? banhCuonCategoryId)
    {
        yield return (new PromotionDetails("WELCOME20", "Giảm 20% đơn hàng", "Giảm 20%, tối đa 50.000đ cho đơn từ 150.000đ.",
            PromotionType.Percentage, 20, 50000, 150000, null, null, 200, PromotionStatus.Active), []);

        yield return (new PromotionDetails("GIAM30K", "Giảm 30.000đ", "Giảm ngay 30.000đ cho đơn từ 100.000đ.",
            PromotionType.FixedAmount, 30000, null, 100000, null, null, null, PromotionStatus.Active), []);

        yield return (new PromotionDetails("FREESHIP", "Miễn phí vận chuyển", "Miễn phí toàn bộ phí giao hàng cho đơn từ 200.000đ.",
            PromotionType.FreeShipping, 100, null, 200000, null, null, null, PromotionStatus.Active), []);

        // A product discount without a scope is invalid, so it is only created when the category exists.
        if (banhCuonCategoryId is { } categoryId)
        {
            yield return (new PromotionDetails("BANHCUON10", "Giảm 10% Bánh cuốn", "Giảm 10% các sản phẩm thuộc danh mục Bánh cuốn.",
                PromotionType.ProductDiscount, 10, 20000, null, null, null, null, PromotionStatus.Active), [categoryId]);
        }
    }
}
