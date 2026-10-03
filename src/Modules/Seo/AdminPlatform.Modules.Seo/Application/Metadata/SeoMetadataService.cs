using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Seo.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Seo.Application.Metadata;

public sealed class SeoMetadataService : ISeoMetadataService
{
    private readonly ISeoDbContext _db;

    public SeoMetadataService(ISeoDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<SeoMetadataResponse>> ListAsync(
        PagedRequest request, string? entityType, CancellationToken cancellationToken)
    {
        var query = _db.SeoMetadata.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            if (!SeoEntityTypeWire.TryParse(entityType, out var parsedType))
            {
                throw new BusinessRuleValidationException($"EntityType must be {SeoEntityTypeWire.AllowedValues}.");
            }

            query = query.Where(m => m.EntityType == parsedType);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(m => (m.EntityId != null && EF.Functions.ILike(m.EntityId, pattern))
                || (m.MetaTitle != null && EF.Functions.ILike(m.MetaTitle, pattern)));
        }

        query = request.IsDescending
            ? query.OrderByDescending(m => m.EntityType).ThenByDescending(m => m.EntityId)
            : query.OrderBy(m => m.EntityType).ThenBy(m => m.EntityId);

        var page = await query.ToPagedResultAsync(request, cancellationToken);
        return new PagedResult<SeoMetadataResponse>(page.Items.Select(ToResponse).ToList(), page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<SeoMetadataResponse?> FindAsync(string entityType, string? entityId, CancellationToken cancellationToken)
    {
        var type = ParseTypeOrThrow(entityType);
        var id = SeoMetadata.NormalizeEntityId(type, entityId);

        var metadata = await _db.SeoMetadata.AsNoTracking()
            .SingleOrDefaultAsync(m => m.EntityType == type && m.EntityId == id, cancellationToken);
        return metadata is null ? null : ToResponse(metadata);
    }

    public async Task<SeoMetadataResponse> UpsertAsync(UpsertSeoMetadataRequest request, CancellationToken cancellationToken)
    {
        var type = ParseTypeOrThrow(request.EntityType);
        var id = SeoMetadata.NormalizeEntityId(type, request.EntityId);
        var details = new SeoMetadataDetails(
            request.MetaTitle, request.MetaDescription, request.CanonicalUrl, request.RobotsIndex, request.RobotsFollow, request.OgTitle,
            request.OgDescription, request.OgImageMediaId, request.TwitterTitle, request.TwitterDescription, request.TwitterImageMediaId);

        var metadata = await _db.SeoMetadata.SingleOrDefaultAsync(m => m.EntityType == type && m.EntityId == id, cancellationToken);
        if (metadata is null)
        {
            metadata = SeoMetadata.Create(type, id, details);
            _db.SeoMetadata.Add(metadata);
        }
        else
        {
            metadata.Update(details);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(metadata);
    }

    public async Task ResetAsync(string entityType, string? entityId, CancellationToken cancellationToken)
    {
        var type = ParseTypeOrThrow(entityType);
        var id = SeoMetadata.NormalizeEntityId(type, entityId);

        var metadata = await _db.SeoMetadata.SingleOrDefaultAsync(m => m.EntityType == type && m.EntityId == id, cancellationToken);
        if (metadata is null)
        {
            return;
        }

        _db.SeoMetadata.Remove(metadata);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NoIndexEntityResponse>> ListNoIndexAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.SeoMetadata.AsNoTracking()
            .Where(m => !m.RobotsIndex)
            .OrderBy(m => m.EntityType).ThenBy(m => m.EntityId)
            .Select(m => new { m.EntityType, m.EntityId })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new NoIndexEntityResponse(SeoEntityTypeWire.ToWire(row.EntityType), row.EntityId)).ToList();
    }

    private static SeoEntityType ParseTypeOrThrow(string entityType) =>
        SeoEntityTypeWire.TryParse(entityType, out var parsed)
            ? parsed
            : throw new BusinessRuleValidationException($"EntityType must be {SeoEntityTypeWire.AllowedValues}.");

    internal static SeoMetadataResponse ToResponse(SeoMetadata m) => new(
        m.Id, SeoEntityTypeWire.ToWire(m.EntityType), m.EntityId, m.MetaTitle, m.MetaDescription, m.CanonicalUrl, m.RobotsIndex,
        m.RobotsFollow, m.OgTitle, m.OgDescription, m.OgImageMediaId, m.TwitterTitle, m.TwitterDescription, m.TwitterImageMediaId,
        m.CreatedAtUtc, m.UpdatedAtUtc ?? m.CreatedAtUtc);
}
