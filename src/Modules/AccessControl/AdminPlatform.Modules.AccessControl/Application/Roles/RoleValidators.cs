using FluentValidation;

namespace AdminPlatform.Modules.AccessControl.Application.Roles;

public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100).Matches("^[a-z0-9_.-]+$")
            .WithMessage("Code must be lowercase alphanumeric with '.', '_' or '-'.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class AssignPermissionsRequestValidator : AbstractValidator<AssignPermissionsRequest>
{
    public AssignPermissionsRequestValidator()
    {
        RuleFor(x => x.PermissionIds).NotNull();
    }
}
