namespace AdminPlatform.Modules.Content.Application.PageSections;

/// <summary>Sections are always addressed through their page; asking for a page that does not exist is a 404.</summary>
public interface IPageSectionService
{
    /// <summary>Every section of the page, by display order (a page has a handful, so no paging).</summary>
    Task<IReadOnlyList<PageSectionResponse>> ListAsync(Guid pageId, CancellationToken cancellationToken);

    Task<PageSectionResponse> GetByIdAsync(Guid pageId, Guid id, CancellationToken cancellationToken);

    Task<PageSectionResponse> CreateAsync(Guid pageId, CreatePageSectionRequest request, CancellationToken cancellationToken);

    Task<PageSectionResponse> UpdateAsync(Guid pageId, Guid id, UpdatePageSectionRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid pageId, Guid id, CancellationToken cancellationToken);
}
