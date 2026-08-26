using FluentValidation;

namespace AdminPlatform.Modules.AccessControl.Application.Permissions;

public sealed class CreatePermissionRequestValidator : AbstractValidator<CreatePermissionRequest>
{
    public CreatePermissionRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(150).Matches("^[a-z0-9]+(\\.[a-z0-9-]+)+$")
            .WithMessage("Code must look like 'resource.action', e.g. 'users.view'.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdatePermissionRequestValidator : AbstractValidator<UpdatePermissionRequest>
{
    public UpdatePermissionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
