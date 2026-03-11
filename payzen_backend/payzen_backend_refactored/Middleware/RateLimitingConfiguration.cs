// Rate limiting configuration helper
// Purpose: register global and named rate limiting policies used by the application.
// - "llm" : stricter limits for LLM endpoints
// - "auth": authentication endpoints with higher limits
// Added during Phase 1 (Infrastructure) in the refactored copy.
using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace payzen_backend.Middleware;

public static class RateLimitingConfiguration
{
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Global concurrency limiter (protects from spikes)
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetConcurrencyLimiter("global", _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = 100,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 100
                })
            );

            // LLM endpoints: more restrictive to avoid high token consumption
            options.AddPolicy("llm", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anon",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        AutoReplenishment = true,
                        QueueLimit = 0
                    }));

            // Auth endpoints: allow more requests but still protect from abuse
            options.AddPolicy("auth", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anon",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 1,
                        QueueLimit = 0
                    }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}
