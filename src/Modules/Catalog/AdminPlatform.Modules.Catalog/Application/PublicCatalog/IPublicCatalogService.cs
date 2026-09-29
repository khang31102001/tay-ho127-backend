namespace AdminPlatform.Modules.Catalog.Application.PublicCatalog;

public interface IPublicCatalogService
{
    Task<PublicCatalogResponse> GetAsync(CancellationToken cancellationToken);
}
