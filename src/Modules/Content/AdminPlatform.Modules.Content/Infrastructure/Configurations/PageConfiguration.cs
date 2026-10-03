using AdminPlatform.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;
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
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
    }
}

internal sealed class PageSectionConfiguration : IEntityTypeConfiguration<PageSection>
{
    public void Configure(EntityTypeBuilder<PageSection> builder)
    {
        builder.ToTable("page_sections");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.Property(s => s.Kind).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(s => s.Eyebrow).HasMaxLength(PageSection.MaxTextLength);
        builder.Property(s => s.Heading).HasMaxLength(PageSection.MaxTextLength);
        builder.Property(s => s.Subheading).HasMaxLength(PageSection.MaxTextLength);
        builder.Property(s => s.Body).HasMaxLength(PageSection.MaxBodyLength);
        builder.Property(s => s.MediaId).HasMaxLength(PageSection.MaxMediaIdLength);
        builder.Property(s => s.CtaLabel).HasMaxLength(PageSection.MaxTextLength);
        builder.Property(s => s.CtaUrl).HasMaxLength(PageSection.MaxUrlLength);

        builder.HasOne<Page>()
            .WithMany()
            .HasForeignKey(s => s.PageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => new { s.PageId, s.DisplayOrder });
    }
}
