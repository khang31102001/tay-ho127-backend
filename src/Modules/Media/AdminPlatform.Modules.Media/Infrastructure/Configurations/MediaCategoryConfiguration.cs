using AdminPlatform.Modules.Media.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Media.Infrastructure.Configurations;

internal sealed class MediaCategoryConfiguration : IEntityTypeConfiguration<MediaCategory>
{
    public void Configure(EntityTypeBuilder<MediaCategory> builder)
    {
        builder.ToTable("media_categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.RowVersion).IsRowVersion();

        builder.Property(c => c.Code).HasMaxLength(MediaCategory.MaxCodeLength).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(MediaCategory.MaxNameLength).IsRequired();
        builder.Property(c => c.SortOrder).IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();
    }
}
