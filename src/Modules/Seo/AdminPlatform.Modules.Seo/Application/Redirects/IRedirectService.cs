using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Seo.Application.Redirects;

public interface IRedirectService
{
    /// <param name="isActive">Filters by the active flag; Search matches the source path or the destination.</param>
    Task<PagedResult<RedirectResponse>> ListAsync(PagedRequest request, bool? isActive, CancellationToken cancellationToken);

    Task<RedirectResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>409 when another redirect already uses the source path; 400 for a bad path/destination or a redirect loop.</summary>
    Task<RedirectResponse> CreateAsync(CreateRedirectRequest request, CancellationToken cancellationToken);

    Task<RedirectResponse> UpdateAsync(Guid id, UpdateRedirectRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Every active redirect — small, read by the website's middleware.</summary>
    Task<IReadOnlyList<PublicRedirectResponse>> ListActiveAsync(CancellationToken cancellationToken);
}
