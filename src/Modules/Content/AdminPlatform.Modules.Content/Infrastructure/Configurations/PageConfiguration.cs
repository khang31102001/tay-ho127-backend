using System.Text.Json;
using AdminPlatform.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Content.Infrastructure.Configurations;

internal sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("pages");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.Property(p => p.Name).HasMaxLength(Page.MaxNameLength).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(Page.MaxPathLength).IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.PageKey).HasMaxLength(ContentKey.MaxLength);
        builder.HasIndex(p => p.PageKey).IsUnique().HasFilter("page_key IS NOT NULL");
        builder.Property(p => p.SortOrder).HasDefaultValue(0);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
    }
}

internal sealed class PageSectionConfiguration : IEntityTypeConfiguration<PageSection>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<PageSection> builder)
    {
        builder.ToTable("page_sections");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.Property(s => s.SectionKey).HasMaxLength(PageSection.MaxKeyLength);
        builder.Property(s => s.Kind).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(s => s.Eyebrow).HasMaxLength(PageSection.MaxTextLength);
        builder.Property(s => s.Heading).HasMaxLength(PageSection.MaxTextLength);
        builder.Property(s => s.Subheading).HasMaxLength(PageSection.MaxTextLength);
        builder.Property(s => s.Body).HasMaxLength(PageSection.MaxBodyLength);
        builder.Property(s => s.MediaId).HasMaxLength(PageSection.MaxMediaIdLength);
        builder.Property(s => s.CtaLabel).HasMaxLength(PageSection.MaxTextLength);
        builder.Property(s => s.CtaUrl).HasMaxLength(PageSection.MaxUrlLength);
        builder.Property(s => s.Settings)
            .HasColumnType("jsonb")
            .HasConversion(
                settings => JsonSerializer.Serialize(settings, Json),
                text => JsonSerializer.Deserialize<PageSectionSettings>(text, Json),
                new ValueComparer<PageSectionSettings?>(
                    (a, b) => JsonSerializer.Serialize(a, Json) == JsonSerializer.Serialize(b, Json),
                    settings => JsonSerializer.Serialize(settings, Json).GetHashCode(),
                    // Settings is immutable (replaced, never mutated in place), so the instance itself is a safe snapshot.
                    settings => settings));

        builder.HasOne<Page>()
            .WithMany()
            .HasForeignKey(s => s.PageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => new { s.PageId, s.DisplayOrder });
        builder.HasIndex(s => new { s.PageId, s.SectionKey }).IsUnique().HasFilter("section_key IS NOT NULL");
    }
}
