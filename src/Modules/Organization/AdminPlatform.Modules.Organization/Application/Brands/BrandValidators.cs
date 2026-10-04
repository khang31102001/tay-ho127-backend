using AdminPlatform.Modules.Organization.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Organization.Application.Brands;

public sealed class CreateBrandRequestValidator : AbstractValidator<CreateBrandRequest>
{
    public CreateBrandRequestValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class BrandContactDtoValidator : AbstractValidator<BrandContactDto>
{
    public BrandContactDtoValidator()
    {
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Hotline).MaximumLength(50);
        RuleFor(x => x.Email).MaximumLength(256);
        RuleFor(x => x.AddressLine).MaximumLength(Brand.MaxTextLength);
        RuleFor(x => x.Ward).MaximumLength(200);
        RuleFor(x => x.District).MaximumLength(200);
        RuleFor(x => x.Province).MaximumLength(200);
        RuleFor(x => x.OpenTime).MaximumLength(5);
        RuleFor(x => x.CloseTime).MaximumLength(5);
        RuleFor(x => x.BusinessHoursNote).MaximumLength(Brand.MaxTextLength);
    }
}

public sealed class UpdateBrandRequestValidator : AbstractValidator<UpdateBrandRequest>
{
    public UpdateBrandRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Contact!).SetValidator(new BrandContactDtoValidator()).When(x => x.Contact is not null);
    }
}
