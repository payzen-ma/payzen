using System.ComponentModel.DataAnnotations;

namespace Payzen.Application.DTOs.Auth;

public class InviteAdminDto
{
    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public int RoleId { get; set; }
}

public class InviteEmployeeDto
{
    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public int RoleId { get; set; }

    [Required]
    public int EmployeeId { get; set; }
}

public class ValidateInvitationResponseDto
{
    public required string CompanyName { get; set; }
    public required string RoleName { get; set; }
    public required string MaskedEmail { get; set; }
    public required DateTime ExpiresAt { get; set; }
}

public class AcceptInvitationViaIdpDto
{
    [Required]
    public required string Token { get; set; }
}

public class InvitationAcceptResult
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public string? ExpectedEmail { get; set; }
    public string? ReceivedEmail { get; set; }

    public static InvitationAcceptResult Success() => new() { IsSuccess = true };

    public static InvitationAcceptResult NotFound() => new() { IsSuccess = false, Error = "INVITATION_NOT_FOUND" };

    public static InvitationAcceptResult AlreadyUsed() =>
        new() { IsSuccess = false, Error = "INVITATION_ALREADY_USED" };

    public static InvitationAcceptResult Expired() => new() { IsSuccess = false, Error = "INVITATION_EXPIRED" };

    public static InvitationAcceptResult EmailMismatch(string expected, string received) =>
        new()
        {
            IsSuccess = false,
            Error = "EMAIL_MISMATCH",
            ExpectedEmail = expected,
            ReceivedEmail = received,
        };

    /// <summary>Aucun utilisateur Payzen ne correspond (JWT ou e-mail invitation).</summary>
    public static InvitationAcceptResult UserNotLinked() => new() { IsSuccess = false, Error = "USER_NOT_LINKED" };

    /// <summary>Référentiel incomplet (ex. statut employé « Active » introuvable).</summary>
    public static InvitationAcceptResult MissingActiveStatus() =>
        new() { IsSuccess = false, Error = "MISSING_ACTIVE_STATUS" };
}
