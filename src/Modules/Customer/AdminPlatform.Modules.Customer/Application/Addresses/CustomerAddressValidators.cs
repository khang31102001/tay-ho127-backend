using FluentValidation;

namespace AdminPlatform.Modules.Customer.Application.Addresses;

public sealed class CreateCustomerAddressRequestValidator : AbstractValidator<CreateCustomerAddressRequest>
{
    public CreateCustomerAddressRequestValidator()
    {
        RuleFor(x => x.ReceiverName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(32);
        RuleFor(x => x.AddressLine).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Ward).MaximumLength(128);
        RuleFor(x => x.District).MaximumLength(128);
        RuleFor(x => x.Province).MaximumLength(128);
        RuleFor(x => x.AddressNote).MaximumLength(1024);
    }
}

public sealed class UpdateCustomerAddressRequestValidator : AbstractValidator<UpdateCustomerAddressRequest>
{
    public UpdateCustomerAddressRequestValidator()
    {
        RuleFor(x => x.ReceiverName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(32);
        RuleFor(x => x.AddressLine).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Ward).MaximumLength(128);
        RuleFor(x => x.District).MaximumLength(128);
        RuleFor(x => x.Province).MaximumLength(128);
        RuleFor(x => x.AddressNote).MaximumLength(1024);
    }
}
