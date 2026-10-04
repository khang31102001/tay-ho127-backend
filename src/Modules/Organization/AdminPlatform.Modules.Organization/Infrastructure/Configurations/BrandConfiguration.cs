using AdminPlatform.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrganizationEntity = AdminPlatform.Modules.Organization.Domain.Organization;

namespace AdminPlatform.Modules.Organization.Infrastructure.Configurations;

internal sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.RowVersion).IsRowVersion();

        builder.Property(b => b.Code).HasMaxLength(50).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Phone).HasMaxLength(50);
        builder.Property(b => b.Hotline).HasMaxLength(50);
        builder.Property(b => b.Email).HasMaxLength(256);
        builder.Property(b => b.AddressLine).HasMaxLength(Brand.MaxTextLength);
        builder.Property(b => b.Ward).HasMaxLength(200);
        builder.Property(b => b.District).HasMaxLength(200);
        builder.Property(b => b.Province).HasMaxLength(200);
        builder.Property(b => b.OpenTime).HasMaxLength(5);
        builder.Property(b => b.CloseTime).HasMaxLength(5);
        builder.Property(b => b.BusinessHoursNote).HasMaxLength(Brand.MaxTextLength);
        builder.HasIndex(b => new { b.OrganizationId, b.Code }).IsUnique();

        // At most one primary branch for the whole website (the service also clears the old one first).
        builder.HasIndex(b => b.IsPrimary).IsUnique().HasFilter("is_primary");

        builder.HasOne<OrganizationEntity>()
            .WithMany()
            .HasForeignKey(b => b.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
