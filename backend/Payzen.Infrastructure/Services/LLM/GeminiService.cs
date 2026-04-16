using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Payzen.Application.DTOs.Payroll;
using Payzen.Application.Interfaces;

namespace Payzen.Infrastructure.Services.LLM;

public class GeminiService : ILlmService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly IWebHostEnvironment _env;

    public GeminiService(IHttpClientFactory httpFactory, IConfiguration config, IWebHostEnvironment env)
    {
        _http = httpFactory.CreateClient("Gemini");
        _apiKey = config["GoogleGemini:ApiKey"] ?? throw new InvalidOperationException("GoogleGemini:ApiKey manquant dans la configuration.");
        _model = config["GoogleGemini:Model"] ?? "gemini-1.5-pro";
        _env = env;
    }

    public async Task<string> AnalyseSalarieAsync(string regleContent, EmployeePayrollDto payrollData, string instruction, CancellationToken ct = default)
    {
        var systemPrompt = $"Tu es un expert en paie marocaine. Voici les règles applicables :\n{regleContent}";
        var userMessage = $"Analyse la fiche de paie suivante et {instruction}\n\nDonnées :\n{JsonSerializer.Serialize(payrollData, new JsonSerializerOptions { WriteIndented = true })}";
        return await CallApiAsync(systemPrompt, userMessage, ct);
    }

    public async Task<string> SimulationSalaryAsync(string regleContent, string instruction, CancellationToken ct = default)
    {
        var systemPrompt = $"Tu es un expert en simulation de paie marocaine. Voici les règles applicables :\n{regleContent}";
        return await CallApiAsync(systemPrompt, instruction, ct);
    }

    public async Task<string> SimulationSalaryStreamAsync(string regleContent, string instruction, CancellationToken ct = default)
        => await SimulationSalaryAsync(regleContent, instruction, ct);

    public async Task<string> SimulateQuickAsync(string regleContent, string instruction, CancellationToken ct = default)
        => await SimulationSalaryAsync(regleContent, "Réponds en une phrase courte : " + instruction, ct);

    public async Task<string> GenerateDslFromNaturalLanguageAsync(string title, string description, CancellationToken ct = default)
    {
        var rulesContent = await GetRulesAsync(ct);
        var systemPrompt = $@"Tu es un expert développeur en langage 'Payzen DSL'.
Tu dois traduire l'explication métier de l'utilisateur en un code strictement conforme à ce langage métier.
Voici les règles globales existantes pour comprendre la syntaxe :
{rulesContent}

Consignes strictes :
- Le résultat doit être encapsulé dans un bloc MODULE[CUSTOM_...] {title}.
- Le module doit contenir une instruction RULE (ex: RULE custom.xxx).
- Tu ne dois retourner QUE le code généré, sans blabla, sans introduction et sans marqueurs Markdown comme ```dsl. Le texte retourné sera compilé directement.";

        var userMessage = $"Voici la règle RH à traduire en DSL : \nTitre : {title}\nDescription textuelle : {description}";
        return await CallApiAsync(systemPrompt, userMessage, ct);
    }

    public async Task<string> GetRulesAsync(CancellationToken ct = default)
    {
        var generatedRulesDirectory = Path.Combine(_env.ContentRootPath, "rules", "generated");
        if (Directory.Exists(generatedRulesDirectory))
        {
            var latestGeneratedFile = Directory
                .GetFiles(generatedRulesDirectory, "regles_paie_company_*.txt")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(latestGeneratedFile) && File.Exists(latestGeneratedFile))
            {
                return await File.ReadAllTextAsync(latestGeneratedFile, ct);
            }
        }

        var candidatePaths = new[]
        {
            Path.Combine(_env.ContentRootPath, "rules", "regles_paie_compact.txt"),
            Path.Combine(_env.ContentRootPath, "rules", "regles_paie.txt"),
            Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "root", "regles_paie.txt"),
            Path.Combine(_env.ContentRootPath, "rules", "regle_simulateur.md")
        };

        var existingPath = candidatePaths.FirstOrDefault(File.Exists);
        if (existingPath == null) throw new FileNotFoundException("Aucun fichier de règles DSL introuvable.");

        return await File.ReadAllTextAsync(existingPath, ct);
    }

    private async Task<string> CallApiAsync(string systemPrompt, string userMessage, CancellationToken ct)
    {
        // Structure de la requête API Gemini
        var request = new
        {
            systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[]
            {
                new { parts = new[] { new { text = userMessage } } }
            },
            generationConfig = new
            {
                temperature = 0.2, // Faible température pour du code prédictif
                maxOutputTokens = 4096
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        
        var response = await _http.PostAsync(url, content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new Exception($"Gemini API Error: {response.StatusCode} - {err}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        try 
        {
            return result.GetProperty("candidates")[0]
                         .GetProperty("content")
                         .GetProperty("parts")[0]
                         .GetProperty("text").GetString() ?? string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
