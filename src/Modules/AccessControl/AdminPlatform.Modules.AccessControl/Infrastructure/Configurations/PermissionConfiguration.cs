using AdminPlatform.Modules.AccessControl.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.AccessControl.Infrastructure.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions", t => t.HasCheckConstraint("ck_permissions_not_own_parent", "parent_id IS NULL OR parent_id <> id"));
        builder.HasKey(p => p.Id);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.Property(p => p.Code).HasMaxLength(150).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();

        builder.Property(p => p.IsGroup).HasDefaultValue(false);
        builder.Property(p => p.SortOrder).HasDefaultValue(0);

        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(p => p.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => new { p.ParentId, p.SortOrder });
    }
}
