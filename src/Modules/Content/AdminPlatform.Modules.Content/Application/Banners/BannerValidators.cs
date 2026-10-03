using AdminPlatform.Modules.Content.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Content.Application.Banners;

internal static class BannerRules
{
    /// <summary>Rules shared by the create and update requests; the CTA-URL safety and date-order rules live
    /// in the <see cref="Banner"/> domain entity.</summary>
    public static void Apply<T>(
        AbstractValidator<T> validator,
        Func<T, string> name,
        Func<T, string?> desktopMediaId,
        Func<T, string?> mobileMediaId,
        Func<T, string> altText,
        Func<T, string?> heading,
        Func<T, string?> subheading,
        Func<T, string?> ctaLabel,
        Func<T, string?> ctaUrl,
        Func<T, string> placement)
    {
        validator.RuleFor(x => name(x)).NotEmpty().MaximumLength(Banner.MaxNameLength);
        validator.RuleFor(x => desktopMediaId(x)).MaximumLength(Banner.MaxMediaIdLength);
        validator.RuleFor(x => mobileMediaId(x)).MaximumLength(Banner.MaxMediaIdLength);
        validator.RuleFor(x => altText(x)).NotEmpty().MaximumLength(Banner.MaxTextLength);
        validator.RuleFor(x => heading(x)).MaximumLength(Banner.MaxTextLength);
        validator.RuleFor(x => subheading(x)).MaximumLength(Banner.MaxTextLength);
        validator.RuleFor(x => ctaLabel(x)).MaximumLength(Banner.MaxTextLength);
        validator.RuleFor(x => ctaUrl(x)).MaximumLength(Banner.MaxUrlLength);
        validator.RuleFor(x => placement(x)).Must(value => BannerWireFormat.TryParsePlacement(value, out _))
            .WithMessage("Placement must be HOME_HERO, HOME_PROMOTION, MENU_HERO or ARTICLE_BANNER.");
    }
}

public sealed class CreateBannerRequestValidator : AbstractValidator<CreateBannerRequest>
{
    public CreateBannerRequestValidator()
    {
        BannerRules.Apply(this, x => x.Name, x => x.DesktopMediaId, x => x.MobileMediaId, x => x.AltText, x => x.Heading,
            x => x.Subheading, x => x.CtaLabel, x => x.CtaUrl, x => x.Placement);
    }
}

public sealed class UpdateBannerRequestValidator : AbstractValidator<UpdateBannerRequest>
{
    public UpdateBannerRequestValidator()
    {
        BannerRules.Apply(this, x => x.Name, x => x.DesktopMediaId, x => x.MobileMediaId, x => x.AltText, x => x.Heading,
            x => x.Subheading, x => x.CtaLabel, x => x.CtaUrl, x => x.Placement);
    }
}
