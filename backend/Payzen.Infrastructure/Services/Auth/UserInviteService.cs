using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Auth;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Auth;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Auth;

public sealed class UserInviteService : IUserInviteService
{
    private readonly AppDbContext _db;

    public UserInviteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult> InviteUserAsync(UserInviteDto dto, int createdBy, CancellationToken ct = default)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == dto.CompanyId && c.DeletedAt == null, ct);

        if (company == null)
            return ServiceResult.Fail("Company introuvable.");

        var employee = await _db.Employees.FirstOrDefaultAsync(
            e => e.CompanyId == dto.CompanyId && e.Email == dto.Email && e.DeletedAt == null,
            ct
        );

        if (employee == null)
            return ServiceResult.Fail("Aucun employe trouve pour cet email et cette societe.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && u.DeletedAt == null, ct);

        if (user == null)
        {
            var baseUsername = dto.Email.Split('@')[0];

            if (baseUsername.Length < 3)
                baseUsername = baseUsername.PadRight(3, 'u');

            var usernameCandidate = baseUsername;
            var suffix = 1;

            while (await _db.Users.AnyAsync(u => u.Username == usernameCandidate && u.DeletedAt == null, ct))
            {
                usernameCandidate = $"{baseUsername}{suffix}";
                suffix++;
            }

            user = new Users
            {
                Username = usernameCandidate,
                Email = dto.Email,
                IsActive = true,
                EmployeeId = employee.Id,
                CreatedBy = createdBy,
                Source = company.AuthType == "C" ? "entra" : null,
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            user.EmployeeId = employee.Id;
            user.IsActive = true;
            if (company.AuthType == "C")
                user.Source = "entra";

            await _db.SaveChangesAsync(ct);
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == dto.RoleId && r.DeletedAt == null, ct);

        if (role == null)
            return ServiceResult.Fail("Role introuvable.");

        var relation = await _db
            .UsersRoles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id, ct);

        if (relation == null)
        {
            _db.UsersRoles.Add(
                new UsersRoles
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    CreatedBy = createdBy,
                }
            );
        }
        else
        {
            relation.DeletedAt = null;
            relation.DeletedBy = null;
        }

        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
