using AdminPlatform.Modules.Seo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Seo.Infrastructure.Configurations;

internal sealed class SeoSettingsConfiguration : IEntityTypeConfiguration<SeoSettings>
{
    public void Configure(EntityTypeBuilder<SeoSettings> builder)
    {
        builder.ToTable("seo_settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.Property(s => s.DefaultTitleTemplate).HasMaxLength(SeoSettings.MaxTitleTemplateLength).IsRequired();
        builder.Property(s => s.DefaultDescription).HasMaxLength(SeoSettings.MaxDescriptionLength).IsRequired();
        builder.Property(s => s.DefaultOgImageMediaId).HasMaxLength(SeoSettings.MaxMediaIdLength);
        builder.Property(s => s.TwitterSite).HasMaxLength(SeoSettings.MaxTwitterHandleLength);
        builder.Property(s => s.TwitterCreator).HasMaxLength(SeoSettings.MaxTwitterHandleLength);

        // Postgres text[] — a short ordered list owned by this row, never queried by element.
        builder.Property(s => s.RobotsDisallowPaths).HasColumnType("text[]").IsRequired();
    }
}
