namespace AlMomiaa.Domain.Common;

public sealed class BusinessRuleException(string message, string errorCode = "domain-rule-violation")
    : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

public sealed class NotFoundException(string entityName, object key)
    : Exception($"{entityName} '{key}' was not found.");

public sealed class ConflictException(string message, string errorCode = "conflict")
    : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

public sealed class ForbiddenDomainException(string message)
    : Exception(message);
