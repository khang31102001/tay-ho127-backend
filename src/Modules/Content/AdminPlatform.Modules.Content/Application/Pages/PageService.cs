using AdminPlatform.Common.Abstractions;
using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Content.Application.Pages;

public sealed class PageService : IPageService
{
    private readonly IContentDbContext _db;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PageService(IContentDbContext db, IDateTimeProvider dateTimeProvider)
    {
        _db = db;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedResult<PageResponse>> ListAsync(PagedRequest request, string? status, CancellationToken cancellationToken)
    {
        var query = _db.Pages.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!ContentWireFormat.TryParseStatus(status, out var parsedStatus))
            {
                throw new BusinessRuleValidationException("Status must be draft, published or archived.");
            }

            query = query.Where(p => p.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern) || EF.Functions.ILike(p.Slug, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(p => p.Slug) : query.OrderBy(p => p.Slug);

        var page = await query.ToPagedResultAsync(request, cancellationToken);
        return new PagedResult<PageResponse>(page.Items.Select(ToResponse).ToList(), page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<PageResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await FindOrThrowAsync(id, cancellationToken));
    }

    public async Task<PageResponse> CreateAsync(CreatePageRequest request, CancellationToken cancellationToken)
    {
        var slug = await ResolveSlugAsync(request.Slug, request.Name, pageId: null, cancellationToken);

        var page = Page.Create(request.Name, slug, ParseStatus(request.Status), _dateTimeProvider.UtcNow);
        _db.Pages.Add(page);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(page);
    }

    public async Task<PageResponse> UpdateAsync(Guid id, UpdatePageRequest request, CancellationToken cancellationToken)
    {
        var page = await FindOrThrowAsync(id, cancellationToken);

        // A blank slug keeps the current one: renaming a page must not silently change its URL.
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? page.Slug
            : await ResolveSlugAsync(request.Slug, request.Name, id, cancellationToken);

        page.Update(request.Name, slug, ParseStatus(request.Status), _dateTimeProvider.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(page);
    }

    /// <summary>Hard delete; the page's sections are removed with it (cascade).</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var page = await FindOrThrowAsync(id, cancellationToken);
        _db.Pages.Remove(page);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>An explicit path must be free (409 otherwise); a generated one ("/ve-chung-toi") gets a numeric
    /// suffix until it is unique ("/ve-chung-toi-2", ...).</summary>
    private async Task<string> ResolveSlugAsync(string? requestedSlug, string name, Guid? pageId, CancellationToken cancellationToken)
    {
        var others = _db.Pages.Where(p => p.Id != pageId).Select(p => p.Slug);

        if (!string.IsNullOrWhiteSpace(requestedSlug))
        {
            var path = requestedSlug.Trim();
            if (await others.AnyAsync(existing => existing == path, cancellationToken))
            {
                throw new ConflictException($"Another page already uses the path '{path}'.");
            }

            return path;
        }

        var basePath = Page.PathFromName(name)
            ?? throw new BusinessRuleValidationException("A path cannot be generated from this name; provide one explicitly.");

        var taken = (await others.Where(existing => existing == basePath || existing.StartsWith(basePath + "-")).ToListAsync(cancellationToken))
            .ToHashSet();

        var candidate = basePath;
        for (var suffix = 2; taken.Contains(candidate); suffix++)
        {
            candidate = $"{basePath}-{suffix}";
        }

        return candidate;
    }

    private async Task<Page> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Pages.SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Page), id);

    private static PublishStatus ParseStatus(string status)
    {
        // The validators already checked the status; a failed parse here would be a programming error.
        ContentWireFormat.TryParseStatus(status, out var parsed);
        return parsed;
    }

    private static PageResponse ToResponse(Page page) =>
        new(page.Id, page.Name, page.Slug, ContentWireFormat.ToWire(page.Status), page.PublishedAtUtc, page.CreatedAtUtc, page.UpdatedAtUtc);
}
