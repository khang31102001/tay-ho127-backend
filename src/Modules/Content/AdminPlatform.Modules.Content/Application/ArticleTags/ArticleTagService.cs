using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Content.Application.ArticleTags;

public sealed class ArticleTagService : IArticleTagService
{
    private readonly IContentDbContext _db;

    public ArticleTagService(IContentDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ArticleTagResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken)
    {
        var query = _db.ArticleTags.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(t => EF.Functions.ILike(t.Name, pattern) || EF.Functions.ILike(t.Slug, pattern));
        }

        query = request.IsDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name);

        var projected = query.Select(t => new ArticleTagResponse(t.Id, t.Name, t.Slug, t.CreatedAtUtc, t.UpdatedAtUtc));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<ArticleTagResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await FindOrThrowAsync(id, cancellationToken));
    }

    public async Task<ArticleTagResponse> CreateAsync(CreateArticleTagRequest request, CancellationToken cancellationToken)
    {
        var slug = await SlugResolver.ResolveAsync(request.Slug, request.Name, "tag", _db.ArticleTags.Select(t => t.Slug), cancellationToken);

        var tag = ArticleTag.Create(request.Name, slug);
        _db.ArticleTags.Add(tag);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(tag);
    }

    public async Task<ArticleTagResponse> UpdateAsync(Guid id, UpdateArticleTagRequest request, CancellationToken cancellationToken)
    {
        var tag = await FindOrThrowAsync(id, cancellationToken);

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? tag.Slug
            : await SlugResolver.ResolveAsync(request.Slug, request.Name, "tag",
                _db.ArticleTags.Where(t => t.Id != id).Select(t => t.Slug), cancellationToken);

        tag.Update(request.Name, slug);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(tag);
    }

    /// <summary>Hard delete; the tag is simply removed from every article that carried it (cascade).</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var tag = await FindOrThrowAsync(id, cancellationToken);
        _db.ArticleTags.Remove(tag);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ArticleTag> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.ArticleTags.SingleOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ArticleTag), id);

    private static ArticleTagResponse ToResponse(ArticleTag tag) =>
        new(tag.Id, tag.Name, tag.Slug, tag.CreatedAtUtc, tag.UpdatedAtUtc);
}
