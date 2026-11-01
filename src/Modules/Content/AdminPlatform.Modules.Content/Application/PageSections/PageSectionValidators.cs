using AdminPlatform.Modules.Content.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Content.Application.PageSections;

internal static class PageSectionRules
{
    public static void Apply<T>(
        AbstractValidator<T> validator,
        Func<T, string> sectionKind,
        Func<T, string?> eyebrow,
        Func<T, string?> heading,
        Func<T, string?> subheading,
        Func<T, string?> body,
        Func<T, string?> mediaId,
        Func<T, string?> ctaLabel,
        Func<T, string?> ctaUrl,
        Func<T, PageSectionSettingsRequest?> settings)
    {
        validator.RuleFor(x => sectionKind(x)).Must(value => ContentWireFormat.TryParseSectionKind(value, out _))
            .WithMessage("SectionKind must be hero, introduction, promotion, highlight, testimonial, cta or story.");
        validator.RuleFor(x => eyebrow(x)).MaximumLength(PageSection.MaxTextLength);
        validator.RuleFor(x => heading(x)).MaximumLength(PageSection.MaxTextLength);
        validator.RuleFor(x => subheading(x)).MaximumLength(PageSection.MaxTextLength);
        validator.RuleFor(x => body(x)).MaximumLength(PageSection.MaxBodyLength);
        validator.RuleFor(x => mediaId(x)).MaximumLength(PageSection.MaxMediaIdLength);
        validator.RuleFor(x => ctaLabel(x)).MaximumLength(PageSection.MaxTextLength);
        validator.RuleFor(x => ctaUrl(x)).MaximumLength(PageSection.MaxUrlLength);
        validator.RuleFor(x => settings(x)!.GalleryMediaIds)
            .Must(ids => ids == null || ids.Count(id => !string.IsNullOrWhiteSpace(id)) <= PageSectionSettings.MaxGalleryMedia)
            .WithMessage($"Settings.GalleryMediaIds can have at most {PageSectionSettings.MaxGalleryMedia} images.")
            .Must(ids => ids == null || ids.All(id => id == null || id.Trim().Length <= PageSection.MaxMediaIdLength))
            .WithMessage($"Each gallery media id must be at most {PageSection.MaxMediaIdLength} characters.")
            .When(x => settings(x) != null);
    }
}

public sealed class CreatePageSectionRequestValidator : AbstractValidator<CreatePageSectionRequest>
{
    public CreatePageSectionRequestValidator()
    {
        PageSectionRules.Apply(this, x => x.SectionKind, x => x.Eyebrow, x => x.Heading, x => x.Subheading, x => x.Body,
            x => x.MediaId, x => x.CtaLabel, x => x.CtaUrl, x => x.Settings);
    }
}

public sealed class UpdatePageSectionRequestValidator : AbstractValidator<UpdatePageSectionRequest>
{
    public UpdatePageSectionRequestValidator()
    {
        PageSectionRules.Apply(this, x => x.SectionKind, x => x.Eyebrow, x => x.Heading, x => x.Subheading, x => x.Body,
            x => x.MediaId, x => x.CtaLabel, x => x.CtaUrl, x => x.Settings);
    }
}
