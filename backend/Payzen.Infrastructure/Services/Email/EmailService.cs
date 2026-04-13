using System.Net;
using System.Net.Mail;
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

        await SendEmailAsync(toEmail, subject, body, ct);
    }

    public async Task SendWelcomeCredentialsEmailAsync(
        string toEmail,
        string companyName,
        string login,
        string temporaryPassword,
        string loginUrl,
        CancellationToken ct = default)
    {
        var safeLoginUrl = string.IsNullOrWhiteSpace(loginUrl)
            ? (_config["Email:InvitationAppBaseUrl"]?.TrimEnd('/') ?? "http://localhost:4200") + "/login"
            : loginUrl;

        var subject = $"Bienvenue sur Payzen HR - Vos identifiants";

        var body = $@"
            <html>
            <body>
                <h2>Bienvenue sur Payzen HR</h2>
                <p>Votre compte a été créé pour rejoindre <strong>{companyName}</strong>.</p>
                <p>Voici vos identifiants de connexion :</p>
                <ul>
                    <li><strong>Login :</strong> {login}</li>
                    <li><strong>Mot de passe temporaire :</strong> {temporaryPassword}</li>
                </ul>
                <p><a href=""{safeLoginUrl}"">Cliquez ici pour vous connecter</a></p>
                <p>Pour des raisons de sécurité, changez votre mot de passe dès la première connexion.</p>
            </body>
            </html>
        ";

        var useMock = _config.GetValue<bool?>("Email:UseMock") ?? true;
        if (useMock)
        {
            _logger.LogWarning(
                "EMAIL MOCK ACTIVE - Welcome credentials non envoyées réellement. To={ToEmail}, Login={Login}, LoginUrl={LoginUrl}, BodyLength={BodyLength}",
                toEmail,
                login,
                safeLoginUrl,
                body.Length);
            return;
        }

        await SendEmailAsync(toEmail, subject, body, ct);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var host = _config["Email:SmtpHost"];
        var username = _config["Email:SmtpUsername"];
        var password = _config["Email:SmtpPassword"];
        var from = _config["Email:From"];
        var port = _config.GetValue<int?>("Email:SmtpPort") ?? 587;
        var enableSsl = _config.GetValue<bool?>("Email:SmtpEnableSsl") ?? true;

        if (string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Configuration SMTP incomplète (Email:SmtpHost, Email:SmtpUsername, Email:SmtpPassword).");
        }

        var sender = string.IsNullOrWhiteSpace(from) ? username : from;

        using var message = new MailMessage();
        message.From = new MailAddress(sender, "Payzen HR");
        message.To.Add(new MailAddress(toEmail));
        message.Subject = subject;
        message.Body = htmlBody;
        message.IsBodyHtml = true;

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(username, password)
        };

        try
        {
            await smtp.SendMailAsync(message, ct);
            _logger.LogInformation("Email envoyé avec succès. To={ToEmail}, Subject={Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Échec envoi email SMTP. To={ToEmail}, Host={Host}, Port={Port}", toEmail, host, port);
            throw;
        }
    }
}
