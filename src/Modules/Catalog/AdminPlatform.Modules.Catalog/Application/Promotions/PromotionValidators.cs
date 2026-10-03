using AdminPlatform.Modules.Catalog.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Catalog.Application.Promotions;

internal static class PromotionRules
{
    /// <summary>Format checks shared by the create and update requests; the value-range rules depend on
    /// the type and live in the <see cref="Promotion"/> domain entity.</summary>
    public static void Apply<T>(
        AbstractValidator<T> validator,
        Func<T, string> code,
        Func<T, string> name,
        Func<T, string?> description,
        Func<T, string> type,
        Func<T, string> status)
    {
        validator.RuleFor(x => code(x)).NotEmpty().MaximumLength(Promotion.MaxCodeLength).Must(Promotion.IsValidCode)
            .WithMessage("Code must contain only letters, digits, '-' and '_'.");
        validator.RuleFor(x => name(x)).NotEmpty().MaximumLength(Promotion.MaxNameLength);
        validator.RuleFor(x => description(x)).MaximumLength(Promotion.MaxDescriptionLength);
        validator.RuleFor(x => type(x)).Must(value => PromotionWireFormat.TryParseType(value, out _))
            .WithMessage("Type must be percentage, fixed_amount, free_shipping or product_discount.");
        validator.RuleFor(x => status(x)).Must(value => PromotionWireFormat.TryParseStatus(value, out _))
            .WithMessage("Status must be draft, active or inactive.");
    }
}

public sealed class CreatePromotionRequestValidator : AbstractValidator<CreatePromotionRequest>
{
    public CreatePromotionRequestValidator()
    {
        PromotionRules.Apply(this, x => x.Code, x => x.Name, x => x.Description, x => x.Type, x => x.Status);
    }
}

public sealed class UpdatePromotionRequestValidator : AbstractValidator<UpdatePromotionRequest>
{
    public UpdatePromotionRequestValidator()
    {
        PromotionRules.Apply(this, x => x.Code, x => x.Name, x => x.Description, x => x.Type, x => x.Status);
    }
}

public sealed class ValidatePromotionRequestValidator : AbstractValidator<ValidatePromotionRequest>
{
    /// <summary>The endpoint is anonymous, so cap what one call can make the server do.</summary>
    public const int MaxItems = 100;

    public ValidatePromotionRequestValidator()
    {
        RuleFor(x => x.Code).MaximumLength(Promotion.MaxCodeLength);
        RuleFor(x => x.Subtotal).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ShippingFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Items).Must(items => items is null || items.Count <= MaxItems)
            .WithMessage($"A cart can have at most {MaxItems} lines.");
        RuleForEach(x => x.Items).ChildRules(item => item.RuleFor(i => i.LineTotal).GreaterThanOrEqualTo(0));
    }
}
