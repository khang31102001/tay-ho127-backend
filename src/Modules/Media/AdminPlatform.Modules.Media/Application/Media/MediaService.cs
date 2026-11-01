using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Media.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MediaEntity = AdminPlatform.Modules.Media.Domain.Media;

namespace AdminPlatform.Modules.Media.Application.Media;

public sealed class MediaService : IMediaService
{
    private static readonly MediaStorageProvider[] UploadProviders = [MediaStorageProvider.Local, MediaStorageProvider.Cloudinary];

    private readonly IMediaDbContext _db;
    private readonly IReadOnlyDictionary<MediaStorageProvider, IFileStorage> _fileStorages;
    private readonly MediaStorageOptions _storageOptions;

    public MediaService(IMediaDbContext db, IEnumerable<IFileStorage> fileStorages, IOptions<MediaStorageOptions> storageOptions)
    {
        _db = db;
        _fileStorages = fileStorages.ToDictionary(s => s.Provider);
        _storageOptions = storageOptions.Value;
    }

    public async Task<PagedResult<MediaResponse>> ListAsync(PagedRequest request, string? kind, string? status, string? storage, CancellationToken cancellationToken)
    {
        var query = _db.Media.AsNoTracking().AsQueryable();

        // Parsed before the query, not inside the Where lambda: EF evaluates in-expression calls while
        // translating the query and wraps their exceptions, so an invalid filter surfaced as a 500 instead
        // of the BusinessRuleValidationException's 400.
        if (!string.IsNullOrWhiteSpace(kind))
        {
            var parsedKind = ParseKind(kind);
            query = query.Where(m => m.Kind == parsedKind);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsedStatus = ParseStatus(status);
            query = query.Where(m => m.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(storage))
        {
            var parsedStorage = ParseStorage(storage);
            query = query.Where(m => m.StorageProvider == parsedStorage);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(m => EF.Functions.ILike(m.FileName, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(m => m.CreatedAtUtc) : query.OrderBy(m => m.CreatedAtUtc);

        // Mapped after paging (not inside the IQueryable projection): Kind/Status are enums and the
        // ToResponse() call below relies on Enum.ToString(), which the Npgsql query translator cannot
        // turn into SQL — see FiscalYearService/SystemSettingService for the inline-Select pattern this
        // deviates from, justified by these two enum-valued columns.
        var pagedMedia = await query.ToPagedResultAsync(request, cancellationToken);
        var items = pagedMedia.Items.Select(ToResponse).ToList();
        return new PagedResult<MediaResponse>(items, pagedMedia.Page, pagedMedia.PageSize, pagedMedia.TotalItems);
    }

    public async Task<MediaResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var media = await FindOrThrowAsync(id, cancellationToken);
        return ToResponse(media);
    }

    public async Task<MediaResponse> CreateAsync(CreateMediaRequest request, CancellationToken cancellationToken)
    {
        var media = MediaEntity.Create(request.FileName, request.Url, ParseKind(request.Type), request.AltText, request.Size, MediaStorageProvider.External);
        _db.Media.Add(media);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(media);
    }

    public async Task<MediaResponse> UploadAsync(MediaUploadFile file, string? altText, string? storage, string publicBaseUrl, CancellationToken cancellationToken)
    {
        var fileStorage = ResolveUploadStorage(storage);

        if (file.Length <= 0)
        {
            throw new BusinessRuleValidationException("File is empty.");
        }

        if (file.Length > _storageOptions.MaxFileBytes)
        {
            throw new BusinessRuleValidationException($"File exceeds the {_storageOptions.MaxFileBytes / (1024 * 1024)} MB limit.");
        }

        // Path.GetFileName: client có thể gửi tên kèm đường dẫn; chỉ giữ phần tên file.
        var originalName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalName);
        if (!MediaFileRules.TryGetKind(extension, out var kind))
        {
            throw new BusinessRuleValidationException($"File type '{extension}' is not allowed. Allowed: {MediaFileRules.AllowedExtensionsText}.");
        }

        var header = new byte[MediaFileRules.SignatureLength];
        var read = 0;
        while (read < header.Length)
        {
            var chunk = await file.Content.ReadAsync(header.AsMemory(read), cancellationToken);
            if (chunk == 0)
            {
                break;
            }

            read += chunk;
        }

        if (!MediaFileRules.HasValidSignature(extension, header[..read]))
        {
            throw new BusinessRuleValidationException("File content does not match its extension.");
        }

        file.Content.Seek(0, SeekOrigin.Begin);

        // Tên lưu trữ là Guid: không đoán được, không đụng tên, không mang ký tự lạ từ client.
        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var location = await fileStorage.SaveAsync(storedFileName, file.Content, cancellationToken);

        // Kiểm tra "/" thay vì Uri.IsAbsoluteUri: trên Linux "/uploads/x" cũng parse được thành file:// tuyệt đối.
        var url = location;
        if (location.StartsWith('/'))
        {
            var baseUrl = string.IsNullOrWhiteSpace(_storageOptions.PublicBaseUrl) ? publicBaseUrl : _storageOptions.PublicBaseUrl;
            url = $"{baseUrl.TrimEnd('/')}{location}";
        }

        var media = MediaEntity.Create(originalName, url, kind, altText, checked((int)file.Length), fileStorage.Provider);
        _db.Media.Add(media);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(media);
    }

    public IReadOnlyList<MediaStorageOptionResponse> GetStorageOptions()
    {
        var defaultProvider = DefaultUploadProvider();
        return UploadProviders
            .Select(p => new MediaStorageOptionResponse(p.ToString(), _fileStorages.ContainsKey(p), p == defaultProvider))
            .ToList();
    }

    public async Task<MediaResponse> UpdateAsync(Guid id, UpdateMediaRequest request, CancellationToken cancellationToken)
    {
        var media = await FindOrThrowAsync(id, cancellationToken);
        media.UpdateMetadata(request.FileName, request.Url, ParseKind(request.Type), request.AltText);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(media);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var media = await FindOrThrowAsync(id, cancellationToken);
        media.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<PublicMediaResponse>> ListPublicAsync(PagedRequest request, CancellationToken cancellationToken)
    {
        var query = _db.Media.AsNoTracking()
            .Where(m => m.Status == MediaStatus.Active)
            .OrderBy(m => m.CreatedAtUtc);

        var pagedMedia = await query.ToPagedResultAsync(request, cancellationToken);
        var items = pagedMedia.Items
            .Select(m => new PublicMediaResponse(m.Id, m.FileName, m.Url, m.Kind.ToString(), m.AltText))
            .ToList();
        return new PagedResult<PublicMediaResponse>(items, pagedMedia.Page, pagedMedia.PageSize, pagedMedia.TotalItems);
    }

    private async Task<MediaEntity> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Media.SingleOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Media), id);

    private IFileStorage ResolveUploadStorage(string? storage)
    {
        var provider = string.IsNullOrWhiteSpace(storage) ? DefaultUploadProvider() : ParseStorage(storage);
        if (!UploadProviders.Contains(provider))
        {
            throw new BusinessRuleValidationException($"Files cannot be uploaded to '{provider}'.");
        }

        return _fileStorages.TryGetValue(provider, out var fileStorage)
            ? fileStorage
            : throw new BusinessRuleValidationException($"Storage '{provider}' is not configured on the server.");
    }

    // MediaModule đã kiểm tra MediaStorage:Provider lúc khởi động; giá trị lạ ở đây (chỉ gặp trong test) coi như Local.
    private MediaStorageProvider DefaultUploadProvider() =>
        Enum.TryParse<MediaStorageProvider>(_storageOptions.Provider, ignoreCase: true, out var parsed) && UploadProviders.Contains(parsed)
            ? parsed
            : MediaStorageProvider.Local;

    // IsDefined: Enum.TryParse nhận cả chuỗi số ("7") và trả về giá trị không thuộc enum.
    private static MediaStorageProvider ParseStorage(string storage) =>
        Enum.TryParse<MediaStorageProvider>(storage, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new BusinessRuleValidationException($"'{storage}' is not a valid storage.");

    private static MediaKind ParseKind(string kind) =>
        Enum.TryParse<MediaKind>(kind, ignoreCase: true, out var parsed)
            ? parsed
            : throw new BusinessRuleValidationException($"'{kind}' is not a valid media type.");

    private static MediaStatus ParseStatus(string status) =>
        Enum.TryParse<MediaStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : throw new BusinessRuleValidationException($"'{status}' is not a valid status.");

    private static MediaResponse ToResponse(MediaEntity media) => new(
        media.Id,
        media.FileName,
        media.Url,
        media.Kind.ToString(),
        media.AltText,
        media.Size,
        media.Status.ToString(),
        media.StorageProvider.ToString(),
        media.CreatedAtUtc,
        media.UpdatedAtUtc);
}
