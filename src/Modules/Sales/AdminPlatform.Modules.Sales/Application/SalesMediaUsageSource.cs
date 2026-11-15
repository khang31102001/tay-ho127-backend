using AdminPlatform.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Sales.Application;

/// <summary>Phương thức thanh toán dùng media ở icon. Ảnh trong đơn hàng cũ (bản chụp lúc đặt) KHÔNG chặn xóa —
/// đơn cũ chỉ hiện ảnh mặc định — nên không tra ở đây.</summary>
public sealed class SalesMediaUsageSource : IMediaUsageSource
{
    private readonly ISalesDbContext _db;

    public SalesMediaUsageSource(ISalesDbContext db)
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

        var methods = await _db.PaymentMethods.AsNoTracking()
            .Where(m => m.IconMediaId != null && idTexts.Contains(m.IconMediaId))
            .Select(m => new { m.Id, m.Name, m.IconMediaId })
            .ToListAsync(cancellationToken);

        return methods
            .Select(m => new MediaUsage(byIdText[m.IconMediaId!], "Phương thức thanh toán", m.Id.ToString(), m.Name, "Icon"))
            .ToList();
    }
}
