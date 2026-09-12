using AdminPlatform.Modules.Customer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CustomerEntity = AdminPlatform.Modules.Customer.Domain.Customer;

namespace AdminPlatform.Modules.Customer.Infrastructure.Configurations;

internal sealed class CustomerAuthIdentityConfiguration : IEntityTypeConfiguration<CustomerAuthIdentity>
{
    public void Configure(EntityTypeBuilder<CustomerAuthIdentity> builder)
    {
        builder.ToTable("customer_auth_identities");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.RowVersion).IsRowVersion();

        builder.Property(i => i.CustomerId).IsRequired();

        builder.Property(i => i.Provider).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(i => i.ProviderUserId).HasMaxLength(256);
        builder.Property(i => i.Email).HasMaxLength(256).IsRequired();
        builder.Property(i => i.PasswordHash).HasMaxLength(1024);

        // One identity per provider per customer (no duplicate "Local" or "Google" rows for the same
        // customer), and a given provider account (e.g. one Google `sub`) can only ever back one customer.
        builder.HasIndex(i => new { i.CustomerId, i.Provider }).IsUnique();
        builder.HasIndex(i => new { i.Provider, i.ProviderUserId }).IsUnique().HasFilter("provider_user_id IS NOT NULL");

        builder.HasOne<CustomerEntity>()
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
