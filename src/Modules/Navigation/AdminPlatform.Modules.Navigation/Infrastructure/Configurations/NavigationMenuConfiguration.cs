using AdminPlatform.Modules.Navigation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Navigation.Infrastructure.Configurations;

internal sealed class NavigationMenuConfiguration : IEntityTypeConfiguration<NavigationMenu>
{
    public void Configure(EntityTypeBuilder<NavigationMenu> builder)
    {
        builder.ToTable("navigation_menus", t => t.HasCheckConstraint(
            "ck_navigation_menus_placement",
            "(scope = 'Admin' AND location = 'Sidebar') OR (scope = 'Site' AND location IN ('Header', 'Footer', 'Mobile'))"));
        builder.HasKey(m => m.Id);
        builder.Property(m => m.RowVersion).IsRowVersion();

        builder.Property(m => m.Code).HasMaxLength(100).IsRequired();
        builder.HasIndex(m => m.Code).IsUnique();
        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Scope).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.Location).HasConversion<string>().HasMaxLength(30).IsRequired();

        // One menu per place: a single admin sidebar and a single header / footer / mobile menu.
        builder.HasIndex(m => new { m.Scope, m.Location }).IsUnique();
    }
}
