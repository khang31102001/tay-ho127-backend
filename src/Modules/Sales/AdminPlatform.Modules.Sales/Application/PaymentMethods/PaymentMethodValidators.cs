using AdminPlatform.Modules.Sales.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Sales.Application.PaymentMethods;

internal static class PaymentMethodRules
{
    /// <summary>Shape rules shared by create and update; business rules (amount range) live in the
    /// <see cref="PaymentMethod"/> entity.</summary>
    public static void Apply<T>(
        AbstractValidator<T> validator,
        Func<T, string> name,
        Func<T, string> group,
        Func<T, string?> description,
        Func<T, string?> instructions,
        Func<T, string?> iconMediaId,
        Func<T, IEnumerable<string?>> shortTexts)
    {
        validator.RuleFor(x => name(x)).NotEmpty().MaximumLength(PaymentMethod.MaxNameLength);
        validator.RuleFor(x => group(x)).Must(value => EnumWire.TryParse<PaymentMethodGroup>(value, out _))
            .WithMessage("The group must be 'cod', 'card', 'bank_transfer' or 'e_wallet'.");
        validator.RuleFor(x => description(x)).MaximumLength(PaymentMethod.MaxTextLength);
        validator.RuleFor(x => instructions(x)).MaximumLength(PaymentMethod.MaxTextLength);
        validator.RuleFor(x => iconMediaId(x)).MaximumLength(PaymentMethod.MaxShortTextLength);
        validator.RuleFor(x => shortTexts(x)).Must(values => values.All(v => v is null || v.Length <= PaymentMethod.MaxShortTextLength))
            .WithMessage($"Gateway and bank fields can have at most {PaymentMethod.MaxShortTextLength} characters.");
    }
}

public sealed class CreatePaymentMethodRequestValidator : AbstractValidator<CreatePaymentMethodRequest>
{
    public CreatePaymentMethodRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(PaymentMethod.MaxCodeLength);
        PaymentMethodRules.Apply(this, x => x.Name, x => x.Group, x => x.Description, x => x.Instructions, x => x.IconMediaId,
            x => [x.Gateway, x.BankName, x.BankAccountNumber, x.BankAccountHolder, x.BankBranch]);
    }
}

public sealed class UpdatePaymentMethodRequestValidator : AbstractValidator<UpdatePaymentMethodRequest>
{
    public UpdatePaymentMethodRequestValidator()
    {
        PaymentMethodRules.Apply(this, x => x.Name, x => x.Group, x => x.Description, x => x.Instructions, x => x.IconMediaId,
            x => [x.Gateway, x.BankName, x.BankAccountNumber, x.BankAccountHolder, x.BankBranch]);
    }
}
