using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Content.Application.Pages;

public interface IPageService
{
    /// <param name="status">Filters by status ("draft" | "published" | "archived").</param>
    Task<PagedResult<PageResponse>> ListAsync(PagedRequest request, string? status, CancellationToken cancellationToken);

    Task<PageResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PageResponse> CreateAsync(CreatePageRequest request, CancellationToken cancellationToken);

    Task<PageResponse> UpdateAsync(Guid id, UpdatePageRequest request, CancellationToken cancellationToken);

    /// <summary>Also deletes every section of the page.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
