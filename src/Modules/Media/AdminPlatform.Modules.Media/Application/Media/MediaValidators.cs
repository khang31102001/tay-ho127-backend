using AdminPlatform.Modules.Media.Domain;
using FluentValidation;

namespace AdminPlatform.Modules.Media.Application.Media;

public sealed class CreateMediaRequestValidator : AbstractValidator<CreateMediaRequest>
{
    public CreateMediaRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.Type).NotEmpty().IsEnumName(typeof(MediaKind), caseSensitive: false);
        RuleFor(x => x.AltText).MaximumLength(512);
        RuleFor(x => x.Size).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateMediaRequestValidator : AbstractValidator<UpdateMediaRequest>
{
    public UpdateMediaRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.Type).NotEmpty().IsEnumName(typeof(MediaKind), caseSensitive: false);
        RuleFor(x => x.AltText).MaximumLength(512);
    }
}
