using System.Text.Json;
using AdminPlatform.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Organization.Infrastructure.Configurations;

internal sealed class BrandProfileConfiguration : IEntityTypeConfiguration<BrandProfile>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<BrandProfile> builder)
    {
        builder.ToTable("brand_profiles");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Tagline).HasMaxLength(500).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(BrandProfile.MaxTextLength);
        builder.Property(p => p.TaxCode).HasMaxLength(50);
        builder.Property(p => p.LegalName).HasMaxLength(300);

        builder.Property(p => p.SocialLinks)
            .HasColumnType("jsonb")
            .HasConversion(
                links => JsonSerializer.Serialize(links, Json),
                text => (IReadOnlyList<SocialLink>)(JsonSerializer.Deserialize<List<SocialLink>>(text, Json) ?? new List<SocialLink>()),
                new ValueComparer<IReadOnlyList<SocialLink>>(
                    (a, b) => a!.SequenceEqual(b!),
                    links => links.Aggregate(0, (hash, link) => HashCode.Combine(hash, link)),
                    links => links.ToList()));

        builder.Property(p => p.LogoMediaId).HasMaxLength(64);
        builder.Property(p => p.LogoDarkMediaId).HasMaxLength(64);
        builder.Property(p => p.LogoLightMediaId).HasMaxLength(64);
        builder.Property(p => p.FaviconMediaId).HasMaxLength(64);
        builder.Property(p => p.OgImageMediaId).HasMaxLength(64);
    }
}
