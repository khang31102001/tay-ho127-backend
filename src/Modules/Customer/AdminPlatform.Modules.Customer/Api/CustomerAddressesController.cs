using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Customer.Application.Addresses;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Customer.Api;

[ApiController]
[Route("api/v1/customers/me/addresses")]
[RequireAccountType(AccountTypes.Customer)]
public sealed class CustomerAddressesController : ControllerBase
{
    private readonly ICustomerAddressService _addressService;

    public CustomerAddressesController(ICustomerAddressService addressService)
    {
        _addressService = addressService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CustomerAddressResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerAddressResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _addressService.ListAsync(User.GetUserId(), cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<CustomerAddressResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerAddressResponse>> Create([FromBody] CreateCustomerAddressRequest request, CancellationToken cancellationToken)
    {
        var created = await _addressService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(List), created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<CustomerAddressResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerAddressResponse>> Update(Guid id, [FromBody] UpdateCustomerAddressRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _addressService.UpdateAsync(User.GetUserId(), id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _addressService.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/default")]
    [ProducesResponseType<CustomerAddressResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerAddressResponse>> SetDefault(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _addressService.SetDefaultAsync(User.GetUserId(), id, cancellationToken));
    }
}
