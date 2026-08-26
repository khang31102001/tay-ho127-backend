using AdminPlatform.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Platform.Infrastructure.Configurations;

internal sealed class FiscalYearConfiguration : IEntityTypeConfiguration<FiscalYear>
{
    public void Configure(EntityTypeBuilder<FiscalYear> builder)
    {
        builder.ToTable("fiscal_years");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.RowVersion).IsRowVersion();

        builder.Property(f => f.Code).HasMaxLength(50).IsRequired();
        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(f => new { f.OrganizationId, f.Code }).IsUnique();

        builder.Property(f => f.StartDate).HasColumnType("date");
        builder.Property(f => f.EndDate).HasColumnType("date");
    }
}
