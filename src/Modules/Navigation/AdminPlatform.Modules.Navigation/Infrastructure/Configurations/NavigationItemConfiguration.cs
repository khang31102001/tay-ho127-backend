using AdminPlatform.Modules.Navigation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Navigation.Infrastructure.Configurations;

internal sealed class NavigationItemConfiguration : IEntityTypeConfiguration<NavigationItem>
{
    public void Configure(EntityTypeBuilder<NavigationItem> builder)
    {
        builder.ToTable("navigation_items", t =>
        {
            t.HasCheckConstraint("ck_navigation_items_group_has_no_url", "is_group = false OR url IS NULL");
            t.HasCheckConstraint("ck_navigation_items_not_own_parent", "parent_id IS NULL OR parent_id <> id");
        });
        builder.HasKey(i => i.Id);
        builder.Property(i => i.RowVersion).IsRowVersion();

        builder.Property(i => i.Code).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.IsGroup).HasDefaultValue(false);
        builder.Property(i => i.Url).HasMaxLength(500);
        builder.Property(i => i.Icon).HasMaxLength(100);

        builder.HasIndex(i => new { i.MenuId, i.Code }).IsUnique();
        builder.HasIndex(i => new { i.MenuId, i.ParentId, i.SortOrder });

        builder.HasOne<NavigationMenu>()
            .WithMany()
            .HasForeignKey(i => i.MenuId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<NavigationItem>()
            .WithMany()
            .HasForeignKey(i => i.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
