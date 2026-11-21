using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Media.Application.Media;

public interface IMediaService
{
    Task<PagedResult<MediaResponse>> ListAsync(PagedRequest request, string? kind, string? status, string? storage, string? category, CancellationToken cancellationToken);

    Task<MediaResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MediaResponse> CreateAsync(CreateMediaRequest request, CancellationToken cancellationToken);

    /// <summary>Lưu file vào nơi lưu <paramref name="storage"/> (trống = mặc định) rồi tạo media thuộc nhóm
    /// <paramref name="category"/> (code nhóm; trống = nhóm Chung).
    /// <paramref name="publicBaseUrl"/> là origin công khai dùng để dựng URL tuyệt đối lưu vào DB.</summary>
    Task<MediaResponse> UploadAsync(MediaUploadFile file, string? altText, string? storage, string? category, string publicBaseUrl, CancellationToken cancellationToken);

    /// <summary>Các nơi lưu cho upload (Local, Cloudinary) kèm trạng thái cấu hình và nơi mặc định.</summary>
    IReadOnlyList<MediaStorageOptionResponse> GetStorageOptions();

    Task<MediaResponse> UpdateAsync(Guid id, UpdateMediaRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Các nhóm nghiệp vụ của kho ảnh, theo thứ tự hiển thị.</summary>
    Task<IReadOnlyList<MediaCategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken);

    Task<PagedResult<PublicMediaResponse>> ListPublicAsync(PagedRequest request, CancellationToken cancellationToken);
}
