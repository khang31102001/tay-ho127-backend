using FluentValidation;

namespace AdminPlatform.Modules.Catalog.Application.SalesMenuProducts;

public sealed class CreateSalesMenuProductRequestValidator : AbstractValidator<CreateSalesMenuProductRequest>
{
    public CreateSalesMenuProductRequestValidator()
    {
        RuleFor(x => x.SalesMenuId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.PriceOverride).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateSalesMenuProductRequestValidator : AbstractValidator<UpdateSalesMenuProductRequest>
{
    public UpdateSalesMenuProductRequestValidator()
    {
        RuleFor(x => x.SalesMenuId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.PriceOverride).GreaterThanOrEqualTo(0);
    }
}
