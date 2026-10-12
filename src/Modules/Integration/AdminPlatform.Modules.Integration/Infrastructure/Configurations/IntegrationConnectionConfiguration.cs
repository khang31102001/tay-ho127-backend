using AdminPlatform.Modules.Integration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Integration.Infrastructure.Configurations;

internal sealed class IntegrationConnectionConfiguration : IEntityTypeConfiguration<IntegrationConnection>
{
    public void Configure(EntityTypeBuilder<IntegrationConnection> builder)
    {
        builder.ToTable("integration_connections");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.RowVersion).IsRowVersion();

        builder.Property(c => c.ProviderCode).HasMaxLength(IntegrationConnection.MaxProviderCodeLength).IsRequired();
        builder.HasIndex(c => c.ProviderCode).IsUnique();

        builder.Property(c => c.DisplayName).HasMaxLength(IntegrationConnection.MaxDisplayNameLength).IsRequired();
        builder.Property(c => c.IsEnabled).IsRequired();
        builder.Property(c => c.Environment).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(c => c.ConfigJson).HasColumnType("jsonb").IsRequired();
    }
}
