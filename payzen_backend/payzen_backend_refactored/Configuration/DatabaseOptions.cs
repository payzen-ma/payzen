// Configuration options: DatabaseOptions
// Holds database related configuration (connection string, timeouts, logging flags).
using System;

namespace payzen_backend.Configuration;

public class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    public string DefaultConnection { get; set; } = null!;
    public int CommandTimeout { get; set; } = 30;
    public bool EnableDetailedErrors { get; set; } = false;
    public bool EnableSensitiveDataLogging { get; set; } = false;
}
