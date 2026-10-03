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

    /// <summary>Active article categories and all tags.</summary>
    [HttpGet("taxonomy")]
    [ProducesResponseType<PublicTaxonomyResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicTaxonomyResponse>> GetTaxonomy(CancellationToken cancellationToken)
    {
        return Ok(await _publicContentService.GetTaxonomyAsync(cancellationToken));
    }
}
