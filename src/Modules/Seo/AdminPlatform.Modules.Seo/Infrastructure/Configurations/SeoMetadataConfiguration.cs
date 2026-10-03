using AdminPlatform.Modules.Seo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Seo.Infrastructure.Configurations;

internal sealed class SeoMetadataConfiguration : IEntityTypeConfiguration<SeoMetadata>
{
    public void Configure(EntityTypeBuilder<SeoMetadata> builder)
    {
        builder.ToTable("seo_metadata");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.RowVersion).IsRowVersion();

        builder.Property(m => m.EntityType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(m => m.EntityId).HasMaxLength(SeoMetadata.MaxEntityIdLength);
        builder.Property(m => m.MetaTitle).HasMaxLength(SeoMetadata.MaxTitleLength);
        builder.Property(m => m.MetaDescription).HasMaxLength(SeoMetadata.MaxDescriptionLength);
        builder.Property(m => m.CanonicalUrl).HasMaxLength(SeoMetadata.MaxUrlLength);
        builder.Property(m => m.OgTitle).HasMaxLength(SeoMetadata.MaxTitleLength);
        builder.Property(m => m.OgDescription).HasMaxLength(SeoMetadata.MaxDescriptionLength);
        builder.Property(m => m.OgImageMediaId).HasMaxLength(SeoMetadata.MaxMediaIdLength);
        builder.Property(m => m.TwitterTitle).HasMaxLength(SeoMetadata.MaxTitleLength);
        builder.Property(m => m.TwitterDescription).HasMaxLength(SeoMetadata.MaxDescriptionLength);
        builder.Property(m => m.TwitterImageMediaId).HasMaxLength(SeoMetadata.MaxMediaIdLength);

        // One override per entity. Postgres treats NULLs as distinct, so the homepage (no entity id) needs its
        // own filtered unique index to stay a singleton.
        builder.HasIndex(m => new { m.EntityType, m.EntityId }).IsUnique();
        builder.HasIndex(m => m.EntityType).IsUnique().HasFilter("entity_id IS NULL").HasDatabaseName("ix_seo_metadata_entity_type_homepage");
    }
}
