using System.Net;
using System.Text.Json;
using LoggingService.Application.Common;
using LoggingService.Application.Common.Exceptions;
using AppValidationException = LoggingService.Application.Common.Exceptions.ValidationException;
namespace LoggingService.API.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    { _next = next; _logger = logger; }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await HandleAsync(context, ex);
        }
    }

    private static async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, msg, errors) = ex switch
        {
            NotFoundException e =>
                (HttpStatusCode.NotFound, e.Message, new List<string>()),
            AppValidationException e =>
                (HttpStatusCode.BadRequest, "Validation failed", e.Errors),
            UnauthorizedException e =>
                (HttpStatusCode.Forbidden, e.Message, new List<string>()),
            UnauthorizedAccessException e =>
                (HttpStatusCode.Forbidden, e.Message, new List<string>()),
            _ =>
                (HttpStatusCode.InternalServerError,
                 "An unexpected error occurred.",
                 new List<string>())
        };
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        var response = ApiResponse<object>.Fail((int)status, msg, errors);
        await context.Response.WriteAsJsonAsync(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}
