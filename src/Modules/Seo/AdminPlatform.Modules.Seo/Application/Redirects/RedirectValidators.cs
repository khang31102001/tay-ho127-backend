using AdminPlatform.Modules.Seo.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Seo.Application.Redirects;

internal static class RedirectRules
{
    /// <summary>Rules shared by the create and update requests; path normalization, destination safety and the
    /// self-redirect rule live in the <see cref="Redirect"/> domain entity.</summary>
    public static void Apply<T>(
        AbstractValidator<T> validator, Func<T, string> sourcePath, Func<T, string> destinationUrl, Func<T, int> redirectType)
    {
        validator.RuleFor(x => sourcePath(x)).NotEmpty().MaximumLength(Redirect.MaxPathLength);
        validator.RuleFor(x => destinationUrl(x)).NotEmpty().MaximumLength(Redirect.MaxPathLength);
        validator.RuleFor(x => redirectType(x)).Must(value => Enum.IsDefined(typeof(RedirectType), value))
            .WithMessage("The redirect type must be 301 or 302.");
    }
}

public sealed class CreateRedirectRequestValidator : AbstractValidator<CreateRedirectRequest>
{
    public CreateRedirectRequestValidator()
    {
        RedirectRules.Apply(this, x => x.SourcePath, x => x.DestinationUrl, x => x.RedirectType);
    }
}

public sealed class UpdateRedirectRequestValidator : AbstractValidator<UpdateRedirectRequest>
{
    public UpdateRedirectRequestValidator()
    {
        RedirectRules.Apply(this, x => x.SourcePath, x => x.DestinationUrl, x => x.RedirectType);
    }
}
