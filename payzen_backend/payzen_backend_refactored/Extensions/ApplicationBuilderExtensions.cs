// Extensions: ApplicationBuilder helpers
// Centralizes the registration of common middlewares used in the pipeline.
using Microsoft.AspNetCore.Builder;
using payzen_backend.Middleware;

namespace payzen_backend.Extensions;

public static class ApplicationBuilderExtensions
{
    // Registers request logging, global exception handler and the rate limiter.
    public static WebApplication UseApplicationMiddlewares(this WebApplication app)
    {
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ExceptionHandlerMiddleware>();
        app.UseRateLimiter();
        return app;
    }
}
