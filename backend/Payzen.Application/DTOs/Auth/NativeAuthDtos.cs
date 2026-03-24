using System.ComponentModel.DataAnnotations;

namespace Payzen.Application.DTOs.Auth;

// ════ Sign in ════════════════════════════════════════════════════════

public class NativeSignInDto
{
    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }
}

// ════ Sign up ════════════════════════════════════════════════════════

public class NativeSignUpDto
{
    [Required, EmailAddress]
    public required string Email { get; set; }
    [Required, StringLength(100, MinimumLength = 8)]
    public required string Password { get; set; }
    // Optionnel - pré-rempli depuis l'invitation
    public string? InvitationToken { get; set; }
}

// ════ Invitations ════════════════════════════════════════════════════

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
    public int EmployeeId { get; set; }
    [Required]
    public int RoleId { get; set; }
}

public class InvitationValidateResponseDto
{
    public required string Email { get; set; }
    public required string CompanyName { get; set; }
    public required string RoleName { get; set; }
    public int CompanyId { get; set; }
    public int RoleId { get; set; }
    public int? EmployeeId { get; set; }
}

public class UpgradeGuestDto
{
    [Required]
    public required string Email { get; set; }
    [Required]
    public required string Password { get; set; }
}