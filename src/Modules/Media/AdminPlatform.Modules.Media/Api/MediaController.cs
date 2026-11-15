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
    private readonly IMediaPurgeService _purgeService;

    public MediaController(IMediaService mediaService, IMediaPurgeService purgeService)
    {
        _mediaService = mediaService;
        _purgeService = purgeService;
    }

    [HttpGet]
    [RequirePermission(MediaPermissions.MediaView)]
    [ProducesResponseType<PagedResult<MediaResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MediaResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] string? kind, [FromQuery] string? status, [FromQuery] string? storage, CancellationToken cancellationToken)
    {
        return Ok(await _mediaService.ListAsync(request, kind, status, storage, cancellationToken));
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

    /// <summary>Các nơi lưu Admin chọn được khi upload (màn Thêm media hiển thị trước ô chọn file).</summary>
    [HttpGet("storage-options")]
    [RequirePermission(MediaPermissions.MediaCreate)]
    [ProducesResponseType<IReadOnlyList<MediaStorageOptionResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<MediaStorageOptionResponse>> GetStorageOptions()
    {
        return Ok(_mediaService.GetStorageOptions());
    }

    /// <summary>Upload file (multipart/form-data: "file" + tùy chọn "altText", "storage" = Local | Cloudinary,
    /// bỏ trống thì dùng nơi lưu mặc định). Server tự xác định loại,
    /// dung lượng và URL; chỉ nhận các đuôi file được phép (xem MediaFileRules).</summary>
    [HttpPost("upload")]
    [RequirePermission(MediaPermissions.MediaCreate)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<MediaResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MediaResponse>> Upload(IFormFile file, [FromForm] string? altText, [FromForm] string? storage, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var publicBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var created = await _mediaService.UploadAsync(new MediaUploadFile(content, file.FileName, file.Length), altText, storage, publicBaseUrl, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(MediaPermissions.MediaUpdate)]
    [ProducesResponseType<MediaResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MediaResponse>> Update(Guid id, [FromBody] UpdateMediaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _mediaService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Những nơi còn dùng media này (sản phẩm, bài viết, banner, section, thương hiệu, SEO, ...).</summary>
    [HttpGet("{id:guid}/usages")]
    [RequirePermission(MediaPermissions.MediaView)]
    [ProducesResponseType<IReadOnlyList<MediaUsageResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MediaUsageResponse>>> GetUsages(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _purgeService.GetUsagesAsync(id, cancellationToken));
    }

    /// <summary>Xóa vĩnh viễn tối đa 100 media: xóa file trên nơi lưu rồi xóa bản ghi. Không hoàn tác được.
    /// Media đang được dùng bị bỏ qua (status "inUse" kèm danh sách nơi dùng); kết quả trả theo từng id.</summary>
    [HttpPost("purge")]
    [RequirePermission(MediaPermissions.MediaPurge)]
    [ProducesResponseType<PurgeMediaResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PurgeMediaResponse>> Purge([FromBody] PurgeMediaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _purgeService.PurgeAsync(request, cancellationToken));
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
