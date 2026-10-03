using AdminPlatform.Modules.Seo.Application.Metadata;
using AdminPlatform.Modules.Seo.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Seo.Application.Schemas;

/// <summary>The JSON validity and "Advanced Mode needs a JSON-LD" rules live in the <see cref="SeoSchema"/> domain entity.</summary>
public sealed class UpsertSeoSchemaRequestValidator : AbstractValidator<UpsertSeoSchemaRequest>
{
    public UpsertSeoSchemaRequestValidator()
    {
        RuleFor(x => x.EntityType).NotEmpty()
            .Must(value => SeoEntityTypeWire.TryParse(value, out _))
            .WithMessage($"EntityType must be {SeoEntityTypeWire.AllowedValues}.");
        RuleFor(x => x.EntityId).MaximumLength(SeoMetadata.MaxEntityIdLength)
            .NotEmpty().When(x => SeoEntityTypeWire.TryParse(x.EntityType, out var type) && type != SeoEntityType.Homepage)
            .WithMessage("EntityId is required unless the entity type is homepage.");
        RuleFor(x => x.SchemaType).NotEmpty()
            .Must(value => SeoSchemaTypeWire.TryParse(value, out _))
            .WithMessage($"SchemaType must be {SeoSchemaTypeWire.AllowedValues}.");
        RuleFor(x => x.CustomJsonLd).MaximumLength(SeoSchema.MaxCustomJsonLdLength);
    }
}
