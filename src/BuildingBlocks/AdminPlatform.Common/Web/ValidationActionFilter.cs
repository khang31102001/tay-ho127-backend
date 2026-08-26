using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AdminPlatform.Common.Web;

/// <summary>Runs FluentValidation for every action argument that has a registered IValidator&lt;T&gt;,
/// before the action executes. A failure throws FluentValidation's ValidationException, which
/// GlobalExceptionHandler turns into a 400 ProblemDetails with a per-field `errors` map (api-design.md
/// §18) — controllers never call validators by hand. Registered once, globally, in the Host.</summary>
public sealed class ValidationActionFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationActionFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (_serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        await next();
    }
}
