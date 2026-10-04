using AdminPlatform.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Sales.Infrastructure.Configurations;

internal sealed class OrderCodeCounterConfiguration : IEntityTypeConfiguration<OrderCodeCounter>
{
    public void Configure(EntityTypeBuilder<OrderCodeCounter> builder)
    {
        builder.ToTable("order_code_counters");
        builder.HasKey(c => c.CounterKey);
        builder.Property(c => c.CounterKey).HasMaxLength(64);
    }
}
