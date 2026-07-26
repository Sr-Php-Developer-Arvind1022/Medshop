namespace Medshop.BuildingBlocks.Common;

public class ApiResponse<T>
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }

    public static ApiResponse<T> SuccessResult(T? data, string message = "Success")
        => new()
        {
            Status = true,
            Message = message,
            Data = data
        };

    public static ApiResponse<T> FailureResult(string message, Dictionary<string, string[]>? errors = null)
        => new()
        {
            Status = false,
            Message = message,
            Errors = errors
        };
}
