using System.Net;
using FluentValidation;
using Medshop.BuildingBlocks.Common;

namespace Medshop.BuildingBlocks.Infrastructure.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteResponseAsync(context, (int)HttpStatusCode.BadRequest, "Validation Failed", ex.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteResponseAsync(context, (int)HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteResponseAsync(context, (int)HttpStatusCode.NotFound, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred.");
            await WriteResponseAsync(context, (int)HttpStatusCode.InternalServerError, "An unexpected error occurred.", new Dictionary<string, string[]> { { "exception", new[] { ex.ToString() } } });
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, int statusCode, string message, Dictionary<string, string[]>? errors = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.FailureResult(message, errors);
        await context.Response.WriteAsJsonAsync(response);
    }
}
