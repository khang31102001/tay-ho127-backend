using AdminPlatform.Modules.Notification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Notification.Infrastructure.Configurations;

internal sealed class DeliveryLogConfiguration : IEntityTypeConfiguration<DeliveryLog>
{
    public void Configure(EntityTypeBuilder<DeliveryLog> builder)
    {
        builder.ToTable("delivery_logs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Provider).HasMaxLength(DeliveryLog.MaxProviderLength).IsRequired();
        builder.Property(l => l.Channel).HasMaxLength(DeliveryLog.MaxChannelLength).IsRequired();
        builder.Property(l => l.Kind).HasMaxLength(DeliveryLog.MaxKindLength).IsRequired();
        builder.Property(l => l.Status).HasMaxLength(DeliveryLog.MaxStatusLength).IsRequired();
        builder.Property(l => l.ProviderMessageId).HasMaxLength(DeliveryLog.MaxProviderMessageIdLength);
        builder.Property(l => l.ResultCode).HasMaxLength(DeliveryLog.MaxResultCodeLength);
        builder.Property(l => l.RequestId).HasMaxLength(DeliveryLog.MaxRequestIdLength);
        builder.Property(l => l.MaskedPhone).HasMaxLength(DeliveryLog.MaxMaskedPhoneLength).IsRequired();
        builder.Property(l => l.CreatedAtUtc).IsRequired();

        builder.HasIndex(l => l.CreatedAtUtc);
        builder.HasIndex(l => l.ReferenceId);
    }
}
