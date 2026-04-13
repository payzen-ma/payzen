using System.Net.Http.Headers;
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
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityProvisioningService> _logger;

    public IdentityProvisioningService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<IdentityProvisioningService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ServiceResult<ProvisionedIdentityResult>> ProvisionEmployeeAccountAsync(
        string email,
        string firstName,
        string lastName,
        CancellationToken ct = default)
    {
        var useMock = _configuration.GetValue<bool?>("IdentityProvisioning:UseMock")
            ?? _configuration.GetValue<bool?>("Email:UseMock")
            ?? true;

        var loginUrl = _configuration["IdentityProvisioning:LoginUrl"]?.Trim()
            ?? _configuration["Email:InvitationAppBaseUrl"]?.TrimEnd('/') + "/login"
            ?? "http://localhost:4200/login";

        var tempPassword = GenerateTemporaryPassword();

        if (useMock)
        {
            var fakeExternalId = Guid.NewGuid().ToString("N");
            _logger.LogWarning(
                "IDENTITY PROVISIONING MOCK ACTIVE. External account not created. Email={EmailMasked}",
                MaskEmail(email));

            return ServiceResult<ProvisionedIdentityResult>.Ok(new ProvisionedIdentityResult
            {
                ExternalId = fakeExternalId,
                Login = email,
                TemporaryPassword = tempPassword,
                LoginUrl = loginUrl
            });
        }

        var tenantId = _configuration["EntraExternalId:TenantId"]?.Trim();
        var clientId = _configuration["EntraExternalId:ClientId"]?.Trim();
        var clientSecret = _configuration["EntraExternalId:ClientSecret"]?.Trim();
        var issuer = _configuration["IdentityProvisioning:Issuer"]?.Trim()
            ?? _configuration["IdentityProvisioning:TenantDomain"]?.Trim();

        if (string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(clientSecret)
            || string.IsNullOrWhiteSpace(issuer))
        {
            return ServiceResult<ProvisionedIdentityResult>.Fail(
                "Configuration Entra incomplète (TenantId, ClientId, ClientSecret, Issuer)."
            );
        }

        var graphToken = await GetGraphAccessTokenAsync(tenantId, clientId, clientSecret, ct);
        if (graphToken == null)
            return ServiceResult<ProvisionedIdentityResult>.Fail("Impossible d'obtenir un token Graph pour provisionner le compte.");

        var displayName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = email;

        var payload = new
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
                    issuerAssignedId = email.Trim()
                }
            },
            passwordProfile = new
            {
                password = tempPassword,
                forceChangePasswordNextSignIn = true
            },
            passwordPolicies = "DisablePasswordExpiration"
        };

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphToken);

        var requestContent = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://graph.microsoft.com/v1.0/users", requestContent, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Graph user provisioning failed. Status={StatusCode} Body={ResponseBody}",
                (int)response.StatusCode,
                responseBody);

            return ServiceResult<ProvisionedIdentityResult>.Fail("La création du compte Entra a échoué.");
        }

        using var json = JsonDocument.Parse(responseBody);
        var externalId = json.RootElement.TryGetProperty("id", out var idNode)
            ? idNode.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(externalId))
            return ServiceResult<ProvisionedIdentityResult>.Fail("Compte Entra créé sans identifiant retourné.");

        return ServiceResult<ProvisionedIdentityResult>.Ok(new ProvisionedIdentityResult
        {
            ExternalId = externalId,
            Login = email,
            TemporaryPassword = tempPassword,
            LoginUrl = loginUrl
        });
    }

    private async Task<string?> GetGraphAccessTokenAsync(
        string tenantId,
        string clientId,
        string clientSecret,
        CancellationToken ct)
    {
        var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

        var form = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["grant_type"] = "client_credentials",
            ["scope"] = "https://graph.microsoft.com/.default"
        };

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Graph token request failed. Status={StatusCode} Body={ResponseBody}",
                (int)response.StatusCode,
                body);
            return null;
        }

        using var json = JsonDocument.Parse(body);
        return json.RootElement.TryGetProperty("access_token", out var token)
            ? token.GetString()
            : null;
    }

    private static string BuildMailNickname(string email)
    {
        var localPart = email.Split('@')[0];
        var cleaned = new string(localPart.Where(ch => char.IsLetterOrDigit(ch) || ch == '.' || ch == '_' || ch == '-').ToArray());
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
            symbols[Random.Shared.Next(symbols.Length)]
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
}
