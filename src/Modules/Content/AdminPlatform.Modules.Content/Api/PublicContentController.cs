using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Content.Application.PublicContent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Content.Api;

/// <summary>Anonymous read-only content for the public website. Only Published articles are ever returned.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/content/public")]
public sealed class PublicContentController : ControllerBase
{
    private readonly IPublicContentService _publicContentService;

    public PublicContentController(IPublicContentService publicContentService)
    {
        _publicContentService = publicContentService;
    }

    /// <summary>Published articles, newest first (no HTML body). Page size is capped at 200.</summary>
    [HttpGet("articles")]
    [ProducesResponseType<PagedResult<PublicArticleSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PublicArticleSummaryResponse>>> ListArticles(
        [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _publicContentService.ListArticlesAsync(request, cancellationToken));
    }

    /// <summary>404 for an unknown slug or an article that is not published.</summary>
    [HttpGet("articles/{slug}")]
    [ProducesResponseType<PublicArticleResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicArticleResponse>> GetArticle(string slug, CancellationToken cancellationToken)
    {
        return Ok(await _publicContentService.GetArticleBySlugAsync(slug, cancellationToken));
    }

    /// <summary>Published pages (id, name, URL path) — used to resolve navigation links to pages.</summary>
    [HttpGet("pages")]
    [ProducesResponseType<IReadOnlyList<PublicPageResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PublicPageResponse>>> ListPages(CancellationToken cancellationToken)
    {
        return Ok(await _publicContentService.ListPublishedPagesAsync(cancellationToken));
    }

    /// <summary>Banners live right now, optionally for one placement (HOME_HERO, HOME_PROMOTION, MENU_HERO, ARTICLE_BANNER).</summary>
    [HttpGet("banners")]
    [ProducesResponseType<IReadOnlyList<PublicBannerResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PublicBannerResponse>>> ListBanners(
        [FromQuery] string? placement, CancellationToken cancellationToken)
    {
        return Ok(await _publicContentService.ListBannersAsync(placement, cancellationToken));
    }

    /// <summary>Active article categories and all tags.</summary>
    [HttpGet("taxonomy")]
    [ProducesResponseType<PublicTaxonomyResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicTaxonomyResponse>> GetTaxonomy(CancellationToken cancellationToken)
    {
        return Ok(await _publicContentService.GetTaxonomyAsync(cancellationToken));
    }
}
