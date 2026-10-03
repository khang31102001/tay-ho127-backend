using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Content.Application.Banners;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Content.Api;

[ApiController]
[Route("api/v1/content/banners")]
public sealed class BannersController : ControllerBase
{
    private readonly IBannerService _bannerService;

    public BannersController(IBannerService bannerService)
    {
        _bannerService = bannerService;
    }

    [HttpGet]
    [RequirePermission(ContentPermissions.BannersView)]
    [ProducesResponseType<PagedResult<BannerResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BannerResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] string? placement, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        return Ok(await _bannerService.ListAsync(request, placement, isActive, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(ContentPermissions.BannersView)]
    [ProducesResponseType<BannerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BannerResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _bannerService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>400 when the CTA URL is not a site path / http(s) URL or the end date is not after the start date.</summary>
    [HttpPost]
    [RequirePermission(ContentPermissions.BannersCreate)]
    [ProducesResponseType<BannerResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<BannerResponse>> Create([FromBody] CreateBannerRequest request, CancellationToken cancellationToken)
    {
        var created = await _bannerService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(ContentPermissions.BannersUpdate)]
    [ProducesResponseType<BannerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BannerResponse>> Update(Guid id, [FromBody] UpdateBannerRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _bannerService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(ContentPermissions.BannersDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _bannerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
