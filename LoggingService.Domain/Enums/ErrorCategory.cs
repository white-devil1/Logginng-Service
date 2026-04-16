namespace LoggingService.Domain.Enums;

public enum ErrorCategory
{
    ServerError = 0, AuthFailure = 1, ValidationFailure = 2,
    NotFound = 3, Conflict = 4, FrontendError = 5
}
