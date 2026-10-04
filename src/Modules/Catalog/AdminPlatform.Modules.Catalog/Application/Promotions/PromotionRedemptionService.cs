using AdminPlatform.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Catalog.Application.Promotions;

/// <summary>Counts the use of a discount code. Each call is ONE conditional UPDATE in the database, so two orders
/// racing for the last use cannot both succeed (a read-check-write could).</summary>
public interface IPromotionRedemptionService
{
    /// <summary>Takes one use; false when the code is not active or its usage limit is already reached.</summary>
    Task<bool> TryRedeemAsync(Guid promotionId, CancellationToken cancellationToken);

    /// <summary>Gives one use back (an order that failed or was cancelled). Never goes below zero.</summary>
    Task ReleaseAsync(Guid promotionId, CancellationToken cancellationToken);
}

public sealed class PromotionRedemptionService : IPromotionRedemptionService
{
    private readonly ICatalogDbContext _db;

    public PromotionRedemptionService(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TryRedeemAsync(Guid promotionId, CancellationToken cancellationToken)
    {
        var affected = await _db.Promotions
            .Where(p => p.Id == promotionId
                && p.Status == PromotionStatus.Active
                && (p.UsageLimit == null || p.UsageCount < p.UsageLimit))
            .ExecuteUpdateAsync(set => set.SetProperty(p => p.UsageCount, p => p.UsageCount + 1), cancellationToken);
        return affected == 1;
    }

    public async Task ReleaseAsync(Guid promotionId, CancellationToken cancellationToken)
    {
        await _db.Promotions
            .Where(p => p.Id == promotionId && p.UsageCount > 0)
            .ExecuteUpdateAsync(set => set.SetProperty(p => p.UsageCount, p => p.UsageCount - 1), cancellationToken);
    }
}
