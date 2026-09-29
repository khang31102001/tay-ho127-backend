using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Customer.Application.Addresses;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Customer.Api;

/// <summary>Admin management of a customer's delivery addresses — same rules as the customer's own
/// /customers/me/addresses endpoints (one default per customer), gated by admin permissions.</summary>
[ApiController]
[Route("api/v1/customers/{customerId:guid}/addresses")]
public sealed class AdminCustomerAddressesController : ControllerBase
{
    private readonly ICustomerAddressService _addressService;

    public AdminCustomerAddressesController(ICustomerAddressService addressService)
    {
        _addressService = addressService;
    }

    [HttpGet]
    [RequirePermission(CustomerPermissions.CustomersView)]
    [ProducesResponseType<IReadOnlyList<CustomerAddressResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerAddressResponse>>> List(Guid customerId, CancellationToken cancellationToken)
    {
        return Ok(await _addressService.ListAsync(customerId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(CustomerPermissions.CustomersView)]
    [ProducesResponseType<CustomerAddressResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerAddressResponse>> GetById(Guid customerId, Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _addressService.GetAsync(customerId, id, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(CustomerPermissions.CustomersManageAddresses)]
    [ProducesResponseType<CustomerAddressResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerAddressResponse>> Create(
        Guid customerId, [FromBody] CreateCustomerAddressRequest request, CancellationToken cancellationToken)
    {
        var created = await _addressService.CreateAsync(customerId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { customerId, id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(CustomerPermissions.CustomersManageAddresses)]
    [ProducesResponseType<CustomerAddressResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerAddressResponse>> Update(
        Guid customerId, Guid id, [FromBody] UpdateCustomerAddressRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _addressService.UpdateAsync(customerId, id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(CustomerPermissions.CustomersManageAddresses)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid customerId, Guid id, CancellationToken cancellationToken)
    {
        await _addressService.DeleteAsync(customerId, id, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/default")]
    [RequirePermission(CustomerPermissions.CustomersManageAddresses)]
    [ProducesResponseType<CustomerAddressResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerAddressResponse>> SetDefault(Guid customerId, Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _addressService.SetDefaultAsync(customerId, id, cancellationToken));
    }
}
