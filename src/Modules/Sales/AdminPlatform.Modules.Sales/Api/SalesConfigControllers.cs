using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Sales.Application.DeliveryMethods;
using AdminPlatform.Modules.Sales.Application.OrderOptions;
using AdminPlatform.Modules.Sales.Application.PaymentMethods;
using AdminPlatform.Modules.Sales.Application.Settings;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Sales.Api;

[ApiController]
[Route("api/v1/sales/delivery-methods")]
public sealed class DeliveryMethodsController : ControllerBase
{
    private readonly IDeliveryMethodService _service;

    public DeliveryMethodsController(IDeliveryMethodService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission(SalesPermissions.DeliveryMethodsView)]
    [ProducesResponseType<PagedResult<DeliveryMethodResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DeliveryMethodResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] bool? isActive, CancellationToken cancellationToken) =>
        Ok(await _service.ListAsync(request, isActive, cancellationToken));

    [HttpGet("{id:guid}")]
    [RequirePermission(SalesPermissions.DeliveryMethodsView)]
    [ProducesResponseType<DeliveryMethodResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DeliveryMethodResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _service.GetByIdAsync(id, cancellationToken));

    /// <summary>400 for an invalid code/type/pickup address/amount range; 409 when the code is already used.</summary>
    [HttpPost]
    [RequirePermission(SalesPermissions.DeliveryMethodsCreate)]
    [ProducesResponseType<DeliveryMethodResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DeliveryMethodResponse>> Create([FromBody] CreateDeliveryMethodRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(SalesPermissions.DeliveryMethodsUpdate)]
    [ProducesResponseType<DeliveryMethodResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DeliveryMethodResponse>> Update(Guid id, [FromBody] UpdateDeliveryMethodRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [RequirePermission(SalesPermissions.DeliveryMethodsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/sales/payment-methods")]
public sealed class PaymentMethodsController : ControllerBase
{
    private readonly IPaymentMethodService _service;

    public PaymentMethodsController(IPaymentMethodService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission(SalesPermissions.PaymentMethodsView)]
    [ProducesResponseType<PagedResult<PaymentMethodResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PaymentMethodResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] bool? isActive, CancellationToken cancellationToken) =>
        Ok(await _service.ListAsync(request, isActive, cancellationToken));

    [HttpGet("{id:guid}")]
    [RequirePermission(SalesPermissions.PaymentMethodsView)]
    [ProducesResponseType<PaymentMethodResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentMethodResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _service.GetByIdAsync(id, cancellationToken));

    /// <summary>400 for an invalid code/group/amount range; 409 when the code is already used.</summary>
    [HttpPost]
    [RequirePermission(SalesPermissions.PaymentMethodsCreate)]
    [ProducesResponseType<PaymentMethodResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PaymentMethodResponse>> Create([FromBody] CreatePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(SalesPermissions.PaymentMethodsUpdate)]
    [ProducesResponseType<PaymentMethodResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentMethodResponse>> Update(Guid id, [FromBody] UpdatePaymentMethodRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [RequirePermission(SalesPermissions.PaymentMethodsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/sales/order-option-groups")]
public sealed class OrderOptionGroupsController : ControllerBase
{
    private readonly IOrderOptionService _service;

    public OrderOptionGroupsController(IOrderOptionService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission(SalesPermissions.OrderOptionGroupsView)]
    [ProducesResponseType<PagedResult<OrderOptionGroupResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderOptionGroupResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.ListAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    [RequirePermission(SalesPermissions.OrderOptionGroupsView)]
    [ProducesResponseType<OrderOptionGroupResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderOptionGroupResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _service.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [RequirePermission(SalesPermissions.OrderOptionGroupsCreate)]
    [ProducesResponseType<OrderOptionGroupResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderOptionGroupResponse>> Create([FromBody] CreateOrderOptionGroupRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Options are replaced as a whole list; options sent with their id keep that id.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(SalesPermissions.OrderOptionGroupsUpdate)]
    [ProducesResponseType<OrderOptionGroupResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderOptionGroupResponse>> Update(Guid id, [FromBody] UpdateOrderOptionGroupRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [RequirePermission(SalesPermissions.OrderOptionGroupsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/sales/order-settings")]
public sealed class OrderSettingsController : ControllerBase
{
    private readonly IOrderSettingsService _service;

    public OrderSettingsController(IOrderSettingsService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission(SalesPermissions.OrderSettingsView)]
    [ProducesResponseType<OrderSettingsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderSettingsResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await _service.GetAsync(cancellationToken));

    /// <summary>Applies to orders placed from now on; existing order codes never change.</summary>
    [HttpPut]
    [RequirePermission(SalesPermissions.OrderSettingsUpdate)]
    [ProducesResponseType<OrderSettingsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderSettingsResponse>> Update([FromBody] UpdateOrderSettingsRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.UpdateAsync(request, cancellationToken));
}
