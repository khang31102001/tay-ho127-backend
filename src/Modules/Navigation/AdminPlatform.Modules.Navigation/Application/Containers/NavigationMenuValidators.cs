using FluentValidation;

namespace AdminPlatform.Modules.Navigation.Application.Containers;

public sealed class UpdateNavigationMenuRequestValidator : AbstractValidator<UpdateNavigationMenuRequest>
{
    public UpdateNavigationMenuRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
