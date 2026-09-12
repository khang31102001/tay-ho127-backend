using FluentValidation;

namespace AdminPlatform.Modules.Customer.Application.Customers;

public sealed class UpdateCustomerProfileRequestValidator : AbstractValidator<UpdateCustomerProfileRequest>
{
    public UpdateCustomerProfileRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Gender).MaximumLength(16);
    }
}

public sealed class UpdateCustomerAvatarRequestValidator : AbstractValidator<UpdateCustomerAvatarRequest>
{
    public UpdateCustomerAvatarRequestValidator()
    {
        RuleFor(x => x.AvatarMediaId).MaximumLength(512);
    }
}
