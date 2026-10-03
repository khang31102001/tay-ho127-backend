using AdminPlatform.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Content.Infrastructure.Configurations;

internal sealed class ArticleCategoryConfiguration : IEntityTypeConfiguration<ArticleCategory>
{
    public void Configure(EntityTypeBuilder<ArticleCategory> builder)
    {
        builder.ToTable("article_categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.RowVersion).IsRowVersion();

        builder.Property(c => c.Name).HasMaxLength(ArticleCategory.MaxNameLength).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(SharedKernel.Slug.MaxLength).IsRequired();
        builder.HasIndex(c => c.Slug).IsUnique();

        builder.HasOne<ArticleCategory>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => c.ParentId);
    }
}

internal sealed class ArticleTagConfiguration : IEntityTypeConfiguration<ArticleTag>
{
    public void Configure(EntityTypeBuilder<ArticleTag> builder)
    {
        builder.ToTable("article_tags");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.RowVersion).IsRowVersion();

        builder.Property(t => t.Name).HasMaxLength(ArticleTag.MaxNameLength).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(SharedKernel.Slug.MaxLength).IsRequired();
        builder.HasIndex(t => t.Slug).IsUnique();
    }
}

internal sealed class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("articles");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.Property(a => a.Title).HasMaxLength(Article.MaxTitleLength).IsRequired();
        builder.Property(a => a.Slug).HasMaxLength(SharedKernel.Slug.MaxLength).IsRequired();
        builder.HasIndex(a => a.Slug).IsUnique();
        builder.Property(a => a.Summary).HasMaxLength(Article.MaxSummaryLength).IsRequired();
        builder.Property(a => a.Content).IsRequired();
        builder.Property(a => a.FeaturedMediaId).HasMaxLength(Article.MaxMediaIdLength);
        builder.Property(a => a.AuthorName).HasMaxLength(Article.MaxAuthorLength).IsRequired();
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.HasOne<ArticleCategory>()
            .WithMany()
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => a.CategoryId);

        // The public list filters on status and orders by publish date.
        builder.HasIndex(a => new { a.Status, a.PublishedAtUtc });

        builder.HasMany(a => a.Tags)
            .WithOne()
            .HasForeignKey(link => link.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(a => a.Tags).HasField("_tags");
    }
}

internal sealed class ArticleTagLinkConfiguration : IEntityTypeConfiguration<ArticleTagLink>
{
    public void Configure(EntityTypeBuilder<ArticleTagLink> builder)
    {
        builder.ToTable("article_tag_links");
        builder.HasKey(link => new { link.ArticleId, link.TagId });

        builder.HasOne<ArticleTag>()
            .WithMany()
            .HasForeignKey(link => link.TagId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(link => link.TagId);
    }
}
