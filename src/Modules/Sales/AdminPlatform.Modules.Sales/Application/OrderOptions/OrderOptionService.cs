using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Sales.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Sales.Application.OrderOptions;

public sealed class OrderOptionService : IOrderOptionService
{
    private readonly ISalesDbContext _db;

    public OrderOptionService(ISalesDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<OrderOptionGroupResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken)
    {
        var query = _db.OrderOptionGroups.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(g => EF.Functions.ILike(g.Name, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(g => g.Name) : query.OrderBy(g => g.Name);

        var page = await query.Include(g => g.Values).AsSplitQuery().ToPagedResultAsync(request, cancellationToken);
        return new PagedResult<OrderOptionGroupResponse>(page.Items.Select(ToResponse).ToList(), page.Page, page.PageSize, page.TotalItems);
    }

    public async Task<OrderOptionGroupResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await FindOrThrowAsync(id, cancellationToken));

    public async Task<OrderOptionGroupResponse> CreateAsync(CreateOrderOptionGroupRequest request, CancellationToken cancellationToken)
    {
        var group = OrderOptionGroup.Create(request.Name, ParseSelectionType(request.SelectionType), request.IsRequired, ToInputs(request.Options));
        _db.OrderOptionGroups.Add(group);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(group);
    }

    public async Task<OrderOptionGroupResponse> UpdateAsync(Guid id, UpdateOrderOptionGroupRequest request, CancellationToken cancellationToken)
    {
        var group = await FindOrThrowAsync(id, cancellationToken);
        group.Update(request.Name, ParseSelectionType(request.SelectionType), request.IsRequired, ToInputs(request.Options));
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(group);
    }

    /// <summary>Hard delete; its values go with it. Orders keep a text snapshot of what was picked.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var group = await FindOrThrowAsync(id, cancellationToken);
        _db.OrderOptionGroups.Remove(group);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrderOptionGroupResponse>> ListPublicAsync(CancellationToken cancellationToken)
    {
        var groups = await _db.OrderOptionGroups.AsNoTracking()
            .Include(g => g.Values)
            .AsSplitQuery()
            .OrderBy(g => g.CreatedAtUtc).ThenBy(g => g.Name)
            .ToListAsync(cancellationToken);
        return groups.Select(ToResponse).ToList();
    }

    private static IReadOnlyList<OrderOptionValueInput> ToInputs(IReadOnlyList<OrderOptionValueRequest> options) =>
        options.Select(o => new OrderOptionValueInput(o.Id, o.Label, o.PriceAdjustment, o.IsDefault)).ToList();

    private static OptionSelectionType ParseSelectionType(string selectionType) =>
        EnumWire.Parse<OptionSelectionType>(selectionType, "selection type");

    private async Task<OrderOptionGroup> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.OrderOptionGroups.Include(g => g.Values).SingleOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(OrderOptionGroup), id);

    internal static OrderOptionGroupResponse ToResponse(OrderOptionGroup group) => new(
        group.Id,
        group.Name,
        EnumWire.ToWire(group.SelectionType),
        group.IsRequired,
        group.Values.OrderBy(v => v.SortOrder).Select(v => new OrderOptionValueResponse(v.Id, v.Label, v.PriceAdjustment, v.IsDefault)).ToList(),
        group.CreatedAtUtc,
        group.UpdatedAtUtc ?? group.CreatedAtUtc);
}
