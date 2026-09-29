using AdminPlatform.Modules.Catalog.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Catalog.Application.SalesMenus;

public sealed class CreateSalesMenuRequestValidator : AbstractValidator<CreateSalesMenuRequest>
{
    public CreateSalesMenuRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Must(SalesMenu.IsValidCode)
            .WithMessage($"Code must be at most {SalesMenu.MaxCodeLength} lowercase letters, digits and single hyphens.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateSalesMenuRequestValidator : AbstractValidator<UpdateSalesMenuRequest>
{
    public UpdateSalesMenuRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
