using AdminPlatform.Modules.Seo.Application.Metadata;
using AdminPlatform.Modules.Seo.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Seo.Application.Schemas;

public sealed class SeoSchemaService : ISeoSchemaService
{
    private readonly ISeoDbContext _db;

    public SeoSchemaService(ISeoDbContext db)
    {
        _db = db;
    }

    public async Task<SeoSchemaResponse?> FindAsync(
        string entityType, string? entityId, string schemaType, CancellationToken cancellationToken)
    {
        var (type, id, schema) = ParseKey(entityType, entityId, schemaType);

        var row = await _db.SeoSchemas.AsNoTracking()
            .SingleOrDefaultAsync(s => s.EntityType == type && s.EntityId == id && s.SchemaType == schema, cancellationToken);
        return row is null ? null : ToResponse(row);
    }

    public async Task<SeoSchemaResponse?> FindActiveAsync(
        string entityType, string? entityId, string schemaType, CancellationToken cancellationToken)
    {
        var (type, id, schema) = ParseKey(entityType, entityId, schemaType);

        var row = await _db.SeoSchemas.AsNoTracking()
            .SingleOrDefaultAsync(s => s.EntityType == type && s.EntityId == id && s.SchemaType == schema && s.IsActive, cancellationToken);
        return row is null ? null : ToResponse(row);
    }

    public async Task<SeoSchemaResponse> UpsertAsync(UpsertSeoSchemaRequest request, CancellationToken cancellationToken)
    {
        var (type, id, schema) = ParseKey(request.EntityType, request.EntityId, request.SchemaType);
        var details = new SeoSchemaDetails(request.Config, request.CustomJsonLd, request.IsCustomOverride, request.IsActive);

        var row = await _db.SeoSchemas
            .SingleOrDefaultAsync(s => s.EntityType == type && s.EntityId == id && s.SchemaType == schema, cancellationToken);
        if (row is null)
        {
            row = SeoSchema.Create(type, id, schema, details);
            _db.SeoSchemas.Add(row);
        }
        else
        {
            row.Update(details);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(row);
    }

    public async Task ResetAsync(string entityType, string? entityId, string schemaType, CancellationToken cancellationToken)
    {
        var (type, id, schema) = ParseKey(entityType, entityId, schemaType);

        var row = await _db.SeoSchemas
            .SingleOrDefaultAsync(s => s.EntityType == type && s.EntityId == id && s.SchemaType == schema, cancellationToken);
        if (row is null)
        {
            return;
        }

        _db.SeoSchemas.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static (SeoEntityType Type, string? Id, SeoSchemaType Schema) ParseKey(string entityType, string? entityId, string schemaType)
    {
        if (!SeoEntityTypeWire.TryParse(entityType, out var type))
        {
            throw new BusinessRuleValidationException($"EntityType must be {SeoEntityTypeWire.AllowedValues}.");
        }

        if (!SeoSchemaTypeWire.TryParse(schemaType, out var schema))
        {
            throw new BusinessRuleValidationException($"SchemaType must be {SeoSchemaTypeWire.AllowedValues}.");
        }

        return (type, SeoMetadata.NormalizeEntityId(type, entityId), schema);
    }

    internal static SeoSchemaResponse ToResponse(SeoSchema s) => new(
        s.Id, SeoEntityTypeWire.ToWire(s.EntityType), s.EntityId, SeoSchemaTypeWire.ToWire(s.SchemaType),
        s.ConfigJson is null ? null : s.Config, s.CustomJsonLd, s.IsCustomOverride, s.IsActive, s.CreatedAtUtc,
        s.UpdatedAtUtc ?? s.CreatedAtUtc);
}
