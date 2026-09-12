using AdminPlatform.Modules.Customer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CustomerEntity = AdminPlatform.Modules.Customer.Domain.Customer;

namespace AdminPlatform.Modules.Customer.Infrastructure.Configurations;

internal sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("customer_addresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.CustomerId).IsRequired();
        builder.HasIndex(a => a.CustomerId);

        builder.Property(a => a.ReceiverName).HasMaxLength(256).IsRequired();
        builder.Property(a => a.Phone).HasMaxLength(32).IsRequired();
        builder.Property(a => a.AddressLine).HasMaxLength(512).IsRequired();
        builder.Property(a => a.Ward).HasMaxLength(128);
        builder.Property(a => a.District).HasMaxLength(128);
        builder.Property(a => a.Province).HasMaxLength(128);
        builder.Property(a => a.AddressNote).HasMaxLength(1024);
        builder.Property(a => a.IsDefault).IsRequired().HasDefaultValue(false);

        builder.HasOne<CustomerEntity>()
            .WithMany()
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
