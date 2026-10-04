using AdminPlatform.Modules.Navigation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Navigation.Infrastructure.Configurations;

internal sealed class NavigationItemPermissionConfiguration : IEntityTypeConfiguration<NavigationItemPermission>
{
    public void Configure(EntityTypeBuilder<NavigationItemPermission> builder)
    {
        builder.ToTable("navigation_item_permissions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ItemId).IsRequired();
        builder.Property(p => p.PermissionCode).HasMaxLength(150).IsRequired();
        builder.HasIndex(p => new { p.ItemId, p.PermissionCode }).IsUnique();

        builder.HasOne<NavigationItem>()
            .WithMany()
            .HasForeignKey(p => p.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
