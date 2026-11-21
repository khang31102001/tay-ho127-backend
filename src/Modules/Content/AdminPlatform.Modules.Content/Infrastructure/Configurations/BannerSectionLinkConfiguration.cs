using AdminPlatform.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Content.Infrastructure.Configurations;

internal sealed class BannerSectionLinkConfiguration : IEntityTypeConfiguration<BannerSectionLink>
{
    public void Configure(EntityTypeBuilder<BannerSectionLink> builder)
    {
        builder.ToTable("banner_section_links");
        builder.HasKey(l => new { l.BannerId, l.PageSectionId });

        builder.HasOne<Banner>().WithMany().HasForeignKey(l => l.BannerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PageSection>().WithMany().HasForeignKey(l => l.PageSectionId).OnDelete(DeleteBehavior.Cascade);

        // The website reads the banners of one section.
        builder.HasIndex(l => l.PageSectionId);
    }
}
