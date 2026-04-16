using Payzen.Application.DTOs.Auth;

namespace Payzen.Application.Interfaces;

public interface IInvitationService
{
    Task<string> CreateInvitationAsync(InviteAdminDto dto, CancellationToken ct = default);
    Task<string> CreateEmployeeInvitationAsync(InviteEmployeeDto dto, CancellationToken ct = default);
    Task<ValidateInvitationResponseDto?> ValidateTokenAsync(string token, CancellationToken ct = default);

    /// <param name="jwtUserId">Id utilisateur issu du JWT Payzen (claim uid/sub) après le premier entra-login.</param>
    Task<InvitationAcceptResult> AcceptViaIdpAsync(string token, int? jwtUserId, CancellationToken ct = default);
}
