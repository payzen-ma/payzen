using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Payzen.Application.Interfaces;

namespace Payzen.Infrastructure.Services.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendInvitationEmailAsync(string toEmail, string companyName, string roleName, string invitationToken, CancellationToken ct = default)
    {
        var appBaseUrl = _config["Email:InvitationAppBaseUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
        var acceptUrl = $"{appBaseUrl}/auth/accept-invite?token={invitationToken}";
        
        var subject = $"Invitation à rejoindre {companyName} sur Payzen HR";
        
        var body = $@"
            <html>
            <body>
                <h2>Bienvenue sur Payzen HR</h2>
                <p>Vous avez été invité à rejoindre <strong>{companyName}</strong> en tant que <strong>{roleName}</strong>.</p>
                <p>Cliquez sur le lien ci-dessous pour accepter l'invitation :</p>
                <p><a href=""{acceptUrl}"">Accepter l'invitation</a></p>
                <p>Ce lien expire dans 48 heures.</p>
                <p>Si vous n'avez pas demandé cette invitation, vous pouvez ignorer cet email.</p>
            </body>
            </html>
        ";
        
        var useMock = _config.GetValue<bool?>("Email:UseMock") ?? true;
        if (useMock)
        {
            _logger.LogWarning(
                "EMAIL MOCK ACTIVE - Invitation non envoyée réellement. To={ToEmail}, Subject={Subject}, AcceptUrl={AcceptUrl}, BodyLength={BodyLength}",
                toEmail,
                subject,
                acceptUrl,
                body.Length);
            return;
        }

        // TODO: Implémenter SMTP / provider réel ici.
        _logger.LogInformation(
            "Email provider non implémenté. Activez Email:UseMock=true en attendant. To={ToEmail}, AcceptUrl={AcceptUrl}",
            toEmail,
            acceptUrl);

        await Task.CompletedTask;
    }
}