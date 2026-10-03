using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.SharedKernel;
using FluentValidation;

namespace AdminPlatform.Modules.Content.Application.ArticleCategories;

public sealed class CreateArticleCategoryRequestValidator : AbstractValidator<CreateArticleCategoryRequest>
{
    public CreateArticleCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ArticleCategory.MaxNameLength);
        RuleFor(x => x.Slug).Must(Slug.IsValid).When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug must be lowercase letters, digits and single hyphens.");
        RuleFor(x => x.ParentId).NotEqual(Guid.Empty);
    }
}

public sealed class UpdateArticleCategoryRequestValidator : AbstractValidator<UpdateArticleCategoryRequest>
{
    public UpdateArticleCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ArticleCategory.MaxNameLength);
        RuleFor(x => x.Slug).Must(Slug.IsValid).When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug must be lowercase letters, digits and single hyphens.");
        RuleFor(x => x.ParentId).NotEqual(Guid.Empty);
    }
}
