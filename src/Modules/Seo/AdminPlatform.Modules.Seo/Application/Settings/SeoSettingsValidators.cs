using AdminPlatform.Modules.Seo.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Seo.Application.Settings;

public sealed class UpdateSeoSettingsRequestValidator : AbstractValidator<UpdateSeoSettingsRequest>
{
    public UpdateSeoSettingsRequestValidator()
    {
        RuleFor(x => x.DefaultTitleTemplate).NotEmpty().MaximumLength(SeoSettings.MaxTitleTemplateLength)
            .Must(value => value.Contains(SeoSettings.TitlePlaceholder, StringComparison.Ordinal))
            .WithMessage($"The title template must contain the placeholder {SeoSettings.TitlePlaceholder}.");
        RuleFor(x => x.DefaultDescription).NotEmpty().MaximumLength(SeoSettings.MaxDescriptionLength);
        RuleFor(x => x.DefaultOgImageMediaId).MaximumLength(SeoSettings.MaxMediaIdLength);
        RuleFor(x => x.TwitterSite).MaximumLength(SeoSettings.MaxTwitterHandleLength);
        RuleFor(x => x.TwitterCreator).MaximumLength(SeoSettings.MaxTwitterHandleLength);
        RuleFor(x => x.RobotsDisallowPaths).NotNull()
            .Must(paths => paths is null || paths.Count <= SeoSettings.MaxDisallowPaths)
            .WithMessage($"At most {SeoSettings.MaxDisallowPaths} robots paths are allowed.");
        RuleForEach(x => x.RobotsDisallowPaths).MaximumLength(SeoSettings.MaxDisallowPathLength);
    }
}
