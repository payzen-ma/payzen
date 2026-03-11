// Configuration options: JwtOptions
// Holds strongly typed JWT configuration values bound from configuration.
using System;

namespace payzen_backend.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshExpirationDays { get; set; } = 7;
}
