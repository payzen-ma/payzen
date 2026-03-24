using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Payzen.Application.Common;
using Payzen.Application.Interfaces;

namespace Payzen.Infrastructure.Services.Auth;

public class NativeAuthService : INativeAuthService
{
    private readonly HttpClient _http;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _baseUrl;
    private readonly ILogger<NativeAuthService> _logger;

    public NativeAuthService(IHttpClientFactory factory, IConfiguration config, ILogger<NativeAuthService> logger)
    {
        _http         = factory.CreateClient("EntraNativeAuth");
        _clientId     = config["EntraNativeAuth:ClientId"]!;
        _clientSecret = config["EntraNativeAuth:ClientSecret"]!;
        _baseUrl      = config["EntraNativeAuth:BaseUrl"]!;
        _logger       = logger;
    }

    // ── Sign in : email + mot de passe ───────────────────────────────────────
    public async Task<ServiceResult<(string Email, string Oid)>> SignInAsync(
        string email, string password, CancellationToken ct = default)
    {
        // Appel direct au token endpoint avec ROPC (Resource Owner Password Credentials)
        // Supporté par Entra External ID pour Native Auth email+password
        var payload = new Dictionary<string, string>
        {
            ["grant_type"]    = "password",
            ["client_id"]     = _clientId,
            ["client_secret"] = _clientSecret,
            ["username"]      = email,
            ["password"]      = password,
            ["scope"]         = "openid profile email"
        };

        var response = await PostFormAsync($"{_baseUrl}/token", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await BuildErrorMessage(response);
            // Credentials incorrects → message clair pour le frontend
            if (err.Contains("invalid_grant") || err.Contains("AADSTS"))
                return ServiceResult<(string, string)>.Fail(
                    "Email ou mot de passe incorrect.");
            return ServiceResult<(string, string)>.Fail(err);
        }

        var json    = await ParseJson(response);
        var idToken = json.GetProperty("id_token").GetString()!;
        var (extractedEmail, oid) = ExtractClaimsFromIdToken(idToken);

        if (extractedEmail == null || oid == null)
            return ServiceResult<(string, string)>.Fail(
                "Claims Entra insuffisants (email ou oid manquant).");

        return ServiceResult<(string, string)>.Ok((extractedEmail, oid));
    }

    // ── Sign up : créer un compte Entra ──────────────────────────────────────
    public async Task<ServiceResult> SignUpAsync(
        string email, string password, CancellationToken ct = default)
    {
        // Étape 1 : initier le signup
        var startPayload = new Dictionary<string, string>
        {
            ["client_id"]      = _clientId,
            ["username"]       = email,
            ["challenge_type"] = "password redirect"
        };

        var startResponse = await PostFormAsync(
            $"{_baseUrl}/signup/v1.0/start", startPayload, ct);

        if (!startResponse.IsSuccessStatusCode)
            return ServiceResult.Fail(await BuildErrorMessage(startResponse));

        var startJson           = await ParseJson(startResponse);
        var continuationToken   = startJson.GetProperty("continuation_token").GetString()!;

        // Étape 2 : soumettre le mot de passe
        var continuePayload = new Dictionary<string, string>
        {
            ["client_id"]          = _clientId,
            ["continuation_token"] = continuationToken,
            ["grant_type"]         = "password",
            ["password"]           = password
        };

        var continueResponse = await PostFormAsync(
            $"{_baseUrl}/signup/v1.0/continue", continuePayload, ct);

        if (!continueResponse.IsSuccessStatusCode)
        {
            var err = await BuildErrorMessage(continueResponse);
            if (err.Contains("password") || err.Contains("complexity"))
                return ServiceResult.Fail(
                    "Le mot de passe ne respecte pas les critères de sécurité requis.");
            return ServiceResult.Fail(err);
        }

        // Entra envoie automatiquement l'email de vérification
        return ServiceResult.Ok();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<HttpResponseMessage> PostFormAsync(
        string url, Dictionary<string, string> data, CancellationToken ct)
        => _http.PostAsync(url, new FormUrlEncodedContent(data), ct);

    private static async Task<JsonElement> ParseJson(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content).RootElement;
    }

    private static async Task<string> BuildErrorMessage(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        try
        {
            var json = JsonDocument.Parse(content).RootElement;
            var desc = json.TryGetProperty("error_description", out var d)
                ? d.GetString() : null;
            var code = json.TryGetProperty("error", out var e)
                ? e.GetString() : null;
            return desc ?? code ?? $"Erreur Entra {(int)response.StatusCode}";
        }
        catch { return $"Erreur Entra {(int)response.StatusCode}: {content}"; }
    }

    private static (string? Email, string? Oid) ExtractClaimsFromIdToken(string idToken)
    {
        try
        {
            var parts   = idToken.Split('.');
            if (parts.Length < 2) return (null, null);
            var padding = parts[1].Length % 4 == 0 ? "" : new string('=', 4 - parts[1].Length % 4);
            var json    = JsonDocument.Parse(
                Convert.FromBase64String(parts[1] + padding)).RootElement;

            var email = json.TryGetProperty("email", out var e) ? e.GetString()
                      : json.TryGetProperty("preferred_username", out var u) ? u.GetString()
                      : null;
            var oid = json.TryGetProperty("oid", out var o) ? o.GetString() : null;
            return (email, oid);
        }
        catch { return (null, null); }
    }
}