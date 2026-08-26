using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Platform.Application.FiscalYears;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Platform.Api;

[ApiController]
[Route("api/v1/fiscal-years")]
public sealed class FiscalYearsController : ControllerBase
{
    private readonly IFiscalYearService _fiscalYearService;

    public FiscalYearsController(IFiscalYearService fiscalYearService)
    {
        _fiscalYearService = fiscalYearService;
    }

    [HttpGet]
    [RequirePermission(PlatformPermissions.FiscalYearsView)]
    [ProducesResponseType<PagedResult<FiscalYearResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FiscalYearResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] Guid? organizationId, CancellationToken cancellationToken)
    {
        return Ok(await _fiscalYearService.ListAsync(request, organizationId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PlatformPermissions.FiscalYearsView)]
    [ProducesResponseType<FiscalYearResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FiscalYearResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _fiscalYearService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(PlatformPermissions.FiscalYearsCreate)]
    [ProducesResponseType<FiscalYearResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<FiscalYearResponse>> Create([FromBody] CreateFiscalYearRequest request, CancellationToken cancellationToken)
    {
        var created = await _fiscalYearService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PlatformPermissions.FiscalYearsUpdate)]
    [ProducesResponseType<FiscalYearResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FiscalYearResponse>> Update(Guid id, [FromBody] UpdateFiscalYearRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _fiscalYearService.UpdateAsync(id, request, cancellationToken));
    }
}
