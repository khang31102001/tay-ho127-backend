using AdminPlatform.SharedKernel;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AdminPlatform.Common.Web;

/// <summary>Central exception -> ProblemDetails translation (api-design.md §18-21). Never leaks stack traces,
/// SQL errors, or file paths to the client — only a stable `type`/`title`/`status`/`traceId`.</summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, type, title, errors) = Map(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception ({StatusCode}) for {Method} {Path}", statusCode, httpContext.Request.Method, httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred. Contact support with the traceId if the problem persists."
                : exception.Message,
            Instance = httpContext.Request.Path,
        };
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Type, string Title, IReadOnlyDictionary<string, string[]>? Errors) Map(Exception exception) =>
        exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "validation_error",
                "One or more validation errors occurred.",
                validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            NotFoundException => (StatusCodes.Status404NotFound, "not_found", "Resource not found", null),
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "authentication_failed", "Authentication failed", null),
            ConflictException => (StatusCodes.Status409Conflict, "conflict", "Conflict", null),
            ForbiddenException => (StatusCodes.Status403Forbidden, "forbidden", "Forbidden", null),
            BusinessRuleValidationException => (StatusCodes.Status400BadRequest, "business_rule_violation", "Business rule violation", null),
            PostgresException { SqlState: "23503" } => (StatusCodes.Status400BadRequest, "invalid_reference", "One of the referenced ids does not exist.", null),
            PostgresException { SqlState: "23505" } => (StatusCodes.Status409Conflict, "duplicate", "A record with the same unique value already exists.", null),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "concurrency_conflict", "The resource was modified by someone else. Reload and try again.", null),
            _ => (StatusCodes.Status500InternalServerError, "internal_server_error", "An unexpected error occurred.", null)
        };
}
