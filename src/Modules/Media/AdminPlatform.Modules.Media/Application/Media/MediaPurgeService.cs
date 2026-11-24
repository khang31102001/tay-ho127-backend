using AdminPlatform.Common.Abstractions;
using AdminPlatform.Modules.Media.Domain;
using AdminPlatform.SharedKernel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MediaEntity = AdminPlatform.Modules.Media.Domain.Media;

namespace AdminPlatform.Modules.Media.Application.Media;

/// <summary>Xóa vĩnh viễn media để dọn kho lưu trữ (khác "Xóa" thường = chỉ ngừng hoạt động).</summary>
public interface IMediaPurgeService
{
    /// <summary>Những nơi còn dùng media (ở mọi module đã đăng ký <see cref="IMediaUsageSource"/>).</summary>
    Task<IReadOnlyList<MediaUsageResponse>> GetUsagesAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Xóa file trên nơi lưu rồi xóa bản ghi, từng media một. Nơi đang dùng media được tự gỡ tham chiếu; chỉ khi còn
    /// tham chiếu không gỡ được (ảnh chèn trong HTML) thì bỏ qua (inUse).</summary>
    Task<PurgeMediaResponse> PurgeAsync(PurgeMediaRequest request, CancellationToken cancellationToken);
}

public sealed record PurgeMediaRequest(IReadOnlyList<Guid> Ids);

public sealed record MediaUsageResponse(string Module, string EntityId, string EntityName, string Field, bool CanDetach = true);

/// <param name="Status">deleted | inUse | notFound | failed.</param>
public sealed record PurgeMediaResult(Guid Id, string Status, string? FileName, string? Message, IReadOnlyList<MediaUsageResponse> Usages);

public sealed record PurgeMediaResponse(int DeletedCount, IReadOnlyList<PurgeMediaResult> Results);

public static class PurgeMediaStatus
{
    public const string Deleted = "deleted";
    public const string InUse = "inUse";
    public const string NotFound = "notFound";
    public const string Failed = "failed";
}

public sealed class PurgeMediaRequestValidator : AbstractValidator<PurgeMediaRequest>
{
    public const int MaxIds = 100;

    public PurgeMediaRequestValidator()
    {
        RuleFor(x => x.Ids).NotEmpty().Must(ids => ids.Count <= MaxIds).WithMessage($"At most {MaxIds} media can be deleted at once.");
        RuleForEach(x => x.Ids).NotEmpty();
    }
}

public sealed class MediaPurgeService : IMediaPurgeService
{
    private readonly IMediaDbContext _db;
    private readonly IReadOnlyDictionary<MediaStorageProvider, IFileStorage> _fileStorages;
    private readonly IReadOnlyList<IMediaUsageSource> _usageSources;
    private readonly IReadOnlyList<IMediaUsageDetacher> _usageDetachers;
    private readonly ILogger<MediaPurgeService> _logger;

    public MediaPurgeService(
        IMediaDbContext db,
        IEnumerable<IFileStorage> fileStorages,
        IEnumerable<IMediaUsageSource> usageSources,
        IEnumerable<IMediaUsageDetacher> usageDetachers,
        ILogger<MediaPurgeService> logger)
    {
        _db = db;
        _fileStorages = fileStorages.ToDictionary(s => s.Provider);
        _usageSources = usageSources.ToList();
        _usageDetachers = usageDetachers.ToList();
        _logger = logger;
    }

