using MediatR;
using LoggingService.Application.DTOs.ActivityLogs;
namespace LoggingService.Application.Commands.ActivityLogs;

public class GetActivityLogByIdCommand : IRequest<ActivityLogResponse>
{
    public Guid Id { get; set; }
    public Guid? CallerTenantId { get; set; }
    public bool IsSuperAdmin { get; set; }
}
