using AdminPlatform.Modules.Media.Application.Media;
using AdminPlatform.Modules.Media.Domain;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace AdminPlatform.Modules.Media.Infrastructure;

/// <summary>Lưu file lên Cloudinary (MediaStorage:Provider = Cloudinary) và trả về secure_url tuyệt đối.
/// Upload đi qua Backend (không cho trình duyệt upload thẳng) để ApiSecret chỉ nằm trên server và vẫn qua
/// kiểm tra quyền + MediaFileRules của MediaService.</summary>
public sealed class CloudinaryFileStorage : IFileStorage
{
    private readonly Cloudinary _cloudinary;
    private readonly string _folder;

    public MediaStorageProvider Provider => MediaStorageProvider.Cloudinary;

    public CloudinaryFileStorage(IOptions<MediaStorageOptions> options)
    {
        var value = options.Value.Cloudinary;
        _cloudinary = new Cloudinary(new Account(value.CloudName, value.ApiKey, value.ApiSecret));
        _cloudinary.Api.Secure = true;
        _folder = value.Folder.Trim('/');
    }

    public async Task<string> SaveAsync(string storedFileName, Stream content, CancellationToken cancellationToken)
    {
        if (storedFileName != Path.GetFileName(storedFileName))
        {
            throw new ArgumentException("Invalid stored file name.", nameof(storedFileName));
        }

        var extension = Path.GetExtension(storedFileName);
        if (!MediaFileRules.TryGetKind(extension, out var kind))
        {
            throw new ArgumentException($"Unsupported file type '{extension}'.", nameof(storedFileName));
        }

        var file = new FileDescription(storedFileName, content);
        var prefix = _folder.Length == 0 ? string.Empty : _folder + "/";
        var publicId = prefix + Path.GetFileNameWithoutExtension(storedFileName);

        // PDF upload dạng "raw": tài khoản Free mặc định chặn phân phối PDF dạng image. public_id của raw phải
        // giữ đuôi file thì URL mới có đuôi.
        UploadResult result = kind switch
        {
            MediaKind.Image => await _cloudinary.UploadAsync(new ImageUploadParams { File = file, PublicId = publicId, Overwrite = false }, cancellationToken),
            MediaKind.Video => await _cloudinary.UploadAsync(new VideoUploadParams { File = file, PublicId = publicId, Overwrite = false }, cancellationToken),
            _ => await _cloudinary.UploadAsync(new RawUploadParams { File = file, PublicId = prefix + storedFileName, Overwrite = false }, cancellationToken: cancellationToken),
        };

        if (result.Error is not null || result.SecureUrl is null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error?.Message ?? "no URL returned"}.");
        }

        return result.SecureUrl.ToString();
    }
}
