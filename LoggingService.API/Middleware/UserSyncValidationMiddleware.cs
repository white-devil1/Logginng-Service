using System.Security.Claims;
using LoggingService.Application.Services;
namespace LoggingService.API.Middleware;

public class UserSyncValidationMiddleware
{
    private readonly RequestDelegate _next;
    public UserSyncValidationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        { await _next(context); return; }

        // SuperAdmin bypasses UserSync check
        if (context.User.FindFirst("IsSuperAdmin")?.Value == "True")
        { await _next(context); return; }

        var userId = context.User.FindFirst("UserId")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            var svc = context.RequestServices.GetRequiredService<IUserSyncService>();
            var user = await svc.GetByUserIdAsync(userId);
            if (user == null || user.IsDeleted || !user.IsActive)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                { message = "Account is inactive or has been removed." });
                return;
            }
        }
        await _next(context);
    }
}
