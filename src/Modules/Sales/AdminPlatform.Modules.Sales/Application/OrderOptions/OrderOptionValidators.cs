using AdminPlatform.Modules.Sales.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Sales.Application.OrderOptions;

public sealed class OrderOptionValueRequestValidator : AbstractValidator<OrderOptionValueRequest>
{
    public OrderOptionValueRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}

public sealed class CreateOrderOptionGroupRequestValidator : AbstractValidator<CreateOrderOptionGroupRequest>
{
    public CreateOrderOptionGroupRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(OrderOptionGroup.MaxNameLength);
        RuleFor(x => x.SelectionType).Must(value => EnumWire.TryParse<OptionSelectionType>(value, out _))
            .WithMessage("The selection type must be 'single' or 'multiple'.");
        RuleFor(x => x.Options).NotEmpty().Must(options => options.Count <= OrderOptionGroup.MaxValueCount)
            .WithMessage($"A group can have at most {OrderOptionGroup.MaxValueCount} options.");
        RuleForEach(x => x.Options).SetValidator(new OrderOptionValueRequestValidator());
    }
}

public sealed class UpdateOrderOptionGroupRequestValidator : AbstractValidator<UpdateOrderOptionGroupRequest>
{
    public UpdateOrderOptionGroupRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(OrderOptionGroup.MaxNameLength);
        RuleFor(x => x.SelectionType).Must(value => EnumWire.TryParse<OptionSelectionType>(value, out _))
            .WithMessage("The selection type must be 'single' or 'multiple'.");
        RuleFor(x => x.Options).NotEmpty().Must(options => options.Count <= OrderOptionGroup.MaxValueCount)
            .WithMessage($"A group can have at most {OrderOptionGroup.MaxValueCount} options.");
        RuleForEach(x => x.Options).SetValidator(new OrderOptionValueRequestValidator());
    }
}
