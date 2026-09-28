using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Customer.Application.Customers;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Customer.Api;

/// <summary>Admin management of customers. Distinct from <see cref="CustomersController"/>
/// (/customers/me, the customer's own profile, gated by account type): these routes are gated by
/// admin permissions. Customers are never hard-deleted — deactivate via PUT with isActive=false.</summary>
[ApiController]
[Route("api/v1/customers")]
public sealed class AdminCustomersController : ControllerBase
{
    private readonly ICustomerProfileService _profileService;

    public AdminCustomersController(ICustomerProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    [RequirePermission(CustomerPermissions.CustomersView)]
    [ProducesResponseType<PagedResult<CustomerProfileResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CustomerProfileResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        return Ok(await _profileService.ListAsync(request, status, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(CustomerPermissions.CustomersView)]
    [ProducesResponseType<CustomerProfileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerProfileResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _profileService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(CustomerPermissions.CustomersCreate)]
    [ProducesResponseType<CustomerProfileResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerProfileResponse>> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var created = await _profileService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(CustomerPermissions.CustomersUpdate)]
    [ProducesResponseType<CustomerProfileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerProfileResponse>> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _profileService.UpdateAsync(id, request, cancellationToken));
    }
}
