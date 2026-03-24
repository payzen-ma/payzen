using Microsoft.EntityFrameworkCore;
using Payzen.Application.DTOs.Auth;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Auth;
using Payzen.Infrastructure.Persistence;
using Payzen.Domain.Enums.Auth;

namespace Payzen.Infrastructure.Services.Auth;

public class InvitationService : IInvitationService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;

    public InvitationService(AppDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    public async Task<string> CreateInvitationAsync(InviteAdminDto dto, CancellationToken ct = default)
    {
        var token = Guid.NewGuid().ToString("N");
        
        var invitation = new Invitation
        {
            Token = token,
            Email = dto.Email.ToLowerInvariant(),
            CompanyId = dto.CompanyId,
            RoleId = dto.RoleId,
            EmployeeId = null,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(48),
            CreatedAt = DateTime.UtcNow
        };
        
        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync(ct);
        
        // Envoyer l'email d'invitation
        var company = await _db.Companies
            .Where(c => c.Id == dto.CompanyId)
            .Select(c => c.CompanyName)
            .FirstOrDefaultAsync(ct);
        
        var role = await _db.Roles
            .Where(r => r.Id == dto.RoleId)
            .Select(r => r.Name)
            .FirstOrDefaultAsync(ct);
        
        await _emailService.SendInvitationEmailAsync(
            dto.Email, 
            company ?? "Payzen", 
            role ?? "Admin", 
            token, 
            ct);
        
        return token;
    }

    public async Task<string> CreateEmployeeInvitationAsync(InviteEmployeeDto dto, CancellationToken ct = default)
    {
        var token = Guid.NewGuid().ToString("N");
        
        var invitation = new Invitation
        {
            Token = token,
            Email = dto.Email.ToLowerInvariant(),
            CompanyId = dto.CompanyId,
            RoleId = dto.RoleId,
            EmployeeId = dto.EmployeeId,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(48),
            CreatedAt = DateTime.UtcNow
        };
        
        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync(ct);
        
        var company = await _db.Companies
            .Where(c => c.Id == dto.CompanyId)
            .Select(c => c.CompanyName)
            .FirstOrDefaultAsync(ct);
        
        var role = await _db.Roles
            .Where(r => r.Id == dto.RoleId)
            .Select(r => r.Name)
            .FirstOrDefaultAsync(ct);
        
        await _emailService.SendInvitationEmailAsync(
            dto.Email, 
            company ?? "Payzen", 
            role ?? "Employé", 
            token, 
            ct);
        
        return token;
    }

    public async Task<ValidateInvitationResponseDto?> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        var invitation = await _db.Invitations
            .Include(i => i.Company)
            .Include(i => i.Role)
            .Where(i => i.Token == token && i.DeletedAt == null)
            .FirstOrDefaultAsync(ct);
        
        if (invitation == null || invitation.Status != InvitationStatus.Pending)
            return null;
        
        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await _db.SaveChangesAsync(ct);
            return null;
        }
        
        return new ValidateInvitationResponseDto
        {
            CompanyName = invitation.Company?.CompanyName ?? "Payzen",
            RoleName = invitation.Role?.Name ?? "Utilisateur",
            MaskedEmail = MaskEmail(invitation.Email),
            ExpiresAt = invitation.ExpiresAt
        };
    }

    public async Task<InvitationAcceptResult> AcceptViaIdpAsync(
        string token, 
        string idpEmail, 
        CancellationToken ct = default)
    {
        var invitation = await _db.Invitations
            .Include(i => i.Company)
            .Include(i => i.Role)
            .Where(i => i.Token == token && i.DeletedAt == null)
            .FirstOrDefaultAsync(ct);
        
        if (invitation == null)
            return InvitationAcceptResult.NotFound();
        
        if (invitation.Status != InvitationStatus.Pending)
            return InvitationAcceptResult.AlreadyUsed();
        
        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await _db.SaveChangesAsync(ct);
            return InvitationAcceptResult.Expired();
        }
        
        // Vérification critique : l'email IdP doit correspondre à l'invitation
        if (!string.Equals(invitation.Email, idpEmail, StringComparison.OrdinalIgnoreCase))
            return InvitationAcceptResult.EmailMismatch(invitation.Email, idpEmail);
        
        // Marquer l'invitation comme acceptée
        invitation.Status = InvitationStatus.Accepted;
        
        // Lier l'utilisateur à l'employé si nécessaire
        var user = await _db.Users
            .Where(u => u.Email == idpEmail.ToLowerInvariant())
            .FirstOrDefaultAsync(ct);
        
        if (user != null && invitation.EmployeeId.HasValue)
        {
            user.EmployeeId = invitation.EmployeeId.Value;
            
            // Assigner le rôle
            var existingRole = await _db.UsersRoles
                .Where(ur => ur.UserId == user.Id && ur.RoleId == invitation.RoleId)
                .FirstOrDefaultAsync(ct);
            
            if (existingRole == null)
            {
                _db.UsersRoles.Add(new UsersRoles
                {
                    UserId = user.Id,
                    RoleId = invitation.RoleId
                });
            }
        }
        
        await _db.SaveChangesAsync(ct);
        
        return InvitationAcceptResult.Success();
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "***" + email[at..];
        return email[0] + "***" + email[(at - 1)..];
    }
}