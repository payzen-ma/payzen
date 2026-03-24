using Payzen.Application.DTOs.Auth;

namespace Payzen.Application.Interfaces;

public interface IInvitationService
{
    Task<string> CreateInvitationAsync(InviteAdminDto dto, CancellationToken ct = default);
    Task<string> CreateEmployeeInvitationAsync(InviteEmployeeDto dto, CancellationToken ct = default);
    Task<ValidateInvitationResponseDto?> ValidateTokenAsync(string token, CancellationToken ct = default);
    Task<InvitationAcceptResult> AcceptViaIdpAsync(string token, string idpEmail, CancellationToken ct = default);
}