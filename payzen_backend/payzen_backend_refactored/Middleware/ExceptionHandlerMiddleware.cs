// Middleware: ExceptionHandlerMiddleware
// Purpose: Capture toutes les exceptions non gérées et renvoyer une réponse JSON
// structurée (`ErrorResponse`) avec le code HTTP approprié. Ajouté en Phase 1
// (Infrastructure) dans la copie refactorisée.
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using payzen_backend.DTOs.Common;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace payzen_backend.Middleware;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlerMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log the exception server-side for observability
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var response = context.Response;
        response.ContentType = "application/json";

        // Map known exception types to proper HTTP status codes and structured ErrorResponse
        var errorResponse = exception switch
        {
            KeyNotFoundException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Message = exception.Message,
                Type = "NotFound"
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Message = exception.Message,
                Type = "Unauthorized"
            },
            ArgumentException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = exception.Message,
                Type = "BadRequest"
            },
            InvalidOperationException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Conflict,
                Message = exception.Message,
                Type = "Conflict"
            },
            _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = _env.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                Type = "InternalServerError",
                Details = _env.IsDevelopment() ? exception.StackTrace : null
            }
        };

        response.StatusCode = errorResponse.StatusCode;

        // Ensure camelCase response body for JS clients
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await response.WriteAsJsonAsync(errorResponse, options);
    }
}
