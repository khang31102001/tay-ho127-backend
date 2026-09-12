using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Media.Application.Media;

public interface IMediaService
{
    Task<PagedResult<MediaResponse>> ListAsync(PagedRequest request, string? kind, string? status, CancellationToken cancellationToken);

    Task<MediaResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MediaResponse> CreateAsync(CreateMediaRequest request, CancellationToken cancellationToken);

    Task<MediaResponse> UpdateAsync(Guid id, UpdateMediaRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
