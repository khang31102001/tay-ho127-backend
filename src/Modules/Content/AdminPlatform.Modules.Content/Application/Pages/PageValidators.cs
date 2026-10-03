using AdminPlatform.Modules.Content.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Content.Application.Pages;

internal static class PageRules
{
    public static void Apply<T>(AbstractValidator<T> validator, Func<T, string> name, Func<T, string?> slug, Func<T, string> status)
    {
        validator.RuleFor(x => name(x)).NotEmpty().MaximumLength(Page.MaxNameLength);
        validator.RuleFor(x => slug(x)).Must(Page.IsValidPath).When(x => !string.IsNullOrWhiteSpace(slug(x)))
            .WithMessage("Slug must be '/' or lowercase /segments joined by hyphens (e.g. /thuc-don).");
        validator.RuleFor(x => status(x)).Must(value => ContentWireFormat.TryParseStatus(value, out _))
            .WithMessage("Status must be draft, published or archived.");
    }
}

public sealed class CreatePageRequestValidator : AbstractValidator<CreatePageRequest>
{
    public CreatePageRequestValidator()
    {
        PageRules.Apply(this, x => x.Name, x => x.Slug, x => x.Status);
    }
}

public sealed class UpdatePageRequestValidator : AbstractValidator<UpdatePageRequest>
{
    public UpdatePageRequestValidator()
    {
        PageRules.Apply(this, x => x.Name, x => x.Slug, x => x.Status);
    }
}
