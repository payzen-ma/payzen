namespace Payzen.Application.Interfaces;

public interface IEmailService
{
    Task SendInvitationEmailAsync(
        string toEmail,
        string companyName,
        string roleName,
        string invitationToken,
        CancellationToken ct = default
    );
    Task SendWelcomeCredentialsEmailAsync(
        string toEmail,
        string companyName,
        string login,
        string temporaryPassword,
        string loginUrl,
        CancellationToken ct = default
    );
}