    public async Task<IReadOnlyList<MediaUsageResponse>> GetUsagesAsync(Guid id, CancellationToken cancellationToken)
    {
        var media = await _db.Media.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Media), id);

        var usages = await FindUsagesAsync([media], cancellationToken);
        return usages.GetValueOrDefault(id) ?? [];
    }

    public async Task<PurgeMediaResponse> PurgeAsync(PurgeMediaRequest request, CancellationToken cancellationToken)
    {
        var ids = request.Ids.Distinct().ToList();
        var mediaById = await _db.Media.Where(m => ids.Contains(m.Id)).ToDictionaryAsync(m => m.Id, cancellationToken);
        var usages = await FindUsagesAsync(mediaById.Values.ToList(), cancellationToken);

        var results = new List<PurgeMediaResult>();
        foreach (var id in ids)
        {
            if (!mediaById.TryGetValue(id, out var media))
            {
                results.Add(new PurgeMediaResult(id, PurgeMediaStatus.NotFound, null, "Media không tồn tại.", []));
                continue;
            }

            var mediaUsages = usages.GetValueOrDefault(id) ?? [];
            var blocking = mediaUsages.Where(u => !u.CanDetach).ToList();
            if (blocking.Count > 0)
            {
                results.Add(new PurgeMediaResult(id, PurgeMediaStatus.InUse, media.FileName,
                    "Media đang được chèn trong nội dung (HTML) nên không tự gỡ được, hãy xóa ảnh khỏi nội dung ở các nơi bên dưới trước.", blocking));
                continue;
            }

            results.Add(await PurgeOneAsync(media, mediaUsages, cancellationToken));
        }

        return new PurgeMediaResponse(results.Count(r => r.Status == PurgeMediaStatus.Deleted), results);
    }

    /// <summary>File trước, bản ghi sau: nơi lưu lỗi thì bản ghi còn nguyên để thử lại, không bao giờ để file mồ côi
    /// mà không còn dấu vết trong thư viện.</summary>
    private async Task<PurgeMediaResult> PurgeOneAsync(MediaEntity media, IReadOnlyList<MediaUsageResponse> detachedUsages, CancellationToken cancellationToken)
    {
        if (media.StorageProvider != MediaStorageProvider.External)
        {
            if (!_fileStorages.TryGetValue(media.StorageProvider, out var storage))
            {
                return Failed(media, $"Nơi lưu {media.StorageProvider} chưa được cấu hình trên server nên không xóa được file.");
            }

            try
            {
                await storage.DeleteAsync(media.Url, media.Kind, cancellationToken);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException)
            {
                _logger.LogWarning(ex, "Purge media {MediaId}: deleting the stored file failed", media.Id);
                return Failed(media, "Không xóa được file trên nơi lưu, vui lòng thử lại sau.");
            }
        }

        // Quy tắc xóa: nơi đang dùng media tự nhả (đặt null / bỏ khỏi danh sách) thay vì chặn xóa.
        var target = new[] { new MediaUsageTarget(media.Id, media.Url) };
        foreach (var detacher in _usageDetachers)
        {
            await detacher.DetachAsync(target, cancellationToken);
        }

        _db.Media.Remove(media);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Media {MediaId} ({FileName}, {Storage}) permanently deleted; detached from {UsageCount} place(s)",
            media.Id, media.FileName, media.StorageProvider, detachedUsages.Count);
        return new PurgeMediaResult(media.Id, PurgeMediaStatus.Deleted, media.FileName, null, detachedUsages);
    }

    private static PurgeMediaResult Failed(MediaEntity media, string message) =>
        new(media.Id, PurgeMediaStatus.Failed, media.FileName, message, []);

    private async Task<Dictionary<Guid, List<MediaUsageResponse>>> FindUsagesAsync(IReadOnlyList<MediaEntity> media, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, List<MediaUsageResponse>>();
        if (media.Count == 0)
        {
            return result;
        }

        var targets = media.Select(m => new MediaUsageTarget(m.Id, m.Url)).ToList();
        foreach (var source in _usageSources)
        {
            foreach (var usage in await source.FindUsagesAsync(targets, cancellationToken))
            {
                if (!result.TryGetValue(usage.MediaId, out var list))
                {
                    result[usage.MediaId] = list = [];
                }

                var response = new MediaUsageResponse(usage.Module, usage.EntityId, usage.EntityName, usage.Field, usage.CanDetach);
                if (!list.Contains(response))
                {
                    list.Add(response);
                }
            }
        }

        return result;
    }
}
