using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrganizationEntity = AdminPlatform.Modules.Organization.Domain.Organization;

namespace AdminPlatform.Modules.Organization.Infrastructure.Configurations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<OrganizationEntity>
{
    public void Configure(EntityTypeBuilder<OrganizationEntity> builder)
    {
        builder.ToTable("organizations");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.Property(o => o.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(o => o.Code).IsUnique();
        builder.Property(o => o.Name).HasMaxLength(200).IsRequired();
    }
}
