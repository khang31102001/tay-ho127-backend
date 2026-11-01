using AdminPlatform.Modules.Media.Application.Media;
using AdminPlatform.Modules.Media.Domain;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace AdminPlatform.Modules.Media.Infrastructure;

/// <summary>Lưu file lên Object Storage chuẩn S3 (MediaStorage:Provider = S3, vd. Vietnix Object Storage) và trả
/// về URL công khai tuyệt đối. Upload đi qua Backend như Cloudinary: SecretKey chỉ nằm trên server và file vẫn
/// qua kiểm tra quyền + MediaFileRules của MediaService.</summary>
public sealed class S3FileStorage : IFileStorage, IDisposable
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".mp4"] = "video/mp4",
        [".webm"] = "video/webm",
        [".pdf"] = "application/pdf",
    };

    private readonly AmazonS3Client _client;
    private readonly S3StorageOptions _options;
    private readonly string _prefix;
    private readonly string _publicBaseUrl;

    public MediaStorageProvider Provider => MediaStorageProvider.S3;

    public S3FileStorage(IOptions<MediaStorageOptions> options)
    {
        _options = options.Value.S3;
        var config = new AmazonS3Config
        {
            ForcePathStyle = _options.ForcePathStyle,
            // SDK mặc định gửi checksum CRC kiểu mới mà nhiều nhà cung cấp S3 ngoài AWS chưa hỗ trợ → chỉ gửi khi bắt buộc.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };
        if (string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_options.Region);
        }
        else
        {
            config.ServiceURL = _options.ServiceUrl;
            config.AuthenticationRegion = _options.Region;
        }

        _client = new AmazonS3Client(new BasicAWSCredentials(_options.AccessKey, _options.SecretKey), config);
        var folder = _options.Folder.Trim('/');
        _prefix = folder.Length == 0 ? string.Empty : folder + "/";
        _publicBaseUrl = _options.PublicBaseUrl.TrimEnd('/');
    }

    public async Task<string> SaveAsync(string storedFileName, Stream content, CancellationToken cancellationToken)
    {
        if (storedFileName != Path.GetFileName(storedFileName))
        {
            throw new ArgumentException("Invalid stored file name.", nameof(storedFileName));
        }

        var extension = Path.GetExtension(storedFileName);
        if (!ContentTypes.TryGetValue(extension, out var contentType))
        {
            throw new ArgumentException($"Unsupported file type '{extension}'.", nameof(storedFileName));
        }

        var key = _prefix + storedFileName;
        var request = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            // Upload dạng aws-chunked không phải nhà cung cấp nào cũng nhận; stream đã seek được nên gửi một lần.
            UseChunkEncoding = false,
        };
        // Tên lưu trữ là Guid nên nội dung không bao giờ đổi → cho trình duyệt/CDN cache lâu.
        request.Headers.CacheControl = "public, max-age=31536000, immutable";
        if (!string.IsNullOrWhiteSpace(_options.ObjectAcl))
        {
            request.CannedACL = new S3CannedACL(_options.ObjectAcl);
        }

        try
        {
            await _client.PutObjectAsync(request, cancellationToken);
        }
        catch (AmazonS3Exception ex)
        {
            throw new InvalidOperationException($"S3 upload failed: {ex.ErrorCode} {ex.Message}", ex);
        }

        return $"{_publicBaseUrl}/{key}";
    }

    public void Dispose() => _client.Dispose();
}
