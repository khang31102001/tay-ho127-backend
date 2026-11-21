using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Media.Application;
using AdminPlatform.Modules.Media.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Media.Infrastructure;

/// <summary>Gán nhóm kho ảnh cho media có sẵn (category_id = null) theo nơi đang dùng: nơi dùng đầu tiên mà
/// IMediaUsageSource tìm được quyết định nhóm; chưa dùng ở đâu (hoặc module không có nhóm riêng) thì vào "Chung".
/// Idempotent và chỉ đụng media chưa có nhóm — media đã có nhóm (kể cả do Admin đổi tay) không bị ghi đè.
/// Chạy ở Migrator `seed`; nằm ở Application/Infrastructure của Media nên không cần biết module nào tồn tại.</summary>
public static class MediaCategoryBackfill
{
    private const int BatchSize = 200;

    /// <returns>Số media vừa được gán nhóm.</returns>
    public static async Task<int> RunAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IMediaDbContext>();
        var usageSources = services.GetServices<IMediaUsageSource>().ToList();

        var categoryIdByCode = await db.MediaCategories.ToDictionaryAsync(c => c.Code, c => c.Id, cancellationToken);
        if (!categoryIdByCode.TryGetValue(MediaCategoryCodes.General, out var generalId))
        {
            return 0;
        }

        var assigned = 0;
        while (true)
        {
            var batch = await db.Media
                .Where(m => m.CategoryId == null)
                .OrderBy(m => m.CreatedAtUtc)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                return assigned;
            }

            var targets = batch.Select(m => new MediaUsageTarget(m.Id, m.Url)).ToList();
            var usagesByMedia = new Dictionary<Guid, List<MediaUsage>>();
            foreach (var source in usageSources)
            {
                foreach (var usage in await source.FindUsagesAsync(targets, cancellationToken))
                {
                    if (!usagesByMedia.TryGetValue(usage.MediaId, out var list))
                    {
                        usagesByMedia[usage.MediaId] = list = [];
                    }

                    list.Add(usage);
                }
            }

            foreach (var media in batch)
            {
                var code = usagesByMedia.GetValueOrDefault(media.Id)?
                    .Select(u => MediaCategoryCodes.FromUsageModule(u.Module))
                    .FirstOrDefault(c => c is not null);

                media.AssignCategory(code is not null && categoryIdByCode.TryGetValue(code, out var id) ? id : generalId);
            }

            await db.SaveChangesAsync(cancellationToken);
            assigned += batch.Count;
        }
    }
}
