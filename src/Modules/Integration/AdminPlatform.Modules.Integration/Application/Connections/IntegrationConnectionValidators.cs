using FluentValidation;

namespace AdminPlatform.Modules.Integration.Application.Connections;

public sealed class UpdateIntegrationConnectionRequestValidator : AbstractValidator<UpdateIntegrationConnectionRequest>
{
    public UpdateIntegrationConnectionRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(Domain.IntegrationConnection.MaxDisplayNameLength);
        RuleFor(x => x.Environment).NotEmpty()
            .Must(value => IntegrationConnectionMapper.TryParseEnvironment(value, out _))
            .WithMessage("Environment must be 'sandbox' or 'live'.");
        RuleFor(x => x.Config).Must(config => config is null || config.Count <= 50)
            .WithMessage("Too many settings.");
    }
}
