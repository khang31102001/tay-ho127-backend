using AdminPlatform.Modules.Catalog.Application.Ports;
using AdminPlatform.Modules.Media.Application.Media;

namespace AdminPlatform.Api.CrossModuleAdapters;

/// <summary>Implements Catalog's ICatalogMediaLookup port with the Media module's IMediaReferenceQueryService.</summary>
internal sealed class CatalogMediaLookupAdapter : ICatalogMediaLookup
{
    private readonly IMediaReferenceQueryService _media;

    public CatalogMediaLookupAdapter(IMediaReferenceQueryService media)
    {
        _media = media;
    }

    public async Task<IReadOnlyDictionary<Guid, CatalogMediaInfo>> FindAsync(
        IReadOnlyCollection<Guid> mediaIds, CancellationToken cancellationToken)
    {
        var media = await _media.FindAsync(mediaIds, cancellationToken);
        return media.ToDictionary(
            m => m.Id,
            m => new CatalogMediaInfo(m.Id, m.Url, m.Type == "image", m.IsActive));
    }
}
