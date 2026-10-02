using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Catalog.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Catalog.Application.SalesMenus;

public sealed class SalesMenuService : ISalesMenuService
{
    private readonly ICatalogDbContext _db;

    public SalesMenuService(ICatalogDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<SalesMenuResponse>> ListAsync(PagedRequest request, bool? isActive, CancellationToken cancellationToken)
    {
        var query = _db.SalesMenus.AsNoTracking().AsQueryable();

        if (isActive is { } active)
        {
            query = query.Where(m => m.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(m => EF.Functions.ILike(m.Code, pattern) || EF.Functions.ILike(m.Name, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(m => m.CreatedAtUtc) : query.OrderBy(m => m.CreatedAtUtc);

        var projected = query.Select(m => new SalesMenuResponse(m.Id, m.Code, m.Name, m.IsActive, m.CreatedAtUtc, m.UpdatedAtUtc));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<SalesMenuResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await FindOrThrowAsync(id, cancellationToken));
    }

    public async Task<SalesMenuResponse> CreateAsync(CreateSalesMenuRequest request, CancellationToken cancellationToken)
    {
        if (await _db.SalesMenus.AnyAsync(m => m.Code == request.Code, cancellationToken))
        {
            throw new ConflictException($"A menu with code '{request.Code}' already exists.");
        }

        var menu = SalesMenu.Create(request.Code, request.Name, request.IsActive);
        _db.SalesMenus.Add(menu);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(menu);
    }

    public async Task<SalesMenuResponse> UpdateAsync(Guid id, UpdateSalesMenuRequest request, CancellationToken cancellationToken)
    {
        var menu = await FindOrThrowAsync(id, cancellationToken);
        menu.Update(request.Name, request.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(menu);
    }

    /// <summary>Hard delete; the menu's product placements are removed with it (cascade).</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var menu = await FindOrThrowAsync(id, cancellationToken);
        _db.SalesMenus.Remove(menu);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<SalesMenu> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.SalesMenus.SingleOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(SalesMenu), id);

    private static SalesMenuResponse ToResponse(SalesMenu menu) =>
        new(menu.Id, menu.Code, menu.Name, menu.IsActive, menu.CreatedAtUtc, menu.UpdatedAtUtc);
}
