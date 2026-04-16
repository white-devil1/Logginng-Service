namespace LoggingService.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public static ApiResponse<T> Ok(T? data, string message = "Success")
        => new() { Success = true, StatusCode = 200, Message = message, Data = data };
    public static ApiResponse<T> Fail(int statusCode, string message, List<string>? errors = null)
        => new() { Success = false, StatusCode = statusCode, Message = message, Errors = errors ?? new() };
}
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string message = "Success")
        => new() { Success = true, StatusCode = 200, Message = message };
    public static new ApiResponse Fail(int statusCode, string message, List<string>? errors = null)
        => new() { Success = false, StatusCode = statusCode, Message = message, Errors = errors ?? new() };
}
