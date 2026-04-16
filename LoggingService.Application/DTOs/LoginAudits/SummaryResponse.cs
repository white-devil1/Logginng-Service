namespace LoggingService.Application.DTOs.LoginAudits;

public class SummaryResponse
{
    public ErrorSummary ErrorLogs { get; set; } = new();
    public ActivitySummary ActivityLogs { get; set; } = new();
    public LoginSummary LoginAudits { get; set; } = new();
}
public class ErrorSummary
{
    public int TotalToday { get; set; }
    public int TotalThisWeek { get; set; }
    public int TotalThisMonth { get; set; }
    public Dictionary<string, int> ByCategory { get; set; } = new();
    public Dictionary<string, int> BySource { get; set; } = new();
    public Dictionary<string, int> ByService { get; set; } = new();
}
public class ActivitySummary
{
    public int TotalToday { get; set; }
    public int TotalThisWeek { get; set; }
    public Dictionary<string, int> ByEntityType { get; set; } = new();
    public Dictionary<string, int> ByActionType { get; set; } = new();
}
public class LoginSummary
{
    public int TotalLoginsToday { get; set; }
    public int TotalLogoutsToday { get; set; }
    public int TotalLoginsThisWeek { get; set; }
    public int UniqueUsersToday { get; set; }
}
