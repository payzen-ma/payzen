// DTO: ErrorResponse
// Standardized error response returned by ExceptionHandlerMiddleware.
// Fields:
// - StatusCode: HTTP status
// - Type: short error type identifier (e.g. NotFound, BadRequest)
// - Message: user-friendly message
// - Details: optional stacktrace or internal details (only in Development)
// - Errors: optional validation errors dictionary
using System;
using System.Collections.Generic;

namespace payzen_backend.DTOs.Common;

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Type { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Details { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
