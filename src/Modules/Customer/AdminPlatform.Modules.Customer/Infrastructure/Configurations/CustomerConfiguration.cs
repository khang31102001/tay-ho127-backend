using AdminPlatform.Modules.Customer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CustomerEntity = AdminPlatform.Modules.Customer.Domain.Customer;

namespace AdminPlatform.Modules.Customer.Infrastructure.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.RowVersion).IsRowVersion();

        builder.Property(c => c.CustomerCode).HasMaxLength(32).IsRequired();
        builder.HasIndex(c => c.CustomerCode).IsUnique();

        builder.Property(c => c.FullName).HasMaxLength(256).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(32);
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.AvatarMediaId).HasMaxLength(512);

        builder.Property(c => c.Gender).HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

        // Duplicate-registration checks (§3/§10) require phone/email to be unique across customers.
        // Nullable columns, so a partial unique index (only enforced when the value is present) —
        // several customers may legitimately have no phone/email on file.
        builder.HasIndex(c => c.Phone).IsUnique().HasFilter("phone IS NOT NULL");
        builder.HasIndex(c => c.Email).IsUnique().HasFilter("email IS NOT NULL");
    }
}
