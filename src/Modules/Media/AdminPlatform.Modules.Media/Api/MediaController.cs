using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Media.Application.Media;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Media.Api;

[ApiController]
[Route("api/v1/media")]
public sealed class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpGet]
    [RequirePermission(MediaPermissions.MediaView)]
    [ProducesResponseType<PagedResult<MediaResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MediaResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] string? kind, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        return Ok(await _mediaService.ListAsync(request, kind, status, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(MediaPermissions.MediaView)]
    [ProducesResponseType<MediaResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MediaResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediaService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(MediaPermissions.MediaCreate)]
    [ProducesResponseType<MediaResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MediaResponse>> Create([FromBody] CreateMediaRequest request, CancellationToken cancellationToken)
    {
        var created = await _mediaService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(MediaPermissions.MediaUpdate)]
    [ProducesResponseType<MediaResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MediaResponse>> Update(Guid id, [FromBody] UpdateMediaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _mediaService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(MediaPermissions.MediaDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediaService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
