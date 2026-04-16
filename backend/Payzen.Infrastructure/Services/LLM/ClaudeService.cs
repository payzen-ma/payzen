using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Payzen.Application.DTOs.Payroll;
using Payzen.Application.Interfaces;

namespace Payzen.Infrastructure.Services.LLM;

public class ClaudeService : ILlmService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly IWebHostEnvironment _env;

    public ClaudeService(IHttpClientFactory httpFactory, IConfiguration config, IWebHostEnvironment env)
    {
        _http = httpFactory.CreateClient("Claude");
        _apiKey =
            config["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic:ApiKey manquant dans la configuration.");
        _model = config["Anthropic:Model"] ?? "claude-sonnet-4-20250514";
        _env = env;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct = default) =>
        await CallApiAsync(systemPrompt, userMessage, ct);

    public async Task<string> AnalyseSalarieAsync(
        string regleContent,
        EmployeePayrollDto payrollData,
        string instruction,
        CancellationToken ct = default
    )
    {
        var systemPrompt = $"Tu es un expert en paie marocaine. Voici les règles applicables :\n{regleContent}";
        var userMessage =
            $"Analyse la fiche de paie suivante et {instruction}\n\nDonnées :\n{JsonSerializer.Serialize(payrollData, new JsonSerializerOptions { WriteIndented = true })}";
        return await CallApiAsync(systemPrompt, userMessage, ct);
    }

    public async Task<string> SimulationSalaryAsync(
        string regleContent,
        string instruction,
        CancellationToken ct = default
    )
    {
        var systemPrompt =
            $"Tu es un expert en simulation de paie marocaine. Voici les règles applicables :\n{regleContent}";
        return await CallApiAsync(systemPrompt, instruction, ct);
    }

    public async Task<string> SimulationSalaryStreamAsync(
        string regleContent,
        string instruction,
        CancellationToken ct = default
    ) => await SimulationSalaryAsync(regleContent, instruction, ct);

    public async Task<string> SimulateQuickAsync(
        string regleContent,
        string instruction,
        CancellationToken ct = default
    ) => await SimulationSalaryAsync(regleContent, "Réponds en une phrase courte : " + instruction, ct);

    public async Task<string> GetRulesAsync(CancellationToken ct = default)
    {
        var candidatePaths = new[]
        {
            Path.Combine(_env.ContentRootPath, "rules", "regles_paie_compact.txt"),
            Path.Combine(_env.ContentRootPath, "rules", "regles_paie.txt"),
            Path.Combine(_env.ContentRootPath, "rules", "regle_simulateur.md"),
            // Fallback temporaire pendant la migration backend -> monolith.
            Path.GetFullPath(
                Path.Combine(
                    _env.ContentRootPath,
                    "..",
                    "..",
                    "payzen",
                    "payzen_backend",
                    "payzen_backend",
                    "rules",
                    "regles_paie_compact.txt"
                )
            ),
            Path.GetFullPath(
                Path.Combine(
                    _env.ContentRootPath,
                    "..",
                    "..",
                    "payzen",
                    "payzen_backend",
                    "payzen_backend",
                    "rules",
                    "regles_paie.txt"
                )
            ),
        };

        var existingPath = candidatePaths.FirstOrDefault(File.Exists);
        if (existingPath == null)
        {
            throw new FileNotFoundException(
                "Aucun fichier de règles DSL introuvable. Placez le fichier dans Payzen.Api/rules/regles_paie_compact.txt."
            );
        }

        return await File.ReadAllTextAsync(existingPath, ct);
    }

    private async Task<string> CallApiAsync(string systemPrompt, string userMessage, CancellationToken ct)
    {
        var request = new
        {
            model = _model,
            max_tokens = 4096,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userMessage } },
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var response = await _http.PostAsync("https://api.anthropic.com/v1/messages", content, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return result.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
    }
}
