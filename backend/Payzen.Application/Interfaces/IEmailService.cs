namespace Payzen.Application.Interfaces;

public interface IEmailService
{
    Task SendInvitationEmailAsync( string toEmail, string companyName, string roleName, string invitationToken, CancellationToken ct = default);
}