using AdminPlatform.Modules.Seo.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Seo.Application.Metadata;

/// <summary>The canonical-URL safety rule lives in the <see cref="SeoMetadata"/> domain entity.</summary>
public sealed class UpsertSeoMetadataRequestValidator : AbstractValidator<UpsertSeoMetadataRequest>
{
    public UpsertSeoMetadataRequestValidator()
    {
        RuleFor(x => x.EntityType).NotEmpty()
            .Must(value => SeoEntityTypeWire.TryParse(value, out _))
            .WithMessage($"EntityType must be {SeoEntityTypeWire.AllowedValues}.");
        RuleFor(x => x.EntityId).MaximumLength(SeoMetadata.MaxEntityIdLength)
            .NotEmpty().When(x => SeoEntityTypeWire.TryParse(x.EntityType, out var type) && type != SeoEntityType.Homepage)
            .WithMessage("EntityId is required unless the entity type is homepage.");

        RuleFor(x => x.MetaTitle).MaximumLength(SeoMetadata.MaxTitleLength);
        RuleFor(x => x.MetaDescription).MaximumLength(SeoMetadata.MaxDescriptionLength);
        RuleFor(x => x.CanonicalUrl).MaximumLength(SeoMetadata.MaxUrlLength);
        RuleFor(x => x.OgTitle).MaximumLength(SeoMetadata.MaxTitleLength);
        RuleFor(x => x.OgDescription).MaximumLength(SeoMetadata.MaxDescriptionLength);
        RuleFor(x => x.OgImageMediaId).MaximumLength(SeoMetadata.MaxMediaIdLength);
        RuleFor(x => x.TwitterTitle).MaximumLength(SeoMetadata.MaxTitleLength);
        RuleFor(x => x.TwitterDescription).MaximumLength(SeoMetadata.MaxDescriptionLength);
        RuleFor(x => x.TwitterImageMediaId).MaximumLength(SeoMetadata.MaxMediaIdLength);
    }
}
