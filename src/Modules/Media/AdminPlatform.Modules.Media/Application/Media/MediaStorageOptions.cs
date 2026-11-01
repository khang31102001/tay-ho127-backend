namespace AdminPlatform.Modules.Media.Application.Media;

/// <summary>Cấu hình lưu file upload (section "MediaStorage"). Local (disk của server) luôn dùng được;
/// Cloudinary dùng được khi đủ 3 khóa. Admin chọn nơi lưu cho từng lần upload, <see cref="Provider"/> là
/// nơi mặc định khi không chọn. URL lưu trong DB là URL tuyệt đối nên ảnh cũ không phụ thuộc cấu hình.</summary>
public sealed class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";

    public const string LocalProvider = "Local";
    public const string CloudinaryProvider = "Cloudinary";

    /// <summary>Nơi lưu mặc định: "Local" hoặc "Cloudinary".</summary>
    public string Provider { get; set; } = LocalProvider;

    /// <summary>Thư mục gốc chứa file. Đường dẫn tương đối được tính từ ContentRoot của Host.</summary>
    public string RootPath { get; set; } = "uploads";

    /// <summary>Đường dẫn URL công khai phục vụ file, vd. "/uploads".</summary>
    public string RequestPath { get; set; } = "/uploads";

    /// <summary>Origin công khai của Backend (vd. https://api.example.com). Để trống thì lấy từ request.
    /// Nên đặt khi chạy sau reverse proxy để URL lưu vào DB luôn đúng scheme/host. Chỉ dùng cho Local.</summary>
    public string? PublicBaseUrl { get; set; }

    public long MaxFileBytes { get; set; } = 10 * 1024 * 1024;

    public CloudinaryStorageOptions Cloudinary { get; set; } = new();
}

/// <summary>Section "MediaStorage:Cloudinary". Khóa lấy ở Cloudinary Console → API Keys; ApiSecret chỉ đặt
/// qua biến môi trường, không ghi vào file commit.</summary>
public sealed class CloudinaryStorageOptions
{
    public string CloudName { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>Thư mục trên Cloudinary chứa file của site, vd. "tayho127".</summary>
    public string Folder { get; set; } = "tayho127";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CloudName) && !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ApiSecret);
}
