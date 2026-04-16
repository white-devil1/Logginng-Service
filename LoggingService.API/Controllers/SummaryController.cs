using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LoggingService.Application.Commands.LoginAudits;
using LoggingService.Application.Common;
using LoggingService.Application.DTOs.LoginAudits;
namespace LoggingService.API.Controllers;

[ApiController]
[Route("api/summary")]
[Authorize]
public class SummaryController : ControllerBase
{
    private readonly IMediator _mediator;
    public SummaryController(IMediator mediator) => _mediator = mediator;

    private bool IsSuperAdmin() => User.FindFirst("IsSuperAdmin")?.Value == "True";
    private Guid? TenantId() => Guid.TryParse(User.FindFirst("TenantId")?.Value, out var g) ? g : null;
    private Guid? BranchId() => Guid.TryParse(User.FindFirst("BranchId")?.Value, out var g) ? g : null;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<SummaryResponse>>> GetSummary()
    {
        var result = await _mediator.Send(new GetSummaryCommand
        { IsSuperAdmin = IsSuperAdmin(), TenantId = TenantId(), BranchId = BranchId() });
        return Ok(ApiResponse<SummaryResponse>.Ok(result, "Summary retrieved"));
    }
}
