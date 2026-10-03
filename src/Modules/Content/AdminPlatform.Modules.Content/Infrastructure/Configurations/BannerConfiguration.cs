using AdminPlatform.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Content.Infrastructure.Configurations;

internal sealed class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.ToTable("banners");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.RowVersion).IsRowVersion();

        builder.Property(b => b.Name).HasMaxLength(Banner.MaxNameLength).IsRequired();
        builder.Property(b => b.DesktopMediaId).HasMaxLength(Banner.MaxMediaIdLength);
        builder.Property(b => b.MobileMediaId).HasMaxLength(Banner.MaxMediaIdLength);
        builder.Property(b => b.AltText).HasMaxLength(Banner.MaxTextLength).IsRequired();
        builder.Property(b => b.Heading).HasMaxLength(Banner.MaxTextLength);
        builder.Property(b => b.Subheading).HasMaxLength(Banner.MaxTextLength);
        builder.Property(b => b.CtaLabel).HasMaxLength(Banner.MaxTextLength);
        builder.Property(b => b.CtaUrl).HasMaxLength(Banner.MaxUrlLength);
        builder.Property(b => b.Placement).HasConversion<string>().HasMaxLength(32).IsRequired();

        // The website asks for the live banners of one placement.
        builder.HasIndex(b => new { b.Placement, b.IsActive, b.DisplayOrder });
    }
}
