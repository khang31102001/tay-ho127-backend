using FluentValidation;

namespace AdminPlatform.Modules.Platform.Application.SystemSettings;

public sealed class CreateSystemSettingRequestValidator : AbstractValidator<CreateSystemSettingRequest>
{
    public CreateSystemSettingRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Value).NotNull().MaximumLength(4000);
    }
}

public sealed class UpdateSystemSettingRequestValidator : AbstractValidator<UpdateSystemSettingRequest>
{
    public UpdateSystemSettingRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Value).NotNull().MaximumLength(4000);
    }
}
