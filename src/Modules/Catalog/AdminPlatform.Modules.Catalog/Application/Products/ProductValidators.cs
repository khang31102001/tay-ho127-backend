using AdminPlatform.SharedKernel;
using AdminPlatform.Modules.Catalog.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Catalog.Application.Products;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).Must(Slug.IsValid).When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug must be lowercase letters, digits and single hyphens.");
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OldPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Badge).MaximumLength(64);
        RuleFor(x => x.Rating).InclusiveBetween(0, 5);
        RuleFor(x => x.RatingCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MediaIds).Must(ids => ids is null || ids.Count <= Product.MaxMediaCount)
            .WithMessage($"A product can have at most {Product.MaxMediaCount} images.");
        RuleForEach(x => x.MediaIds).NotEmpty().MaximumLength(ProductMedia.MaxMediaIdLength);
        RuleForEach(x => x.ModifierGroupIds).NotEmpty();
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).Must(Slug.IsValid).When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug must be lowercase letters, digits and single hyphens.");
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OldPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Badge).MaximumLength(64);
        RuleFor(x => x.Rating).InclusiveBetween(0, 5);
        RuleFor(x => x.RatingCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MediaIds).Must(ids => ids is null || ids.Count <= Product.MaxMediaCount)
            .WithMessage($"A product can have at most {Product.MaxMediaCount} images.");
        RuleForEach(x => x.MediaIds).NotEmpty().MaximumLength(ProductMedia.MaxMediaIdLength);
        RuleForEach(x => x.ModifierGroupIds).NotEmpty();
    }
}
