using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Catalog.Application.Promotions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AdminPlatform.Modules.Catalog.Api;

[ApiController]
[Route("api/v1/catalog/promotions")]
public sealed class PromotionsController : ControllerBase
{
    /// <summary>Name of the rate-limiter policy registered by the Host for the anonymous validate endpoint.</summary>
    public const string ValidateRateLimitPolicy = "promotion-validate";

    private readonly IPromotionService _promotionService;
    private readonly IPromotionValidationService _validationService;

    public PromotionsController(IPromotionService promotionService, IPromotionValidationService validationService)
    {
        _promotionService = promotionService;
        _validationService = validationService;
    }

    [HttpGet]
    [RequirePermission(CatalogPermissions.PromotionsView)]
    [ProducesResponseType<PagedResult<PromotionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PromotionResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        return Ok(await _promotionService.ListAsync(request, status, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(CatalogPermissions.PromotionsView)]
    [ProducesResponseType<PromotionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PromotionResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _promotionService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>409 when the code is already used; 400 when a product-discount has no product/category.</summary>
    [HttpPost]
    [RequirePermission(CatalogPermissions.PromotionsCreate)]
    [ProducesResponseType<PromotionResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PromotionResponse>> Create([FromBody] CreatePromotionRequest request, CancellationToken cancellationToken)
    {
        var created = await _promotionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(CatalogPermissions.PromotionsUpdate)]
    [ProducesResponseType<PromotionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PromotionResponse>> Update(Guid id, [FromBody] UpdatePromotionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _promotionService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Also removes the promotion's product/category scope.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(CatalogPermissions.PromotionsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _promotionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Anonymous (guest checkout): checks a code against the cart and returns the computed discount.
    /// An unusable code is 200 with isValid=false. Rate-limited per client to make code guessing impractical.</summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    [EnableRateLimiting(ValidateRateLimitPolicy)]
    [ProducesResponseType<ValidatePromotionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ValidatePromotionResponse>> Validate([FromBody] ValidatePromotionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _validationService.ValidateAsync(request, cancellationToken));
    }
}
