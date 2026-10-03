using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.SharedKernel;
using FluentValidation;

namespace AdminPlatform.Modules.Content.Application.ArticleTags;

public sealed class CreateArticleTagRequestValidator : AbstractValidator<CreateArticleTagRequest>
{
    public CreateArticleTagRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ArticleTag.MaxNameLength);
        RuleFor(x => x.Slug).Must(Slug.IsValid).When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug must be lowercase letters, digits and single hyphens.");
    }
}

public sealed class UpdateArticleTagRequestValidator : AbstractValidator<UpdateArticleTagRequest>
{
    public UpdateArticleTagRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ArticleTag.MaxNameLength);
        RuleFor(x => x.Slug).Must(Slug.IsValid).When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug must be lowercase letters, digits and single hyphens.");
    }
}
