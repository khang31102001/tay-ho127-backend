using AdminPlatform.Modules.Seo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Seo.Infrastructure.Configurations;

internal sealed class SeoSchemaConfiguration : IEntityTypeConfiguration<SeoSchema>
{
    public void Configure(EntityTypeBuilder<SeoSchema> builder)
    {
        builder.ToTable("seo_schemas");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.Property(s => s.EntityType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(s => s.EntityId).HasMaxLength(SeoMetadata.MaxEntityIdLength);
        builder.Property(s => s.SchemaType).HasConversion<string>().HasMaxLength(32).IsRequired();

        // A small JSON object of strings, read back whole — never queried by key.
        builder.Property(s => s.ConfigJson).HasColumnType("jsonb");
        builder.Property(s => s.CustomJsonLd).HasColumnType("text");

        // The computed view of ConfigJson, not a column.
        builder.Ignore(s => s.Config);

        // One row per entity and schema type. Postgres treats NULLs as distinct, so the homepage (no entity id)
        // needs its own filtered unique index.
        builder.HasIndex(s => new { s.EntityType, s.EntityId, s.SchemaType }).IsUnique();
        builder.HasIndex(s => new { s.EntityType, s.SchemaType }).IsUnique()
            .HasFilter("entity_id IS NULL").HasDatabaseName("ix_seo_schemas_entity_type_schema_type_homepage");
    }
}
