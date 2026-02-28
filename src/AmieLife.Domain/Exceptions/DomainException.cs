namespace AmieLife.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-level rule violations.
/// Never expose this directly to the API response — the middleware maps it to HTTP errors.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Thrown when a requested resource does not exist.
/// Maps to HTTP 404.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string resource, object key)
        : base($"{resource} with identifier '{key}' was not found.") { }
}

/// <summary>
/// Thrown when a business rule is violated (e.g., account locked, token already used).
/// Maps to HTTP 400.
/// </summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message) { }
}

/// <summary>
/// Thrown when an operation is not permitted for the current user.
/// Maps to HTTP 403.
/// </summary>
public class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "You are not authorized to perform this action.")
        : base(message) { }
}
