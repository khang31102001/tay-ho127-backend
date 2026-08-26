using AdminPlatform.Modules.Navigation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Navigation.Infrastructure.Configurations;

internal sealed class MenuPermissionConfiguration : IEntityTypeConfiguration<MenuPermission>
{
    public void Configure(EntityTypeBuilder<MenuPermission> builder)
    {
        builder.ToTable("menu_permissions");
        builder.HasKey(mp => mp.Id);

        builder.Property(mp => mp.MenuId).IsRequired();
        builder.Property(mp => mp.PermissionCode).HasMaxLength(150).IsRequired();
        builder.HasIndex(mp => new { mp.MenuId, mp.PermissionCode }).IsUnique();

        builder.HasOne<Menu>()
            .WithMany()
            .HasForeignKey(mp => mp.MenuId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
