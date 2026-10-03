using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Seo.Application.Metadata;

public interface ISeoMetadataService
{
    /// <param name="entityType">Filters by entity type wire name (e.g. "product").</param>
    Task<PagedResult<SeoMetadataResponse>> ListAsync(PagedRequest request, string? entityType, CancellationToken cancellationToken);

    /// <summary>The override of one entity, or null when it has none (it then uses the defaults).</summary>
    Task<SeoMetadataResponse?> FindAsync(string entityType, string? entityId, CancellationToken cancellationToken);

    /// <summary>Creates the entity's override, or replaces it when it already has one.</summary>
    Task<SeoMetadataResponse> UpsertAsync(UpsertSeoMetadataRequest request, CancellationToken cancellationToken);

    /// <summary>Removes the entity's override so it goes back to the defaults. Idempotent: no override is not an error.</summary>
    Task ResetAsync(string entityType, string? entityId, CancellationToken cancellationToken);

    /// <summary>Entities whose override sets robots index off.</summary>
    Task<IReadOnlyList<NoIndexEntityResponse>> ListNoIndexAsync(CancellationToken cancellationToken);
}
