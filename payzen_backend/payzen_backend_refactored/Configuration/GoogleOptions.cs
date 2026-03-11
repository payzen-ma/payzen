// Configuration options: GoogleOptions
// Holds Google/Gemini related configuration.
using System;

namespace payzen_backend.Configuration;

public class GoogleOptions
{
    public const string SectionName = "Google";

    public string ApiKey { get; set; } = null!;
    public string Model { get; set; } = "gemini-pro";
    public bool UseGemini { get; set; } = false;
}
