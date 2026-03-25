using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<InvitationService> _logger;

    public InvitationService(AppDbContext db, IEmailService emailService, ILogger<InvitationService> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<string> CreateInvitationAsync(InviteAdminDto dto, CancellationToken ct = default)
    {
        var token = Guid.NewGuid().ToString("N");
        _logger.LogInformation(
            "CreateInvitationAsync(admin) Email={Email} CompanyId={CompanyId} RoleId={RoleId} Token={Token}",
            dto.Email,
            dto.CompanyId,
            dto.RoleId,
            token);
        
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
        _logger.LogInformation(
            "CreateEmployeeInvitationAsync Email={Email} CompanyId={CompanyId} RoleId={RoleId} EmployeeId={EmployeeId} Token={Token}",
            dto.Email,
            dto.CompanyId,
            dto.RoleId,
            dto.EmployeeId,
            token);
        
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

    public async Task<InvitationAcceptResult> AcceptViaIdpAsync(string token, int? jwtUserId, CancellationToken ct = default)
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

        var normalizedInvitationEmail = invitation.Email.Trim().ToLowerInvariant();

        // 1) Utilisateur du JWT (premier entra-login) — plus fiable que l’e-mail seul (casse, UPN IdP).
        Users? user = null;
        if (jwtUserId.HasValue)
        {
            var byId = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == jwtUserId.Value && u.DeletedAt == null, ct);
            if (byId != null)
            {
                if (!string.Equals(byId.Email.Trim(), invitation.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                    return InvitationAcceptResult.EmailMismatch(invitation.Email, byId.Email);

                user = byId;
            }
        }

        // 2) Secours : même e-mail que l’invitation (normalisé)
        if (user == null)
        {
            user = await _db.Users
                .FirstOrDefaultAsync(
                    u => u.Email.ToLower() == normalizedInvitationEmail && u.DeletedAt == null,
                    ct);
        }

        if (user == null)
            return InvitationAcceptResult.UserNotLinked();

        if (invitation.EmployeeId.HasValue)
        {
            user.EmployeeId = invitation.EmployeeId.Value;
        }
        else
        {
            var empForCompany = await _db.Employees
                .FirstOrDefaultAsync(
                    e => e.Email.ToLower() == normalizedInvitationEmail
                         && e.CompanyId == invitation.CompanyId
                         && e.DeletedAt == null,
                    ct);
            if (empForCompany != null)
            {
                user.EmployeeId = empForCompany.Id;
            }
            else
            {
                var activeStatus = await _db.Statuses
                    .FirstOrDefaultAsync(s => s.Code.ToLower() == "active" && s.DeletedAt == null, ct);
                if (activeStatus == null)
                    return InvitationAcceptResult.MissingActiveStatus();

                var (firstName, lastName) = DeriveNameFromEmail(user.Email);
                var newEmployee = new Payzen.Domain.Entities.Employee.Employee
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = user.Email.Trim(),
                    Phone = string.Empty,
                    CinNumber = "TEMP-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
                    DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
                    CompanyId = invitation.CompanyId,
                    StatusId = activeStatus.Id,
                    CreatedBy = user.Id
                };
                _db.Employees.Add(newEmployee);
                await _db.SaveChangesAsync(ct);
                user.EmployeeId = newEmployee.Id;
            }
        }

        invitation.Status = InvitationStatus.Accepted;

        var existingRole = await _db.UsersRoles
            .Where(ur => ur.UserId == user.Id && ur.RoleId == invitation.RoleId && ur.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (existingRole == null)
        {
            _db.UsersRoles.Add(new UsersRoles
            {
                UserId = user.Id,
                RoleId = invitation.RoleId,
                CreatedBy = 1
            });
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

    /// <summary>Prénom / nom provisoires à partir de l’e-mail (complétables en RH).</summary>
    private static (string FirstName, string LastName) DeriveNameFromEmail(string email)
    {
        var local = email.Trim().Split('@')[0];
        var parts = local
            .Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();
        var first = parts.Length > 0 ? parts[0] : "Utilisateur";
        var last = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : "Invité";
        const int max = 80;
        if (first.Length > max) first = first[..max];
        if (last.Length > max) last = last[..max];
        return (first, last);
    }
}