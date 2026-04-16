using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LoggingService.Application.Commands.LoginAudits;
using LoggingService.Application.Common;
using LoggingService.Application.DTOs.LoginAudits;
using LoggingService.Domain.Enums;
namespace LoggingService.API.Controllers;

[ApiController]
[Route("api/loginaudits")]
[Authorize]
public class LoginAuditsController : ControllerBase
{
    private readonly IMediator _mediator;
    public LoginAuditsController(IMediator mediator) => _mediator = mediator;

    private bool IsSuperAdmin() => User.FindFirst("IsSuperAdmin")?.Value == "True";
    private Guid? TenantId() => Guid.TryParse(User.FindFirst("TenantId")?.Value, out var g) ? g : null;
    private Guid? BranchId() => Guid.TryParse(User.FindFirst("BranchId")?.Value, out var g) ? g : null;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<LoginAuditListResponse>>> GetLogs(
        [FromQuery] LoginEventType? eventType, [FromQuery] string? userId,
        [FromQuery] string? email, [FromQuery] Guid? tenantId,
        [FromQuery] Guid? branchId, [FromQuery] string? ipAddress,
        [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sortOrder = "desc")
    {
        if (!IsSuperAdmin()) { tenantId = TenantId(); branchId = BranchId(); }
        var result = await _mediator.Send(new ListLoginAuditsCommand
        {
            EventType = eventType,
            UserId = userId,
            Email = email,
            TenantId = tenantId,
            BranchId = branchId,
            IpAddress = ipAddress,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize,
            SortOrder = sortOrder
        });
        return Ok(ApiResponse<LoginAuditListResponse>.Ok(
            result, "Login audits retrieved"));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LoginAuditResponse>>> GetLog(Guid id)
    {
        var result = await _mediator.Send(new GetLoginAuditByIdCommand
        { Id = id, IsSuperAdmin = IsSuperAdmin(), CallerTenantId = TenantId() });
        return Ok(ApiResponse<LoginAuditResponse>.Ok(result, "Login audit retrieved"));
    }
}
