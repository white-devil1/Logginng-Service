namespace LoggingService.Application.DTOs.ActivityLogs;

public class AvailableFiltersResponse
{
    public List<string> ActionTypes { get; set; } = new();
    public List<string> EntityTypes { get; set; } = new();
}
