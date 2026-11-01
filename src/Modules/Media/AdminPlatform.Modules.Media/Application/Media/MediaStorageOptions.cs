namespace AdminPlatform.Modules.Media.Application.Media;

/// <summary>Cấu hình lưu file upload (section "MediaStorage"). Local (disk của server) luôn dùng được;
/// Cloudinary dùng được khi đủ 3 khóa; S3 (Vietnix Object Storage...) dùng được khi đủ bucket, khóa và URL
/// công khai. Admin chọn nơi lưu cho từng lần upload, <see cref="Provider"/> là
/// nơi mặc định khi không chọn. URL lưu trong DB là URL tuyệt đối nên ảnh cũ không phụ thuộc cấu hình.</summary>
public sealed class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";

    public const string LocalProvider = "Local";
    public const string CloudinaryProvider = "Cloudinary";
    public const string S3Provider = "S3";

    /// <summary>Nơi lưu mặc định: "Local", "Cloudinary" hoặc "S3".</summary>
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

    public S3StorageOptions S3 { get; set; } = new();
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

/// <summary>Section "MediaStorage:S3" — Object Storage chuẩn S3 (Vietnix Object Storage, MinIO, AWS...). Khóa lấy
/// ở trang quản lý Object Storage; SecretKey chỉ đặt qua biến môi trường, không ghi vào file commit.</summary>
public sealed class S3StorageOptions
{
    /// <summary>Endpoint S3 của nhà cung cấp, vd. https://s3.example.vn. Để trống = AWS theo <see cref="Region"/>.</summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>Region dùng để ký request. Nhà cung cấp ngoài AWS thường chấp nhận "us-east-1".</summary>
    public string Region { get; set; } = "us-east-1";

    public string Bucket { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Thư mục (tiền tố key) chứa file của site, vd. "tayho127".</summary>
    public string Folder { get; set; } = "tayho127";

    /// <summary>URL công khai trỏ tới gốc bucket (vd. https://s3.example.vn/tayho127 hoặc domain CDN riêng). URL lưu
    /// vào DB = giá trị này + "/" + key của file.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>Canned ACL gắn cho từng file, mặc định "public-read". Để trống khi bucket đã public bằng
    /// bucket policy hoặc nhà cung cấp không hỗ trợ ACL.</summary>
    public string ObjectAcl { get; set; } = "public-read";

    /// <summary>true = URL dạng endpoint/bucket/key (đa số nhà cung cấp ngoài AWS cần kiểu này).</summary>
    public bool ForcePathStyle { get; set; } = true;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Bucket) && !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey)
        && !string.IsNullOrWhiteSpace(PublicBaseUrl);
}
