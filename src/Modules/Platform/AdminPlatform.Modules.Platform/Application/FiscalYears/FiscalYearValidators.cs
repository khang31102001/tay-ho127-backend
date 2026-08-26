using FluentValidation;

namespace AdminPlatform.Modules.Platform.Application.FiscalYears;

public sealed class CreateFiscalYearRequestValidator : AbstractValidator<CreateFiscalYearRequest>
{
    public CreateFiscalYearRequestValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}

public sealed class UpdateFiscalYearRequestValidator : AbstractValidator<UpdateFiscalYearRequest>
{
    public UpdateFiscalYearRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}
