using AdminPlatform.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Platform.Infrastructure.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
        builder.Property(a => a.EntityName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.ChangesJson).HasColumnType("jsonb");
        builder.Property(a => a.CorrelationId).HasMaxLength(100);

        builder.HasIndex(a => a.AtUtc);
        builder.HasIndex(a => a.ActorUserId);
        builder.HasIndex(a => a.EntityName);
    }
}
