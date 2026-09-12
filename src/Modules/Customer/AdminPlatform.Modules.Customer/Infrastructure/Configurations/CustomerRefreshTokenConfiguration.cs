using AdminPlatform.Modules.Customer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CustomerEntity = AdminPlatform.Modules.Customer.Domain.Customer;

namespace AdminPlatform.Modules.Customer.Infrastructure.Configurations;

internal sealed class CustomerRefreshTokenConfiguration : IEntityTypeConfiguration<CustomerRefreshToken>
{
    public void Configure(EntityTypeBuilder<CustomerRefreshToken> builder)
    {
        builder.ToTable("customer_refresh_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.CustomerId).IsRequired();
        builder.HasIndex(t => t.CustomerId);

        builder.Property(t => t.TokenHash).HasMaxLength(256).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.DeviceInfo).HasMaxLength(512);
        builder.Property(t => t.IpAddress).HasMaxLength(64);

        builder.HasOne<CustomerEntity>()
            .WithMany()
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
