namespace AdminPlatform.Modules.Media.Application.Media;

/// <summary>Nơi lưu byte của file upload. MediaService chỉ biết interface này, nên việc chuyển từ disk
/// sang cloud không đụng tới nghiệp vụ media.</summary>
public interface IFileStorage
{
    /// <summary>Lưu file với tên lưu trữ <paramref name="storedFileName"/> và trả về đường dẫn công khai
    /// (bắt đầu bằng "/", vd. "/uploads/abc.jpg").</summary>
    Task<string> SaveAsync(string storedFileName, Stream content, CancellationToken cancellationToken);
}
