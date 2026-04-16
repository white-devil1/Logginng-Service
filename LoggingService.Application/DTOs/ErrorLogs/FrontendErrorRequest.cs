using LoggingService.Domain.Enums;
namespace LoggingService.Application.DTOs.ErrorLogs;

public class FrontendErrorRequest
{
    public string Message { get; set; } = default!;
    public string? StackTrace { get; set; }
    public string? RequestPath { get; set; }
    public string? AdditionalData { get; set; }
    public Severity Severity { get; set; } = Severity.Error;

    // Direct column mappings
    public string? HttpMethod { get; set; }
    public int? ResponseStatus { get; set; }
    public string? CorrelationId { get; set; }
    public string? ServiceName { get; set; }
    public string? Environment { get; set; }

    // Bundled into AdditionalData JSON
    public string? ErrorType { get; set; }
    public string? Component { get; set; }
    public string? AppVersion { get; set; }
    public string? ApiEndpoint { get; set; }
    public string? Url { get; set; }
}
