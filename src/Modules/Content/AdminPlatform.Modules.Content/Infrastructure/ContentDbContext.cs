using AdminPlatform.Modules.Content.Application;
using AdminPlatform.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Content.Infrastructure;

public sealed class ContentDbContext : DbContext, IContentDbContext
{
    public const string Schema = "content";

    public DbSet<ArticleCategory> ArticleCategories => Set<ArticleCategory>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();
    public DbSet<Article> Articles => Set<Article>();

    public ContentDbContext(DbContextOptions<ContentDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContentDbContext).Assembly);
    }
}
