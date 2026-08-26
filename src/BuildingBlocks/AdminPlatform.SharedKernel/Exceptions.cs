namespace AdminPlatform.SharedKernel;

/// <summary>Base for all domain/application errors that should be translated to a ProblemDetails response.</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}

/// <summary>Requested entity does not exist. Maps to HTTP 404.</summary>
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.")
    {
    }
}

/// <summary>The request conflicts with the current state of the resource. Maps to HTTP 409.</summary>
public sealed class ConflictException : DomainException
{
    public ConflictException(string message) : base(message)
    {
    }
}

/// <summary>A domain/business rule was violated. Maps to HTTP 400.</summary>
public sealed class BusinessRuleValidationException : DomainException
{
    public BusinessRuleValidationException(string message) : base(message)
    {
    }
}

/// <summary>The caller does not have the required permission. Maps to HTTP 403.</summary>
public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message)
    {
    }
}

/// <summary>Credentials or token could not be authenticated (bad password, expired/revoked/reused refresh
/// token). Deliberately generic — never reveals which part was wrong. Maps to HTTP 401.</summary>
public sealed class AuthenticationFailedException : DomainException
{
    public AuthenticationFailedException(string message) : base(message)
    {
    }
}
