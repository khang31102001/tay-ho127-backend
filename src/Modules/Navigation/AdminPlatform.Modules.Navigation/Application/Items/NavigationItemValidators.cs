using FluentValidation;

namespace AdminPlatform.Modules.Navigation.Application.Items;

internal static class NavigationItemRules
{
    public const string CodePattern = "^[a-z0-9][a-z0-9.\\-]*$";
    public static readonly string[] TargetTypes = ["route", "page", "external"];
}

public sealed class NavigationSiteDetailRequestValidator : AbstractValidator<NavigationSiteDetailRequest>
{
    public NavigationSiteDetailRequestValidator()
    {
        RuleFor(x => x.TargetType).Must(value => NavigationItemRules.TargetTypes.Contains(value?.ToLowerInvariant()))
            .WithMessage("TargetType must be 'route', 'page' or 'external'.");
    }
}

public sealed class CreateNavigationItemRequestValidator : AbstractValidator<CreateNavigationItemRequest>
{
    public CreateNavigationItemRequestValidator()
    {
        RuleFor(x => x.MenuId).NotEmpty();
        RuleFor(x => x.Code).MaximumLength(100).Matches(NavigationItemRules.CodePattern)
            .When(x => !string.IsNullOrWhiteSpace(x.Code))
            .WithMessage("Code may only contain lowercase letters, digits, '.' and '-'.");
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Url).MaximumLength(500);
        RuleFor(x => x.Icon).MaximumLength(100);
        RuleFor(x => x.Site!).SetValidator(new NavigationSiteDetailRequestValidator()).When(x => x.Site is not null);
    }
}

public sealed class UpdateNavigationItemRequestValidator : AbstractValidator<UpdateNavigationItemRequest>
{
    public UpdateNavigationItemRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Url).MaximumLength(500);
        RuleFor(x => x.Icon).MaximumLength(100);
        RuleFor(x => x.Site!).SetValidator(new NavigationSiteDetailRequestValidator()).When(x => x.Site is not null);
    }
}

public sealed class ReorderNavigationItemsRequestValidator : AbstractValidator<ReorderNavigationItemsRequest>
{
    public ReorderNavigationItemsRequestValidator()
    {
        RuleFor(x => x.MenuId).NotEmpty();
        RuleFor(x => x.OrderedItemIds).NotEmpty();
    }
}
