// Configuration options: AnthropicOptions
// Holds Anthropic/Claude related configuration (API key, model, etc.).
using System;

namespace payzen_backend.Configuration;

public class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    public string ApiKey { get; set; } = null!;
    public string Model { get; set; } = "claude-3-5-sonnet-20241022";
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.7;
    public bool UseMock { get; set; } = false;
}
