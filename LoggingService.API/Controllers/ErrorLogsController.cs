using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LoggingService.Application.Commands.ErrorLogs;
using LoggingService.Application.Common;
using LoggingService.Application.DTOs.ErrorLogs;
using LoggingService.Domain.Entities;
using LoggingService.Domain.Enums;
using LoggingService.Infrastructure.Persistence;
namespace LoggingService.API.Controllers;

[ApiController]
[Route("api/errorlogs")]
public class ErrorLogsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly LoggingDbContext _db;
    public ErrorLogsController(IMediator mediator, LoggingDbContext db)
    { _mediator = mediator; _db = db; }

    private bool IsSuperAdmin() => User.FindFirst("IsSuperAdmin")?.Value == "True";
    private Guid? TenantId() => Guid.TryParse(User.FindFirst("TenantId")?.Value, out var g) ? g : null;
    private Guid? BranchId() => Guid.TryParse(User.FindFirst("BranchId")?.Value, out var g) ? g : null;
    private string? UserId() => User.FindFirst("UserId")?.Value;
    private string? UserEmail() => User.FindFirst("email")?.Value ?? User.FindFirst("Email")?.Value;

    // GET — SuperAdmin only
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ErrorLogListResponse>>> GetLogs(
        [FromQuery] Severity? severity, [FromQuery] LogSource? source,
        [FromQuery] ErrorCategory? category,
        [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
        [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sortOrder = "desc",
        [FromQuery] string sortBy = "Timestamp")
    {
        if (!IsSuperAdmin()) return Forbid();
        var result = await _mediator.Send(new ListErrorLogsCommand
        {
            Severity = severity,
            Source = source,
            Category = category,
            FromDate = fromDate,
            ToDate = toDate,
            Search = search,
            Page = page,
            PageSize = pageSize,
            SortOrder = sortOrder,
            SortBy = sortBy
        });
        return Ok(ApiResponse<ErrorLogListResponse>.Ok(
            result, "Error logs retrieved"));
    }

    // GET by ID — SuperAdmin only
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ErrorLogResponse>>> GetLog(Guid id)
    {
        if (!IsSuperAdmin()) return Forbid();
        var result = await _mediator.Send(new GetErrorLogByIdCommand { Id = id });
        return Ok(ApiResponse<ErrorLogResponse>.Ok(result, "Error log retrieved"));
    }

    // POST frontend error — NO AUTH — any caller including unauthenticated
    // Pre-auth errors (login page, forgot password, OTP) post here without token
    // Post-auth errors also post here — JWT read if available to enrich the log
    [HttpPost("frontend")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> PostFrontendError(
        [FromBody] FrontendErrorRequest request)
    {
        string? userId = null, userEmail = null;
        Guid? tenantId = null, branchId = null;

        // Extract from JWT if present — enriches the log
        if (User.Identity?.IsAuthenticated == true)
        {
            userId = UserId();
            userEmail = UserEmail();
            tenantId = TenantId();
            branchId = BranchId();
        }

        var additionalData = BuildAdditionalData(
            request.AdditionalData,
            request.ErrorType, request.Component,
            request.AppVersion, request.ApiEndpoint, request.Url);

        _db.ErrorLogs.Add(new ErrorLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Severity = request.Severity,
            Source = LogSource.Frontend,
            Category = ErrorCategory.FrontendError,
            ServiceName = !string.IsNullOrEmpty(request.ServiceName) ? request.ServiceName : "Frontend",
            Environment = !string.IsNullOrEmpty(request.Environment) ? request.Environment : "Production",
            Message = request.Message,
            StackTrace = request.StackTrace,
            RequestPath = request.RequestPath,
            RequestMethod = request.HttpMethod,
            StatusCode = request.ResponseStatus,
            CorrelationId = request.CorrelationId,
            AdditionalData = additionalData,
            UserId = userId,
            UserEmail = userEmail,
            TenantId = tenantId,
            BranchId = branchId,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Error logged"));
    }

    private static string? BuildAdditionalData(
        string? originalAdditionalData,
        string? errorType, string? component,
        string? appVersion, string? apiEndpoint,
        string? url)
    {
        var hasExtra = !string.IsNullOrEmpty(errorType)
                    || !string.IsNullOrEmpty(component)
                    || !string.IsNullOrEmpty(appVersion)
                    || !string.IsNullOrEmpty(apiEndpoint)
                    || !string.IsNullOrEmpty(url)
                    || !string.IsNullOrEmpty(originalAdditionalData);
        if (!hasExtra) return null;

        var obj = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(errorType)) obj["errorType"] = errorType;
        if (!string.IsNullOrEmpty(component)) obj["component"] = component;
        if (!string.IsNullOrEmpty(appVersion)) obj["appVersion"] = appVersion;
        if (!string.IsNullOrEmpty(apiEndpoint)) obj["apiEndpoint"] = apiEndpoint;
        if (!string.IsNullOrEmpty(url)) obj["url"] = url;
        if (!string.IsNullOrEmpty(originalAdditionalData))
        {
            try
            {
                var existing = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(originalAdditionalData);
                if (existing != null)
                {
                    foreach (var kv in existing) obj[kv.Key] = kv.Value;
                }
                else
                {
                    obj["customData"] = originalAdditionalData;
                }
            }
            catch
            {
                obj["customData"] = originalAdditionalData;
            }
        }
        return System.Text.Json.JsonSerializer.Serialize(obj);
    }
}
