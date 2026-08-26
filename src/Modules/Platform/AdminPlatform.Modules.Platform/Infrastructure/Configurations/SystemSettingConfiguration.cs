using AdminPlatform.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Platform.Infrastructure.Configurations;

internal sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.Property(s => s.Code).HasMaxLength(150).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Value).HasMaxLength(4000).IsRequired();
        builder.HasIndex(s => new { s.OrganizationId, s.Code }).IsUnique();
    }
}
