using AdminPlatform.SharedKernel;
using FluentValidation;

namespace AdminPlatform.Modules.Customer.Application.Auth;

public sealed class RegisterCustomerRequestValidator : AbstractValidator<RegisterCustomerRequest>
{
    public RegisterCustomerRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(256);
        RuleFor(x => x.OtpCode).MaximumLength(16);
    }
}

public sealed class RequestRegistrationOtpRequestValidator : AbstractValidator<RequestRegistrationOtpRequest>
{
    public RequestRegistrationOtpRequestValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(32)
            .Must(phone => VietnamesePhone.TryNormalize(phone, out _))
            .WithMessage("Số điện thoại không đúng định dạng.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public sealed class CustomerLoginRequestValidator : AbstractValidator<CustomerLoginRequest>
{
    public CustomerLoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
    }
}

public sealed class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
    }
}

public sealed class CustomerRefreshTokenRequestValidator : AbstractValidator<CustomerRefreshTokenRequest>
{
    public CustomerRefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
