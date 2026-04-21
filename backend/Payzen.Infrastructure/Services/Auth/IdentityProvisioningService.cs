using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Payzen.Application.Common;
using Payzen.Application.Interfaces;

namespace Payzen.Infrastructure.Services.Auth;

public sealed class IdentityProvisioningService : IIdentityProvisioningService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityProvisioningService> _logger;

    public IdentityProvisioningService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<IdentityProvisioningService> logger
    )
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ServiceResult<ProvisionedIdentityResult>> ProvisionEmployeeAccountAsync(
        string email,
        string firstName,
        string lastName,
        CancellationToken ct = default
    )
    {
        var useMock =
            _configuration.GetValue<bool?>("IdentityProvisioning:UseMock")
            ?? _configuration.GetValue<bool?>("Email:UseMock")
            ?? true;

        var loginUrl =
            _configuration["IdentityProvisioning:LoginUrl"]?.Trim()
            ?? _configuration["Email:InvitationAppBaseUrl"]?.TrimEnd('/') + "/login"
            ?? "http://localhost:4200/login";

        var tempPassword = GenerateTemporaryPassword();

        if (useMock)
        {
            var fakeExternalId = Guid.NewGuid().ToString("N");
            _logger.LogWarning(
                "IDENTITY PROVISIONING MOCK ACTIVE. External account not created. Email={EmailMasked}",
                MaskEmail(email)
            );

            return ServiceResult<ProvisionedIdentityResult>.Ok(
                new ProvisionedIdentityResult
                {
                    ExternalId = fakeExternalId,
                    Login = email,
                    TemporaryPassword = tempPassword,
                    LoginUrl = loginUrl,
                }
            );
        }

        var tenantId =
            _configuration["EntraExternalId:TenantId"]?.Trim()
            ?? _configuration["Entra:TenantId"]?.Trim();
        var clientId =
            _configuration["EntraExternalId:ClientId"]?.Trim()
            ?? _configuration["Entra:ClientId"]?.Trim();
        var clientSecret =
            _configuration["EntraExternalId:ClientSecret"]?.Trim()
            ?? _configuration["Entra:ClientSecret"]?.Trim();
        var issuer =
            _configuration["IdentityProvisioning:Issuer"]?.Trim()
            ?? _configuration["IdentityProvisioning:TenantDomain"]?.Trim()
            ?? ExtractDomainFromCiamInstance(_configuration["EntraExternalId:Instance"])
            ?? ExtractIssuerFromAuthority(_configuration["Entra:Authority"])
            ?? string.Empty;

        if (
            string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(clientSecret)
        )
        {
            return ServiceResult<ProvisionedIdentityResult>.Fail(
                "Configuration Entra incomplète (TenantId, ClientId, ClientSecret)."
            );
        }

        var (graphToken, tokenError) = await GetGraphAccessTokenAsync(tenantId, clientId, clientSecret, ct);
        if (graphToken == null)
            return ServiceResult<ProvisionedIdentityResult>.Fail(
                tokenError ?? "Impossible d'obtenir un token Graph pour provisionner le compte."
            );

        var displayName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = email;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphToken);

        var detectedIssuer = await GetTenantDomainNameAsync(client, ct);
        if (!string.IsNullOrWhiteSpace(detectedIssuer))
        {
            _logger.LogInformation(
                "Using detected tenant domainName as issuer. ConfiguredIssuer={ConfiguredIssuer} DetectedIssuer={DetectedIssuer}",
                issuer,
                detectedIssuer
            );
            issuer = detectedIssuer;
        }
        else if (string.IsNullOrWhiteSpace(issuer))
        {
            return ServiceResult<ProvisionedIdentityResult>.Fail(
                "Impossible de déterminer l'issuer du tenant. Veuillez le configurer dans IdentityProvisioning:Issuer."
            );
        }
        else
        {
            _logger.LogInformation(
                "Using configured issuer (auto-detection failed). Issuer={Issuer}",
                issuer
            );
        }

        var issuersToTry = new List<string> { issuer };
        
        if (issuer.EndsWith(".onmicrosoft.com"))
        {
            var ciamInstance = _configuration["EntraExternalId:Instance"]?.Trim();
            if (!string.IsNullOrWhiteSpace(ciamInstance) && Uri.TryCreate(ciamInstance, UriKind.Absolute, out var ciamUri))
            {
                issuersToTry.Add(ciamUri.Host);
                
                var subdomain = issuer.Replace(".onmicrosoft.com", "");
                issuersToTry.Add($"{subdomain}.ciamlogin.com");
            }
        }

        _logger.LogInformation(
            "Will try issuers in order: {Issuers}",
            string.Join(", ", issuersToTry)
        );

        HttpResponseMessage? response = null;
        string? responseBody = null;

        foreach (var issuerToTry in issuersToTry)
        {
            _logger.LogInformation(
                "Creating user with issuer={Issuer} email={EmailMasked}",
                issuerToTry,
                MaskEmail(email)
            );

            var payload = BuildUserPayload(email, firstName, lastName, tempPassword, issuerToTry);
            var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
            _logger.LogDebug("User creation payload: {Payload}", payloadJson);
            
            var requestContent = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            response = await client.PostAsync("https://graph.microsoft.com/v1.0/users", requestContent, ct);
            responseBody = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("User created successfully with issuer={Issuer}", issuerToTry);
                break;
            }

            if (!responseBody.Contains("Issuer should match tenants domainName", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            _logger.LogWarning(
                "Issuer mismatch with {Issuer}, trying next option if available. Status={StatusCode}",
                issuerToTry,
                (int)response.StatusCode
            );
        }

        if (response == null || responseBody == null)
        {
            return ServiceResult<ProvisionedIdentityResult>.Fail(
                "Aucune réponse de l'API Graph lors de la création de l'utilisateur."
            );
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Graph user provisioning failed. Status={StatusCode} Body={ResponseBody}",
                (int)response.StatusCode,
                responseBody
            );

            return ServiceResult<ProvisionedIdentityResult>.Fail(
                $"La création du compte Entra a échoué ({(int)response.StatusCode}): {ExtractGraphError(responseBody)}"
            );
        }

        using var json = JsonDocument.Parse(responseBody);
        var externalId = json.RootElement.TryGetProperty("id", out var idNode) ? idNode.GetString() : null;

        if (string.IsNullOrWhiteSpace(externalId))
            return ServiceResult<ProvisionedIdentityResult>.Fail("Compte Entra créé sans identifiant retourné.");

        return ServiceResult<ProvisionedIdentityResult>.Ok(
            new ProvisionedIdentityResult
            {
                ExternalId = externalId,
                Login = email,
                TemporaryPassword = tempPassword,
                LoginUrl = loginUrl,
            }
        );
    }

    private async Task<(string? Token, string? Error)> GetGraphAccessTokenAsync(
        string tenantId,
        string clientId,
        string clientSecret,
        CancellationToken ct
    )
    {
        var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

        var form = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["grant_type"] = "client_credentials",
            ["scope"] = "https://graph.microsoft.com/.default",
        };

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Graph token request failed. Status={StatusCode} Body={ResponseBody}",
                (int)response.StatusCode,
                body
            );
            return (
                null,
                $"Impossible d'obtenir un token Graph ({(int)response.StatusCode}): {ExtractGraphError(body)}"
            );
        }

        using var json = JsonDocument.Parse(body);
        if (!json.RootElement.TryGetProperty("access_token", out var token))
            return (null, "Réponse OAuth invalide: access_token absent.");
        return (token.GetString(), null);
    }

    private static string BuildMailNickname(string email)
    {
        var localPart = email.Split('@')[0];
        var cleaned = new string(
            localPart.Where(ch => char.IsLetterOrDigit(ch) || ch == '.' || ch == '_' || ch == '-').ToArray()
        );
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "payzenuser";
        return cleaned.Length > 64 ? cleaned[..64] : cleaned;
    }

    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%";
        var all = upper + lower + digits + symbols;

        var chars = new List<char>
        {
            upper[Random.Shared.Next(upper.Length)],
            lower[Random.Shared.Next(lower.Length)],
            digits[Random.Shared.Next(digits.Length)],
            symbols[Random.Shared.Next(symbols.Length)],
        };

        for (var i = chars.Count; i < 14; i++)
            chars.Add(all[Random.Shared.Next(all.Length)]);

        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1)
            return "***" + email[Math.Max(at, 0)..];
        return email[0] + "***" + email[(at - 1)..];
    }

    private static string? ExtractIssuerFromAuthority(string? authority)
    {
        if (string.IsNullOrWhiteSpace(authority))
            return null;

        if (!Uri.TryCreate(authority, UriKind.Absolute, out var uri))
            return null;

        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 ? segments[0] : null;
    }

    private static string? ExtractDomainFromCiamInstance(string? instance)
    {
        if (string.IsNullOrWhiteSpace(instance))
            return null;

        if (!Uri.TryCreate(instance, UriKind.Absolute, out var uri))
            return null;

        var host = uri.Host;
        if (host.EndsWith(".ciamlogin.com", StringComparison.OrdinalIgnoreCase))
        {
            var subdomain = host.Substring(0, host.Length - ".ciamlogin.com".Length);
            return $"{subdomain}.onmicrosoft.com";
        }

        return null;
    }

    private static object BuildUserPayload(
        string email,
        string firstName,
        string lastName,
        string temporaryPassword,
        string issuer
    )
    {
        var displayName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = email;

        return new
        {
            accountEnabled = true,
            displayName,
            mailNickname = BuildMailNickname(email),
            identities = new[]
            {
                new
                {
                    signInType = "emailAddress",
                    issuer,
                    issuerAssignedId = email.Trim(),
                },
            },
            passwordProfile = new { password = temporaryPassword, forceChangePasswordNextSignIn = true },
            passwordPolicies = "DisablePasswordExpiration",
        };
    }

    private async Task<string?> GetTenantDomainNameAsync(HttpClient client, CancellationToken ct)
    {
        try
        {
            var response = await client.GetAsync("https://graph.microsoft.com/v1.0/organization?$select=verifiedDomains", ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body))
                return null;

            using var json = JsonDocument.Parse(body);
            if (!json.RootElement.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
                return null;

            var org = value[0];
            if (!org.TryGetProperty("verifiedDomains", out var domains) || domains.ValueKind != JsonValueKind.Array)
                return null;

            string? fallback = null;
            foreach (var d in domains.EnumerateArray())
            {
                var name = d.TryGetProperty("name", out var n) ? n.GetString() : null;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var isDefault = d.TryGetProperty("isDefault", out var def) && def.ValueKind == JsonValueKind.True;
                var isInitial = d.TryGetProperty("isInitial", out var ini) && ini.ValueKind == JsonValueKind.True;
                if (isDefault || isInitial)
                    return name;
                fallback ??= name;
            }

            return fallback;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect tenant domainName from Graph organization endpoint.");
            return null;
        }
    }

    private static string ExtractGraphError(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return "Réponse vide.";
        try
        {
            using var json = JsonDocument.Parse(responseBody);
            if (json.RootElement.TryGetProperty("error_description", out var desc))
                return desc.GetString() ?? responseBody;
            if (json.RootElement.TryGetProperty("error", out var err))
            {
                if (err.ValueKind == JsonValueKind.String)
                    return err.GetString() ?? responseBody;
                if (err.ValueKind == JsonValueKind.Object)
                {
                    if (err.TryGetProperty("message", out var msg))
                        return msg.GetString() ?? responseBody;
                    if (err.TryGetProperty("code", out var code))
                        return code.GetString() ?? responseBody;
                }
            }
        }
        catch
        {
            // keep raw body fallback
        }
        return responseBody;
    }
}
