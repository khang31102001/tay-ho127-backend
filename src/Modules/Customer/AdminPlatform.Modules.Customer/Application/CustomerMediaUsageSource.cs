using AdminPlatform.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Customer.Application;

/// <summary>Khách hàng dùng media ở ảnh đại diện (avatar).</summary>
public sealed class CustomerMediaUsageSource : IMediaUsageSource
{
    private readonly ICustomerDbContext _db;

    public CustomerMediaUsageSource(ICustomerDbContext db)
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

        var customers = await _db.Customers.AsNoTracking()
            .Where(c => c.AvatarMediaId != null && idTexts.Contains(c.AvatarMediaId))
            .Select(c => new { c.Id, c.FullName, c.AvatarMediaId })
            .ToListAsync(cancellationToken);

        return customers
            .Select(c => new MediaUsage(byIdText[c.AvatarMediaId!], "Khách hàng", c.Id.ToString(), c.FullName, "Ảnh đại diện"))
            .ToList();
    }
}
