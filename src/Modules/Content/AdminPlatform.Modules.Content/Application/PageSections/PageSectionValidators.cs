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
        Func<T, string?> ctaUrl)
    {
        validator.RuleFor(x => sectionKind(x)).Must(value => ContentWireFormat.TryParseSectionKind(value, out _))
            .WithMessage("SectionKind must be hero, introduction, promotion, highlight, testimonial or cta.");
        validator.RuleFor(x => eyebrow(x)).MaximumLength(PageSection.MaxTextLength);
        validator.RuleFor(x => heading(x)).MaximumLength(PageSection.MaxTextLength);
        validator.RuleFor(x => subheading(x)).MaximumLength(PageSection.MaxTextLength);
        validator.RuleFor(x => body(x)).MaximumLength(PageSection.MaxBodyLength);
        validator.RuleFor(x => mediaId(x)).MaximumLength(PageSection.MaxMediaIdLength);
        validator.RuleFor(x => ctaLabel(x)).MaximumLength(PageSection.MaxTextLength);
        validator.RuleFor(x => ctaUrl(x)).MaximumLength(PageSection.MaxUrlLength);
    }
}

public sealed class CreatePageSectionRequestValidator : AbstractValidator<CreatePageSectionRequest>
{
    public CreatePageSectionRequestValidator()
    {
        PageSectionRules.Apply(this, x => x.SectionKind, x => x.Eyebrow, x => x.Heading, x => x.Subheading, x => x.Body,
            x => x.MediaId, x => x.CtaLabel, x => x.CtaUrl);
    }
}

public sealed class UpdatePageSectionRequestValidator : AbstractValidator<UpdatePageSectionRequest>
{
    public UpdatePageSectionRequestValidator()
    {
        PageSectionRules.Apply(this, x => x.SectionKind, x => x.Eyebrow, x => x.Heading, x => x.Subheading, x => x.Body,
            x => x.MediaId, x => x.CtaLabel, x => x.CtaUrl);
    }
}
