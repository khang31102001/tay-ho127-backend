using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Sales.Application.OrderOptions;

public interface IOrderOptionService
{
    Task<PagedResult<OrderOptionGroupResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken);

    Task<OrderOptionGroupResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<OrderOptionGroupResponse> CreateAsync(CreateOrderOptionGroupRequest request, CancellationToken cancellationToken);

    /// <summary>Options are replaced as a whole list; options sent with their id keep that id.</summary>
    Task<OrderOptionGroupResponse> UpdateAsync(Guid id, UpdateOrderOptionGroupRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Every group, for the cart/checkout (anonymous).</summary>
    Task<IReadOnlyList<OrderOptionGroupResponse>> ListPublicAsync(CancellationToken cancellationToken);
}
