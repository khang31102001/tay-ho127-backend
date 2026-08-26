using FluentValidation;

namespace AdminPlatform.Modules.Navigation.Application.Menus;

public sealed class CreateMenuRequestValidator : AbstractValidator<CreateMenuRequest>
{
    public CreateMenuRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Route).MaximumLength(500);
        RuleFor(x => x.Icon).MaximumLength(100);
    }
}

public sealed class UpdateMenuRequestValidator : AbstractValidator<UpdateMenuRequest>
{
    public UpdateMenuRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Route).MaximumLength(500);
        RuleFor(x => x.Icon).MaximumLength(100);
    }
}
