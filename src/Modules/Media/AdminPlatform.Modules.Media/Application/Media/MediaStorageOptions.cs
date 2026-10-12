namespace AdminPlatform.Modules.Media.Application.Media;

/// <summary>Cấu hình lưu file upload (section "MediaStorage"). Hiện lưu trên disk của server;
/// đổi sang cloud (Cloudinary/S3) chỉ cần thay implementation của <see cref="IFileStorage"/>.</summary>
public sealed class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";

    /// <summary>Thư mục gốc chứa file. Đường dẫn tương đối được tính từ ContentRoot của Host.</summary>
    public string RootPath { get; set; } = "uploads";

    /// <summary>Đường dẫn URL công khai phục vụ file, vd. "/uploads".</summary>
    public string RequestPath { get; set; } = "/uploads";

    /// <summary>Origin công khai của Backend (vd. https://api.example.com). Để trống thì lấy từ request.
    /// Nên đặt khi chạy sau reverse proxy để URL lưu vào DB luôn đúng scheme/host.</summary>
    public string? PublicBaseUrl { get; set; }

    public long MaxFileBytes { get; set; } = 10 * 1024 * 1024;
}
