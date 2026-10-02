namespace AdminPlatform.Modules.Catalog.Domain;

/// <summary>Ordered product image link. MediaId is an opaque reference with no FK: it is either a Media
/// module id (GUID text) or a legacy key of an image bundled with the website (e.g. "media-banh-cuon-dish")
/// that the site resolves itself — modules never share tables (ARCHITECTURE.md).</summary>
public sealed class ProductMedia
{
    public const int MaxMediaIdLength = 64;

    public Guid ProductId { get; private set; }
    public string MediaId { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    private ProductMedia()
    {
        // EF Core
    }

    internal static ProductMedia Create(Guid productId, string mediaId, int sortOrder) =>
        new() { ProductId = productId, MediaId = mediaId, SortOrder = sortOrder };

    internal void MoveTo(int sortOrder) => SortOrder = sortOrder;
}
