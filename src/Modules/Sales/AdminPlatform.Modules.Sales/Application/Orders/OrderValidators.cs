using AdminPlatform.Modules.Sales.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Sales.Application.Orders;

public sealed class SelectedOptionRequestValidator : AbstractValidator<SelectedOptionRequest>
{
    public SelectedOptionRequestValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.OptionId).NotEmpty();
    }
}

public sealed class OrderItemRequestValidator : AbstractValidator<OrderItemRequest>
{
    public OrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, Order.MaxQuantity);
        RuleFor(x => x.Note).MaximumLength(Order.MaxNoteLength);
        RuleForEach(x => x.Modifiers).SetValidator(new SelectedOptionRequestValidator());
    }
}

/// <summary>Shape and size checks. Everything that depends on data (prices, methods, codes) is decided later
/// by the pricer; the phone format is checked here so a typo is a normal 400 at the boundary.</summary>
public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(Order.MaxNameLength);
        RuleFor(x => x.Phone).NotEmpty().Must(value => PhoneNumber.TryNormalize(value, out _))
            .WithMessage("Số điện thoại không đúng định dạng.");
        RuleFor(x => x.Email).MaximumLength(Order.MaxEmailLength).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.DeliveryAddress).MaximumLength(Order.MaxAddressLength);
        RuleFor(x => x.DeliveryMethodCode).NotEmpty().MaximumLength(DeliveryMethod.MaxCodeLength);
        RuleFor(x => x.PaymentMethodCode).NotEmpty().MaximumLength(PaymentMethod.MaxCodeLength);
        RuleFor(x => x.Items).NotEmpty().Must(items => items.Count <= Order.MaxItemCount)
            .WithMessage($"An order can have at most {Order.MaxItemCount} items.");
        RuleForEach(x => x.Items).SetValidator(new OrderItemRequestValidator());
        RuleForEach(x => x.OrderOptions).SetValidator(new SelectedOptionRequestValidator());
        RuleFor(x => x.Note).MaximumLength(Order.MaxNoteLength);
        RuleFor(x => x.DiscountCode).MaximumLength(64);
        RuleFor(x => x.IdempotencyKey).MaximumLength(100);
    }
}

public sealed class OrderLookupRequestValidator : AbstractValidator<OrderLookupRequest>
{
    public OrderLookupRequestValidator()
    {
        RuleFor(x => x.OrderCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(32);
    }
}

public sealed class ChangeOrderStatusRequestValidator : AbstractValidator<ChangeOrderStatusRequest>
{
    public ChangeOrderStatusRequestValidator()
    {
        RuleFor(x => x.ToStatus).Must(value => EnumWire.TryParse<OrderStatus>(value, out _))
            .WithMessage("The status is not a valid order status.");
        RuleFor(x => x.Note).MaximumLength(Order.MaxNoteLength);
    }
}
