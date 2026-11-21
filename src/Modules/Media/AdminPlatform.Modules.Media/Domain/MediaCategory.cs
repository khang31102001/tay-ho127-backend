using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Media.Domain;

/// <summary>Nhóm nghiệp vụ của kho ảnh: cho biết media được THÊM từ đâu (Sản phẩm, Bài viết, Banner, ...) để
/// Admin lọc/tìm nhanh trong thư viện. Mỗi media thuộc đúng một nhóm (<see cref="Media.CategoryId"/>); việc
/// media ĐANG được dùng ở đâu vẫn do IMediaUsageSource trả lời. Danh sách nhóm là dữ liệu hệ thống do migration
/// tạo sẵn (code cố định, FE gửi code lên khi upload) — không có màn thêm/sửa/xóa.</summary>
public sealed class MediaCategory : AuditableEntity
{
    public const int MaxCodeLength = 32;
    public const int MaxNameLength = 100;

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    private MediaCategory()
    {
        // EF Core
    }

    /// <summary>Dữ liệu hệ thống do migration tạo; factory này chỉ để dựng dữ liệu trong test.</summary>
    public static MediaCategory Create(string code, string name, int sortOrder) => new()
    {
        Id = Guid.NewGuid(),
        Code = Guard.NotNullOrWhiteSpace(code, nameof(code)).Trim().ToLowerInvariant(),
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim(),
        SortOrder = sortOrder,
    };
}

/// <summary>Code của các nhóm hệ thống — PHẢI khớp dữ liệu tạo sẵn trong migration AddMediaCategories và
/// hằng MEDIA_CATEGORY_CODES ở FE (features/media/types/media-category.types.ts).</summary>
public static class MediaCategoryCodes
{
    public const string Products = "products";
    public const string Articles = "articles";
    public const string Banners = "banners";
    public const string PageSections = "page-sections";
    public const string Brand = "brand";
    public const string Customers = "customers";
    public const string Payments = "payments";
    public const string Seo = "seo";

    /// <summary>Nhóm mặc định khi nơi upload không nói rõ (upload ở trang Thư viện Media, link ngoài...).</summary>
    public const string General = "general";

    /// <summary>Tên module trả về từ IMediaUsageSource → nhóm tương ứng (dùng để gán nhóm cho media có sẵn).</summary>
    public static string? FromUsageModule(string module) => module switch
    {
        "Sản phẩm" => Products,
        "Bài viết" => Articles,
        "Banner" => Banners,
        "Section trang" => PageSections,
        "Thông tin thương hiệu" => Brand,
        "Khách hàng" => Customers,
        "Phương thức thanh toán" => Payments,
        "SEO" => Seo,
        _ => null,
    };
}
