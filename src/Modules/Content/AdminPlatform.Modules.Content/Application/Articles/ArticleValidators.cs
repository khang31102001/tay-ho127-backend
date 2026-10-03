using AdminPlatform.Modules.Content.Domain;
using AdminPlatform.SharedKernel;
using FluentValidation;

namespace AdminPlatform.Modules.Content.Application.Articles;

internal static class ArticleRules
{
    public const int MaxTagCount = 20;

    /// <summary>Rules shared by the create and update requests.</summary>
    public static void Apply<T>(
        AbstractValidator<T> validator,
        Func<T, string> title,
        Func<T, string?> slug,
        Func<T, string> summary,
        Func<T, string> content,
        Func<T, string?> featuredMediaId,
        Func<T, IReadOnlyList<Guid>?> tagIds,
        Func<T, string> authorName,
        Func<T, string> status)
    {
        validator.RuleFor(x => title(x)).NotEmpty().MaximumLength(Article.MaxTitleLength);
        validator.RuleFor(x => slug(x)).Must(Slug.IsValid).When(x => !string.IsNullOrWhiteSpace(slug(x)))
            .WithMessage("Slug must be lowercase letters, digits and single hyphens.");
        validator.RuleFor(x => summary(x)).NotEmpty().MaximumLength(Article.MaxSummaryLength);
        validator.RuleFor(x => content(x)).NotEmpty();
        validator.RuleFor(x => featuredMediaId(x)).MaximumLength(Article.MaxMediaIdLength);
        validator.RuleFor(x => tagIds(x)).Must(ids => ids is null || ids.Count <= MaxTagCount)
            .WithMessage($"An article can have at most {MaxTagCount} tags.");
        validator.RuleFor(x => authorName(x)).NotEmpty().MaximumLength(Article.MaxAuthorLength);
        validator.RuleFor(x => status(x)).Must(value => ContentWireFormat.TryParseStatus(value, out _))
            .WithMessage("Status must be draft, published or archived.");
    }
}

public sealed class CreateArticleRequestValidator : AbstractValidator<CreateArticleRequest>
{
    public CreateArticleRequestValidator()
    {
        ArticleRules.Apply(this, x => x.Title, x => x.Slug, x => x.Summary, x => x.Content, x => x.FeaturedMediaId,
            x => x.TagIds, x => x.AuthorName, x => x.Status);
    }
}

public sealed class UpdateArticleRequestValidator : AbstractValidator<UpdateArticleRequest>
{
    public UpdateArticleRequestValidator()
    {
        ArticleRules.Apply(this, x => x.Title, x => x.Slug, x => x.Summary, x => x.Content, x => x.FeaturedMediaId,
            x => x.TagIds, x => x.AuthorName, x => x.Status);
    }
}
