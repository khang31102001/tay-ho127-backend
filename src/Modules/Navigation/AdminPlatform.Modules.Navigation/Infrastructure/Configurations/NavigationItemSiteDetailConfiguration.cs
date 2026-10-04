using AdminPlatform.Modules.Navigation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Navigation.Infrastructure.Configurations;

internal sealed class NavigationItemSiteDetailConfiguration : IEntityTypeConfiguration<NavigationItemSiteDetail>
{
    public void Configure(EntityTypeBuilder<NavigationItemSiteDetail> builder)
    {
        builder.ToTable("navigation_item_site_details");
        builder.HasKey(d => d.ItemId);

        builder.Property(d => d.TargetType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.OpenInNewTab).HasDefaultValue(false);

        builder.HasOne<NavigationItem>()
            .WithOne()
            .HasForeignKey<NavigationItemSiteDetail>(d => d.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
