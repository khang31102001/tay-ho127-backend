using AdminPlatform.Modules.Sales.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Sales.Application.DeliveryMethods;

internal static class DeliveryMethodRules
{
    /// <summary>Shape rules shared by create and update; business rules (pickup address, ranges) live in the
    /// <see cref="DeliveryMethod"/> entity.</summary>
    public static void Apply<T>(
        AbstractValidator<T> validator,
        Func<T, string> name,
        Func<T, string?> description,
        Func<T, string> type,
        Func<T, decimal> baseFee,
        Func<T, string?> pickupAddress)
    {
        validator.RuleFor(x => name(x)).NotEmpty().MaximumLength(DeliveryMethod.MaxNameLength);
        validator.RuleFor(x => description(x)).MaximumLength(DeliveryMethod.MaxTextLength);
        validator.RuleFor(x => type(x)).Must(value => EnumWire.TryParse<DeliveryMethodType>(value, out _))
            .WithMessage("The type must be 'delivery' or 'pickup'.");
        validator.RuleFor(x => baseFee(x)).GreaterThanOrEqualTo(0);
        validator.RuleFor(x => pickupAddress(x)).MaximumLength(DeliveryMethod.MaxTextLength);
    }
}

public sealed class CreateDeliveryMethodRequestValidator : AbstractValidator<CreateDeliveryMethodRequest>
{
    public CreateDeliveryMethodRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(DeliveryMethod.MaxCodeLength);
        DeliveryMethodRules.Apply(this, x => x.Name, x => x.Description, x => x.Type, x => x.BaseFee, x => x.PickupAddress);
    }
}

public sealed class UpdateDeliveryMethodRequestValidator : AbstractValidator<UpdateDeliveryMethodRequest>
{
    public UpdateDeliveryMethodRequestValidator()
    {
        DeliveryMethodRules.Apply(this, x => x.Name, x => x.Description, x => x.Type, x => x.BaseFee, x => x.PickupAddress);
    }
}
