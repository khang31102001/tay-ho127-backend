namespace AdminPlatform.Modules.Seo.Application.Schemas;

public interface ISeoSchemaService
{
    /// <summary>The schema row of one entity and type (whatever its active flag), or null when it has none.</summary>
    Task<SeoSchemaResponse?> FindAsync(string entityType, string? entityId, string schemaType, CancellationToken cancellationToken);

    /// <summary>Like <see cref="FindAsync"/> but only when the row is switched on — what the website may use.</summary>
    Task<SeoSchemaResponse?> FindActiveAsync(string entityType, string? entityId, string schemaType, CancellationToken cancellationToken);

    /// <summary>Creates the row, or replaces it when the entity already has one for that schema type.</summary>
    Task<SeoSchemaResponse> UpsertAsync(UpsertSeoSchemaRequest request, CancellationToken cancellationToken);

    /// <summary>Removes the row so the schema is generated again. Idempotent: no row is not an error.</summary>
    Task ResetAsync(string entityType, string? entityId, string schemaType, CancellationToken cancellationToken);
}
