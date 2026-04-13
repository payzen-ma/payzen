using Payzen.Application.Common;
using Payzen.Application.DTOs.Company;

namespace Payzen.Application.Interfaces;

public interface IPublicSignupService
{
    /// <summary>
    /// Crée une company + admin (fiche employé) puis envoie l'invitation via Microsoft Entra External ID.
    /// </summary>
    Task<ServiceResult<object>> SignupCompanyAdminAsync(PublicSignupRequest request, CancellationToken ct);

    /// <summary>
    /// Complete onboarding (post-auth Entra) : crée company + employee admin et lie le user courant.
    /// </summary>
    Task<ServiceResult<object>> CompleteCompanyOnboardingAsync(
        AuthenticatedCompanySignupDto request,
        int currentUserId,
        string currentUserEmail,
        CancellationToken ct);
}

public sealed record PublicSignupRequest(
    string CompanyName,
    string CompanyEmail,
    string CompanyPhoneNumber,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminPhone
);
