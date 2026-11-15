using AdminPlatform.Modules.Media.Domain;

namespace AdminPlatform.Modules.Media.Application.Media;

/// <summary>Nơi lưu byte của file upload. MediaService chỉ biết interface này, nên việc chuyển từ disk
/// sang cloud không đụng tới nghiệp vụ media.</summary>
public interface IFileStorage
{
    /// <summary>Nơi lưu mà implementation này đại diện — Admin chọn theo giá trị này khi upload.</summary>
    MediaStorageProvider Provider { get; }

    /// <summary>Lưu file với tên lưu trữ <paramref name="storedFileName"/> và trả về nơi truy cập công khai:
    /// đường dẫn bắt đầu bằng "/" (vd. "/uploads/abc.jpg", MediaService ghép thêm origin) hoặc URL tuyệt đối
    /// (vd. URL Cloudinary, dùng nguyên).</summary>
    Task<string> SaveAsync(string storedFileName, Stream content, CancellationToken cancellationToken);

    /// <summary>Xóa vĩnh viễn file đã lưu, tìm theo URL mà <see cref="SaveAsync"/> từng trả về (DB chỉ lưu URL).
    /// File không còn trên nơi lưu = coi như đã xóa. Lỗi của nơi lưu (mạng, sai khóa...) → ném
    /// <see cref="InvalidOperationException"/>; URL không thuộc nơi lưu này → <see cref="ArgumentException"/>.</summary>
    Task DeleteAsync(string url, MediaKind kind, CancellationToken cancellationToken);
}

/// <summary>Tách tên file lưu trữ ("&lt;guid&gt;.png") ở cuối URL — dùng chung cho các nơi lưu.</summary>
public static class StoredFileUrl
{
    public static string LastSegment(string url)
    {
        var path = Uri.TryCreate(url, UriKind.Absolute, out var absolute) ? absolute.AbsolutePath : url.Split('?', '#')[0];
        // Tên lưu trữ luôn là "<guid>.<đuôi>": URL kết thúc bằng "/" hay không có đuôi file = không trỏ tới file.
        var segment = Uri.UnescapeDataString(path.Split('/').LastOrDefault() ?? string.Empty);
        if (segment.Length == 0 || segment != Path.GetFileName(segment) || segment.Contains("..") || !Path.HasExtension(segment))
        {
            throw new ArgumentException("The media URL does not point to a stored file.", nameof(url));
        }

        return segment;
    }
}
