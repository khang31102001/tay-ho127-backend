using AdminPlatform.Modules.Media.Application.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace AdminPlatform.Modules.Media.Infrastructure;

/// <summary>Lưu file vào thư mục trên disk của server. Lưu ý: disk của một số host (vd. Render free tier)
/// bị xóa khi redeploy — chỉ dùng tạm cho đến khi chuyển sang cloud storage.</summary>
public sealed class LocalDiskFileStorage : IFileStorage
{
    private readonly string _rootPath;
    private readonly string _requestPath;

    public LocalDiskFileStorage(IOptions<MediaStorageOptions> options, IWebHostEnvironment environment)
    {
        var value = options.Value;
        _rootPath = ResolveRootPath(value.RootPath, environment.ContentRootPath);
        _requestPath = "/" + value.RequestPath.Trim('/');
        Directory.CreateDirectory(_rootPath);
    }

    public static string ResolveRootPath(string rootPath, string contentRootPath) =>
        Path.IsPathRooted(rootPath) ? rootPath : Path.Combine(contentRootPath, rootPath);

    public async Task<string> SaveAsync(string storedFileName, Stream content, CancellationToken cancellationToken)
    {
        // Chặn path traversal: chỉ nhận tên file thuần, không có thư mục.
        if (storedFileName != Path.GetFileName(storedFileName))
        {
            throw new ArgumentException("Invalid stored file name.", nameof(storedFileName));
        }

        var fullPath = Path.Combine(_rootPath, storedFileName);
        await using var target = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(target, cancellationToken);

        return $"{_requestPath}/{storedFileName}";
    }
}
