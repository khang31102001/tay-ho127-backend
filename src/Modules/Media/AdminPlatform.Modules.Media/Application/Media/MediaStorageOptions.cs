namespace AdminPlatform.Modules.Media.Application.Media;

/// <summary>Cấu hình lưu file upload (section "MediaStorage"). <see cref="Provider"/> chọn nơi lưu:
/// "Local" (disk của server) hoặc "Cloudinary". Đổi qua lại chỉ cần đổi cấu hình — URL đã lưu trong DB
/// là URL tuyệt đối nên ảnh cũ vẫn hiển thị.</summary>
public sealed class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";

    public const string LocalProvider = "Local";
    public const string CloudinaryProvider = "Cloudinary";

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
}
