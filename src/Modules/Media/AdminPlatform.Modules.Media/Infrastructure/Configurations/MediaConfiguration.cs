using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MediaEntity = AdminPlatform.Modules.Media.Domain.Media;

namespace AdminPlatform.Modules.Media.Infrastructure.Configurations;

internal sealed class MediaConfiguration : IEntityTypeConfiguration<MediaEntity>
{
    public void Configure(EntityTypeBuilder<MediaEntity> builder)
    {
        builder.ToTable("media");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.RowVersion).IsRowVersion();

        builder.Property(m => m.FileName).HasMaxLength(256).IsRequired();
        builder.Property(m => m.Url).HasMaxLength(2048).IsRequired();
        builder.Property(m => m.AltText).HasMaxLength(512);
        builder.Property(m => m.Size).IsRequired();

        builder.Property(m => m.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.HasIndex(m => m.Kind);
        builder.HasIndex(m => m.Status);
    }
}
