using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LoggingService.Application.Commands.ActivityLogs;
using LoggingService.Application.Common;
using LoggingService.Application.DTOs.ActivityLogs;
using LoggingService.Application.Services;
namespace LoggingService.API.Controllers;

[ApiController]
[Route("api/activitylogs")]
[Authorize]
public class ActivityLogsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IActivityLogService _activityLogService;
    public ActivityLogsController(IMediator mediator, IActivityLogService activityLogService)
    { _mediator = mediator; _activityLogService = activityLogService; }

    private bool IsSuperAdmin() => User.FindFirst("IsSuperAdmin")?.Value == "True";
    private Guid? TenantId() => Guid.TryParse(User.FindFirst("TenantId")?.Value, out var g) ? g : null;
    private Guid? BranchId() => Guid.TryParse(User.FindFirst("BranchId")?.Value, out var g) ? g : null;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ActivityLogListResponse>>> GetLogs(
        [FromQuery] string? actionType, [FromQuery] string? entityType,
        [FromQuery] string? serviceName, [FromQuery] string? userId,
        [FromQuery] Guid? tenantId, [FromQuery] Guid? branchId,
        [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
        [FromQuery] string? search, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] string sortOrder = "desc",
        [FromQuery] string sortBy = "Timestamp")
    {
        // Non-SuperAdmin sees only their own tenant/branch
        if (!IsSuperAdmin()) { tenantId = TenantId(); branchId = BranchId(); }
        var result = await _mediator.Send(new ListActivityLogsCommand
        {
            ActionType = actionType,
            EntityType = entityType,
            ServiceName = serviceName,
            UserId = userId,
            TenantId = tenantId,
            BranchId = branchId,
            FromDate = fromDate,
            ToDate = toDate,
            Search = search,
            Page = page,
            PageSize = pageSize,
            SortOrder = sortOrder,
            SortBy = sortBy
        });
        return Ok(ApiResponse<ActivityLogListResponse>.Ok(
            result, "Activity logs retrieved"));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ActivityLogResponse>>> GetLog(Guid id)
    {
        var result = await _mediator.Send(new GetActivityLogByIdCommand
        { Id = id, IsSuperAdmin = IsSuperAdmin(), CallerTenantId = TenantId() });
        return Ok(ApiResponse<ActivityLogResponse>.Ok(result, "Activity log retrieved"));
    }

    [HttpGet("available-filters")]
    public async Task<ActionResult<ApiResponse<AvailableFiltersResponse>>> GetAvailableFilters()
    {
        var result = await _activityLogService.GetAvailableFiltersAsync();
        return Ok(ApiResponse<AvailableFiltersResponse>.Ok(
            result, "Available filters retrieved"));
    }
}
