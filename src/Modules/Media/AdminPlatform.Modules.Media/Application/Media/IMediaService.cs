using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Media.Application.Media;

public interface IMediaService
{
    Task<PagedResult<MediaResponse>> ListAsync(PagedRequest request, string? kind, string? status, CancellationToken cancellationToken);

    Task<MediaResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MediaResponse> CreateAsync(CreateMediaRequest request, CancellationToken cancellationToken);

    /// <summary>Lưu file vào IFileStorage rồi tạo media. <paramref name="publicBaseUrl"/> là origin công khai
    /// dùng để dựng URL tuyệt đối lưu vào DB.</summary>
    Task<MediaResponse> UploadAsync(MediaUploadFile file, string? altText, string publicBaseUrl, CancellationToken cancellationToken);

    Task<MediaResponse> UpdateAsync(Guid id, UpdateMediaRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<PublicMediaResponse>> ListPublicAsync(PagedRequest request, CancellationToken cancellationToken);
}
