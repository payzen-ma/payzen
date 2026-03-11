// Middleware: RequestLoggingMiddleware
// Purpose: Logs incoming HTTP requests with trace id, HTTP method, path,
// status code and elapsed time. Added during Phase 1 (Infrastructure)
// in the refactored project copy.
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace payzen_backend.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Start timer for request duration measurement
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;

        // Log request start
        _logger.LogInformation(
            "[{RequestId}] {Method} {Path} started",
            requestId,
            context.Request.Method,
            context.Request.Path);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response?.StatusCode;
            // Log completion with elapsed milliseconds
            _logger.LogInformation(
                "[{RequestId}] {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms",
                requestId,
                context.Request.Method,
                context.Request.Path,
                statusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
