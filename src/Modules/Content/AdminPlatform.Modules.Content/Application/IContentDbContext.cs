using AdminPlatform.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Content.Application;

/// <summary>Persistence port for the Content module — Application depends on this, not on EF Core directly.
/// Implemented by ContentDbContext (Infrastructure).</summary>
public interface IContentDbContext
{
    DbSet<ArticleCategory> ArticleCategories { get; }
    DbSet<ArticleTag> ArticleTags { get; }
    DbSet<Article> Articles { get; }
    DbSet<Banner> Banners { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
