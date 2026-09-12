using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Media.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using MediaEntity = AdminPlatform.Modules.Media.Domain.Media;

namespace AdminPlatform.Modules.Media.Application.Media;

public sealed class MediaService : IMediaService
{
    private readonly IMediaDbContext _db;

    public MediaService(IMediaDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<MediaResponse>> ListAsync(PagedRequest request, string? kind, string? status, CancellationToken cancellationToken)
    {
        var query = _db.Media.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(kind))
        {
            query = query.Where(m => m.Kind == ParseKind(kind));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(m => m.Status == ParseStatus(status));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(m => EF.Functions.ILike(m.FileName, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(m => m.CreatedAtUtc) : query.OrderBy(m => m.CreatedAtUtc);

        // Mapped after paging (not inside the IQueryable projection): Kind/Status are enums and the
        // ToResponse() call below relies on Enum.ToString(), which the Npgsql query translator cannot
        // turn into SQL — see FiscalYearService/SystemSettingService for the inline-Select pattern this
        // deviates from, justified by these two enum-valued columns.
        var pagedMedia = await query.ToPagedResultAsync(request, cancellationToken);
        var items = pagedMedia.Items.Select(ToResponse).ToList();
        return new PagedResult<MediaResponse>(items, pagedMedia.Page, pagedMedia.PageSize, pagedMedia.TotalItems);
    }

    public async Task<MediaResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var media = await FindOrThrowAsync(id, cancellationToken);
        return ToResponse(media);
    }

    public async Task<MediaResponse> CreateAsync(CreateMediaRequest request, CancellationToken cancellationToken)
    {
        var media = MediaEntity.Create(request.FileName, request.Url, ParseKind(request.Type), request.AltText, request.Size);
        _db.Media.Add(media);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(media);
    }

    public async Task<MediaResponse> UpdateAsync(Guid id, UpdateMediaRequest request, CancellationToken cancellationToken)
    {
        var media = await FindOrThrowAsync(id, cancellationToken);
        media.UpdateMetadata(request.FileName, request.Url, ParseKind(request.Type), request.AltText);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(media);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var media = await FindOrThrowAsync(id, cancellationToken);
        media.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<MediaEntity> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Media.SingleOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Media), id);

    private static MediaKind ParseKind(string kind) =>
        Enum.TryParse<MediaKind>(kind, ignoreCase: true, out var parsed)
            ? parsed
            : throw new BusinessRuleValidationException($"'{kind}' is not a valid media type.");

    private static MediaStatus ParseStatus(string status) =>
        Enum.TryParse<MediaStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : throw new BusinessRuleValidationException($"'{status}' is not a valid status.");

    private static MediaResponse ToResponse(MediaEntity media) => new(
        media.Id,
        media.FileName,
        media.Url,
        media.Kind.ToString(),
        media.AltText,
        media.Size,
        media.Status.ToString(),
        media.CreatedAtUtc,
        media.UpdatedAtUtc);
}
