namespace AdminPlatform.Modules.Catalog.Application.Ports;

/// <summary>Port: Catalog cần biết một media id có tồn tại, có phải ảnh, còn hoạt động không và URL của nó
/// (ảnh đại diện, danh sách ảnh sản phẩm). Host cài bằng CatalogMediaLookupAdapter gọi module Media.</summary>
public interface ICatalogMediaLookup
{
    /// <summary>Chỉ trả các id có trong media.media (kể cả đã ẩn); id không có trong kết quả = không tồn tại.</summary>
    Task<IReadOnlyDictionary<Guid, CatalogMediaInfo>> FindAsync(IReadOnlyCollection<Guid> mediaIds, CancellationToken cancellationToken);
}

public sealed record CatalogMediaInfo(Guid Id, string Url, bool IsImage, bool IsActive);
