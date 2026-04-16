namespace LoggingService.Domain.Enums;
// Used in ErrorLogs for AuthFailure category
public enum LoginFailureReason
{
    None = 0, WrongPassword = 1, AccountDeactivated = 2,
    AccountDeleted = 3, ExpiredTemporaryPassword = 4,
    InvalidToken = 5, ExpiredToken = 6
}
