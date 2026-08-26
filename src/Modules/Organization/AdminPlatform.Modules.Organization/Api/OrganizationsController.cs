using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Organization.Application.Organizations;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Organization.Api;

[ApiController]
[Route("api/v1/organizations")]
public sealed class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    public OrganizationsController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    [HttpGet]
    [RequirePermission(OrganizationPermissions.OrganizationsView)]
    [ProducesResponseType<PagedResult<OrganizationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrganizationResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _organizationService.ListAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(OrganizationPermissions.OrganizationsView)]
    [ProducesResponseType<OrganizationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _organizationService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(OrganizationPermissions.OrganizationsCreate)]
    [ProducesResponseType<OrganizationResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrganizationResponse>> Create([FromBody] CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var created = await _organizationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(OrganizationPermissions.OrganizationsUpdate)]
    [ProducesResponseType<OrganizationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationResponse>> Update(Guid id, [FromBody] UpdateOrganizationRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _organizationService.UpdateAsync(id, request, cancellationToken));
    }
}
