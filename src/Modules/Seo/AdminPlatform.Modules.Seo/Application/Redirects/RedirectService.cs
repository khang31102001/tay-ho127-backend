using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Seo.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Seo.Application.Redirects;

public sealed class RedirectService : IRedirectService
{
    /// <summary>A chain longer than this is treated as a loop (real chains are a couple of hops).</summary>
    private const int MaxChainLength = 20;

    private readonly ISeoDbContext _db;

    public RedirectService(ISeoDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<RedirectResponse>> ListAsync(PagedRequest request, bool? isActive, CancellationToken cancellationToken)
    {
        var query = _db.Redirects.AsNoTracking().AsQueryable();

        if (isActive is { } active)
        {
            query = query.Where(r => r.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(r => EF.Functions.ILike(r.SourcePath, pattern) || EF.Functions.ILike(r.DestinationUrl, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(r => r.SourcePath) : query.OrderBy(r => r.SourcePath);

        var page = await query.ToPagedResultAsync(request, cancellationToken);
        return new PagedResult<RedirectResponse>(page.Items.Select(ToResponse).ToList(), page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<RedirectResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await FindOrThrowAsync(id, cancellationToken));
    }

    public async Task<RedirectResponse> CreateAsync(CreateRedirectRequest request, CancellationToken cancellationToken)
    {
        var redirect = Redirect.Create(ToDetails(request.SourcePath, request.DestinationUrl, request.RedirectType, request.IsActive));
        await EnsureValidAsync(redirect, cancellationToken);

        _db.Redirects.Add(redirect);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(redirect);
    }

    public async Task<RedirectResponse> UpdateAsync(Guid id, UpdateRedirectRequest request, CancellationToken cancellationToken)
    {
        var redirect = await FindOrThrowAsync(id, cancellationToken);
        redirect.Update(ToDetails(request.SourcePath, request.DestinationUrl, request.RedirectType, request.IsActive));
        await EnsureValidAsync(redirect, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(redirect);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var redirect = await FindOrThrowAsync(id, cancellationToken);
        _db.Redirects.Remove(redirect);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PublicRedirectResponse>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.Redirects.AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.SourcePath)
            .ToListAsync(cancellationToken);
        return rows.Select(r => new PublicRedirectResponse(r.SourcePath, r.DestinationUrl, (int)r.RedirectType)).ToList();
    }

    /// <summary>The source path must be unique, and an active redirect must not lead (directly or through other
    /// active redirects) back to its own source: that would loop forever for the visitor.</summary>
    private async Task EnsureValidAsync(Redirect redirect, CancellationToken cancellationToken)
    {
        if (await _db.Redirects.AnyAsync(r => r.SourcePath == redirect.SourcePath && r.Id != redirect.Id, cancellationToken))
        {
            throw new ConflictException($"Another redirect already uses the source path '{redirect.SourcePath}'.");
        }

        if (!redirect.IsActive || !Redirect.TryGetDestinationPath(redirect.DestinationUrl, out var current))
        {
            return;
        }

        var activeDestinations = await _db.Redirects.AsNoTracking()
            .Where(r => r.IsActive && r.Id != redirect.Id)
            .Select(r => new { r.SourcePath, r.DestinationUrl })
            .ToDictionaryAsync(r => r.SourcePath, r => r.DestinationUrl, cancellationToken);

        for (var hops = 0; hops < MaxChainLength; hops++)
        {
            if (current == redirect.SourcePath)
            {
                throw new BusinessRuleValidationException("This redirect would loop back to its own source path.");
            }

            if (!activeDestinations.TryGetValue(current, out var next) || !Redirect.TryGetDestinationPath(next, out current))
            {
                return;
            }
        }

        throw new BusinessRuleValidationException("This redirect leads into a chain of redirects that never ends.");
    }

    private async Task<Redirect> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Redirects.SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Redirect), id);

    private static RedirectDetails ToDetails(string sourcePath, string destinationUrl, int redirectType, bool isActive) =>
        // The validators already checked the type; an undefined value would be rejected by the domain anyway.
        new(sourcePath, destinationUrl, (RedirectType)redirectType, isActive);

    internal static RedirectResponse ToResponse(Redirect r) => new(
        r.Id, r.SourcePath, r.DestinationUrl, (int)r.RedirectType, r.IsActive, r.CreatedAtUtc, r.UpdatedAtUtc ?? r.CreatedAtUtc);
}
