using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Navigation.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Navigation.Application.Menus;

public sealed class MenuService : IMenuService
{
    private readonly INavigationDbContext _db;

    public MenuService(INavigationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<MenuResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken)
    {
        var query = _db.Menus.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(m => EF.Functions.ILike(m.Code, pattern) || EF.Functions.ILike(m.Name, pattern));
        }

        query = query.OrderBy(m => m.SortOrder).ThenBy(m => m.Name);

        var projected = query.Select(m => new MenuResponse(m.Id, m.Code, m.Name, m.IsActive, m.ParentId, m.Route, m.Icon, m.SortOrder));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<MenuResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var menu = await FindOrThrowAsync(id, cancellationToken);
        return ToResponse(menu);
    }

    public async Task<IReadOnlyList<MenuTreeNode>> GetTreeAsync(CancellationToken cancellationToken)
    {
        var menus = await _db.Menus.AsNoTracking().OrderBy(m => m.SortOrder).ThenBy(m => m.Name).ToListAsync(cancellationToken);
        return BuildTree(menus, null);
    }

    public async Task<MenuResponse> CreateAsync(CreateMenuRequest request, CancellationToken cancellationToken)
    {
        var codeExists = await _db.Menus.AnyAsync(m => m.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException($"A menu with code '{request.Code}' already exists.");
        }

        var menu = Menu.Create(request.Code, request.Name, request.ParentId, request.Route, request.Icon, request.SortOrder);
        _db.Menus.Add(menu);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(menu);
    }

    public async Task<MenuResponse> UpdateAsync(Guid id, UpdateMenuRequest request, CancellationToken cancellationToken)
    {
        var menu = await FindOrThrowAsync(id, cancellationToken);

        if (request.ParentId == id)
        {
            throw new BusinessRuleValidationException("A menu cannot be its own parent.");
        }

        menu.Update(request.Name, request.IsActive, request.ParentId, request.Route, request.Icon, request.SortOrder);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(menu);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var menu = await FindOrThrowAsync(id, cancellationToken);

        var hasChildren = await _db.Menus.AnyAsync(m => m.ParentId == id, cancellationToken);
        if (hasChildren)
        {
            throw new ConflictException("This menu still has child menus and cannot be deleted.");
        }

        var links = await _db.MenuPermissions.Where(mp => mp.MenuId == id).ToListAsync(cancellationToken);
        _db.MenuPermissions.RemoveRange(links);
        _db.Menus.Remove(menu);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid menuId, CancellationToken cancellationToken)
    {
        await FindOrThrowAsync(menuId, cancellationToken);
        return await _db.MenuPermissions.Where(mp => mp.MenuId == menuId).Select(mp => mp.PermissionCode).ToListAsync(cancellationToken);
    }

    public async Task SetPermissionsAsync(Guid menuId, AssignMenuPermissionsRequest request, CancellationToken cancellationToken)
    {
        await FindOrThrowAsync(menuId, cancellationToken);

        var requested = request.PermissionCodes.Distinct().ToHashSet();
        var current = await _db.MenuPermissions.Where(mp => mp.MenuId == menuId).ToListAsync(cancellationToken);
        var currentCodes = current.Select(mp => mp.PermissionCode).ToHashSet();

        _db.MenuPermissions.RemoveRange(current.Where(mp => !requested.Contains(mp.PermissionCode)));

        foreach (var code in requested.Except(currentCodes))
        {
            _db.MenuPermissions.Add(MenuPermission.Create(menuId, code));
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static List<MenuTreeNode> BuildTree(List<Menu> all, Guid? parentId)
    {
        return all
            .Where(m => m.ParentId == parentId)
            .Select(m => new MenuTreeNode(m.Id, m.Code, m.Name, m.Route, m.Icon, m.SortOrder, BuildTree(all, m.Id)))
            .ToList();
    }

    private async Task<Menu> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Menus.SingleOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Menu), id);

    private static MenuResponse ToResponse(Menu menu) =>
        new(menu.Id, menu.Code, menu.Name, menu.IsActive, menu.ParentId, menu.Route, menu.Icon, menu.SortOrder);
}
