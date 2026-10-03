using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Seo.Application.Redirects;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Seo.Api;

[ApiController]
[Route("api/v1/seo/redirects")]
public sealed class RedirectsController : ControllerBase
{
    private readonly IRedirectService _redirectService;

    public RedirectsController(IRedirectService redirectService)
    {
        _redirectService = redirectService;
    }

    [HttpGet]
    [RequirePermission(SeoPermissions.RedirectsView)]
    [ProducesResponseType<PagedResult<RedirectResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RedirectResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        return Ok(await _redirectService.ListAsync(request, isActive, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(SeoPermissions.RedirectsView)]
    [ProducesResponseType<RedirectResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RedirectResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _redirectService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>400 for a bad source path (root, query, /admin, /api...), an unsafe destination, a redirect to itself or a
    /// loop; 409 when the source path is already used.</summary>
    [HttpPost]
    [RequirePermission(SeoPermissions.RedirectsCreate)]
    [ProducesResponseType<RedirectResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RedirectResponse>> Create([FromBody] CreateRedirectRequest request, CancellationToken cancellationToken)
    {
        var created = await _redirectService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(SeoPermissions.RedirectsUpdate)]
    [ProducesResponseType<RedirectResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RedirectResponse>> Update(Guid id, [FromBody] UpdateRedirectRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _redirectService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(SeoPermissions.RedirectsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _redirectService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
