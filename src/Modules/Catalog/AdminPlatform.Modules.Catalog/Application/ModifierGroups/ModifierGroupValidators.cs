using AdminPlatform.Modules.Catalog.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Catalog.Application.ModifierGroups;

public sealed class ModifierOptionRequestValidator : AbstractValidator<ModifierOptionRequest>
{
    public ModifierOptionRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}

public sealed class CreateModifierGroupRequestValidator : AbstractValidator<CreateModifierGroupRequest>
{
    public CreateModifierGroupRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SelectionType).NotEmpty().IsEnumName(typeof(ModifierSelectionType), caseSensitive: false);
        RuleFor(x => x.Options).NotEmpty().Must(options => options.Count <= ModifierGroup.MaxOptionCount)
            .WithMessage($"A modifier group can have at most {ModifierGroup.MaxOptionCount} options.");
        RuleForEach(x => x.Options).SetValidator(new ModifierOptionRequestValidator());
    }
}

public sealed class UpdateModifierGroupRequestValidator : AbstractValidator<UpdateModifierGroupRequest>
{
    public UpdateModifierGroupRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SelectionType).NotEmpty().IsEnumName(typeof(ModifierSelectionType), caseSensitive: false);
        RuleFor(x => x.Options).NotEmpty().Must(options => options.Count <= ModifierGroup.MaxOptionCount)
            .WithMessage($"A modifier group can have at most {ModifierGroup.MaxOptionCount} options.");
        RuleForEach(x => x.Options).SetValidator(new ModifierOptionRequestValidator());
    }
}
