using AdminPlatform.Modules.Media.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Media.Application.Media;

/// <summary>Hợp đồng đọc công khai của module Media cho module khác (qua adapter ở Host): tra nhanh nhiều
/// media theo id để kiểm tra tham chiếu và lấy URL hiển thị.</summary>
public interface IMediaReferenceQueryService
{
    Task<IReadOnlyList<MediaReference>> FindAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}

public sealed record MediaReference(Guid Id, string Url, string Type, bool IsActive);

public sealed class MediaReferenceQueryService : IMediaReferenceQueryService
{
    private readonly IMediaDbContext _db;

    public MediaReferenceQueryService(IMediaDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MediaReference>> FindAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var distinctIds = ids.Distinct().ToList();
        var rows = await _db.Media.AsNoTracking()
            .Where(m => distinctIds.Contains(m.Id))
            .Select(m => new { m.Id, m.Url, m.Kind, m.Status })
            .ToListAsync(cancellationToken);

        return rows
            .Select(m => new MediaReference(m.Id, m.Url, m.Kind.ToString().ToLowerInvariant(), m.Status == MediaStatus.Active))
            .ToList();
    }
}
