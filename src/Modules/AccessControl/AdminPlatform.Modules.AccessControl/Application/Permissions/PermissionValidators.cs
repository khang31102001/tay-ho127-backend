using AdminPlatform.Modules.AccessControl.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.AccessControl.Application.Permissions;

internal static class PermissionCodeRules
{
    public const string LeafPattern = "^[a-z0-9]+(\\.[a-z0-9-]+)+$";
    public const string GroupPattern = "^group:[a-z0-9-]+(\\.[a-z0-9-]+)*$";
}

public sealed class CreatePermissionRequestValidator : AbstractValidator<CreatePermissionRequest>
{
    public CreatePermissionRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Code).Matches(PermissionCodeRules.GroupPattern).When(x => x.IsGroup)
            .WithMessage($"A group code must look like '{Permission.GroupCodePrefix}module' or '{Permission.GroupCodePrefix}module.resource'.");
        RuleFor(x => x.Code).Matches(PermissionCodeRules.LeafPattern).When(x => !x.IsGroup)
            .WithMessage("Code must look like 'resource.action', e.g. 'users.view'.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ParentId).NotNull().When(x => !x.IsGroup)
            .WithMessage("A permission must belong to a group.");
    }
}

public sealed class UpdatePermissionRequestValidator : AbstractValidator<UpdatePermissionRequest>
{
    public UpdatePermissionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ParentId).NotNull().When(x => !x.IsGroup)
            .WithMessage("A permission must belong to a group.");
    }
}
