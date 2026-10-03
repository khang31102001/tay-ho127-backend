using AdminPlatform.Modules.Seo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Seo.Infrastructure.Configurations;

internal sealed class RedirectConfiguration : IEntityTypeConfiguration<Redirect>
{
    public void Configure(EntityTypeBuilder<Redirect> builder)
    {
        builder.ToTable("redirects");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.Property(r => r.SourcePath).HasMaxLength(Redirect.MaxPathLength).IsRequired();
        builder.Property(r => r.DestinationUrl).HasMaxLength(Redirect.MaxPathLength).IsRequired();

        // Stored as the HTTP status code itself (301 / 302).
        builder.Property(r => r.RedirectType).HasConversion<int>().IsRequired();

        // The middleware matches the exact path, and one path can only redirect to one place.
        builder.HasIndex(r => r.SourcePath).IsUnique();
    }
}
