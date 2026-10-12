using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Media.Application.Media;
using Microsoft.AspNetCore.Authorization;
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

    /// <summary>Anonymous, read-only list of ACTIVE media for public pages (the user site renders images
    /// by media id). Paged; inactive (soft-deleted) media is never returned.</summary>
    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<PublicMediaResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PublicMediaResponse>>> ListPublic([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _mediaService.ListPublicAsync(request, cancellationToken));
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

    /// <summary>Upload file (multipart/form-data: "file" + tùy chọn "altText"). Server tự xác định loại,
    /// dung lượng và URL; chỉ nhận các đuôi file được phép (xem MediaFileRules).</summary>
    [HttpPost("upload")]
    [RequirePermission(MediaPermissions.MediaCreate)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<MediaResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MediaResponse>> Upload(IFormFile file, [FromForm] string? altText, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var publicBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var created = await _mediaService.UploadAsync(new MediaUploadFile(content, file.FileName, file.Length), altText, publicBaseUrl, cancellationToken);
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
