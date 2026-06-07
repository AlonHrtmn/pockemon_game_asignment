using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace pokemon_backend.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error occurred");
            await WriteErrorResponse(context, HttpStatusCode.ServiceUnavailable, 
                "Database service temporarily unavailable. Please try again later.");
        }
        catch (Npgsql.NpgsqlException ex)
        {
            _logger.LogError(ex, "PostgreSQL connection error occurred");
            await WriteErrorResponse(context, HttpStatusCode.ServiceUnavailable, 
                "Database service temporarily unavailable. Please try again later.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "External HTTP request failed");
            await WriteErrorResponse(context, HttpStatusCode.BadGateway, 
                "External service temporarily unavailable. Please try again later.");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Request timeout occurred");
            await WriteErrorResponse(context, HttpStatusCode.GatewayTimeout, 
                "Request timed out. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred");
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError, 
                "An unexpected error occurred. Please try again later.");
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        var response = JsonSerializer.Serialize(new { message });
        await context.Response.WriteAsync(response);
    }
}
