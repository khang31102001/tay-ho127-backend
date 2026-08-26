using AdminPlatform.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Organization.Infrastructure.Configurations;

internal sealed class UserBrandConfiguration : IEntityTypeConfiguration<UserBrand>
{
    public void Configure(EntityTypeBuilder<UserBrand> builder)
    {
        builder.ToTable("user_brands");
        builder.HasKey(ub => ub.Id);

        builder.Property(ub => ub.UserId).IsRequired();
        builder.Property(ub => ub.BrandId).IsRequired();
        builder.HasIndex(ub => new { ub.UserId, ub.BrandId }).IsUnique();

        builder.HasOne<Brand>()
            .WithMany()
            .HasForeignKey(ub => ub.BrandId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
