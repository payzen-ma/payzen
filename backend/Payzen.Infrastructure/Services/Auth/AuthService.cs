using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Auth;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Auth;
using Payzen.Domain.Enums.Auth;
using Payzen.Infrastructure.Persistence;
using Payzen.Infrastructure.Services.EventLog;

namespace Payzen.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwt;
    private readonly ILogger<AuthService> _logger;
    private readonly HashSet<string> _adminAllowedDomains;
    private readonly HashSet<string> _adminAllowedEmails;

    public AuthService(AppDbContext db, IJwtService jwt, ILogger<AuthService> logger, IConfiguration configuration)
    {
        _db = db;
        _jwt = jwt;
        _logger = logger;
        _adminAllowedDomains = configuration
            .GetSection("Auth:AdminPayzen:AllowedDomains")
            .Get<string[]>()?
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim().ToLowerInvariant())
            .ToHashSet() ?? new HashSet<string>();
        _adminAllowedEmails = configuration
            .GetSection("Auth:AdminPayzen:AllowedEmails")
            .Get<string[]>()?
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim().ToLowerInvariant())
            .ToHashSet() ?? new HashSet<string>();
    }

    // ── Login ────────────────────────────────────────────────────────────────
    // PayZen ne gère aucun mot de passe : authentification déléguée à Entra (Workforce/External ID).

    // ── Entra-login (Type C hybride) ─────────────────────────────────────────────
    //
    // Hypothèses (choix utilisateur validés) :
    // - B : si un Employee existe pour l'email Entra, on auto-active et on crée le compte si nécessaire.
    // - C : la stratégie d'auth est stockée dans Company.AuthType et doit valoir "C".
    public async Task<ServiceResult<LoginResponse>> LoginWithEntraAsync(EntraLoginRequestDto dto, CancellationToken ct = default)
    {
        var email = dto.Email.Trim();
        var externalId = dto.ExternalId.Trim();
        var maskedEmail = MaskEmail(email);
        _logger.LogInformation(
            "Entra login started for {EmailMasked}. ExternalIdLength={ExternalIdLength}",
            maskedEmail,
            externalId.Length);

        // 1) Vérifier si un employé correspond à cet email Entra
        var employee = await _db.Employees
            .Include(e => e.Company)
            .FirstOrDefaultAsync(e => e.Email == email && e.DeletedAt == null, ct);

        // Détermine si l'utilisateur peut être rattaché à un employé "Type C".
        // Sinon on bascule vers un compte visiteur standalone.
        var isEmployeeEligible =
            employee != null &&
            employee.Company != null &&
            string.Equals(employee.Company.AuthType, "C", System.StringComparison.OrdinalIgnoreCase);
        _logger.LogDebug(
            "Employee lookup completed for {EmailMasked}. Eligible={Eligible}, EmployeeId={EmployeeId}",
            maskedEmail,
            isEmployeeEligible,
            employee?.Id);

        if (isEmployeeEligible && !employee!.Company!.isActive)
            return ServiceResult<LoginResponse>.Fail("Votre entreprise est désactivée. Contactez l'administrateur.");

        // 2) Récupérer ou créer l'utilisateur lié à cet employé
        // Dev: on autorise aussi certains comptes Entra invités (#EXT#) pour auto-provisioning Admin Payzen.
        // Prod: conserver uniquement les domaines/identités internes approuvés.
        var isPayZenStaffEmail = IsPayZenStaffEmail(email);
        _logger.LogDebug(
            "Admin allowlist check completed for {EmailMasked}. IsStaff={IsStaff}",
            maskedEmail,
            isPayZenStaffEmail);

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null, ct);
        _logger.LogDebug(
            "User lookup completed for {EmailMasked}. Found={Found}",
            maskedEmail,
            user != null);

        if (user == null)
        {
            var baseUsername = email.Split('@')[0];
            if (baseUsername.Length < 3)
                baseUsername = baseUsername.PadRight(3, 'u');

            var usernameCandidate = baseUsername;
            var suffix = 1;
            while (await _db.Users.AnyAsync(u => u.Username == usernameCandidate && u.DeletedAt == null, ct))
            {
                usernameCandidate = $"{baseUsername}{suffix}";
                suffix++;
            }

            var source = isPayZenStaffEmail ? "payzenhr_entra"
                       : isEmployeeEligible ? "entra"
                       : "visitor_entra";

            user = new Users
            {
                Username = usernameCandidate,
                Email = email,
                IsActive = true,
                EmployeeId = isEmployeeEligible ? employee!.Id : null,
                ExternalId = externalId,
                Source = source,
                CreatedBy = 1
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "User created from Entra login. UserId={UserId}, Source={Source}, EmailMasked={EmailMasked}",
                user.Id,
                user.Source,
                maskedEmail);
        }
        else
        {
            // Auto-activation (si employé éligible) ou mise à jour simple (compte visiteur).
            user.ExternalId = externalId;
            // Important: lors du flux invitation, le front appelle `loginWithEntra` avant puis après `acceptViaIdp`.
            // `acceptViaIdp` peut lier `User.EmployeeId` même si la company n'est pas "C" (cas admin invitation).
            // On ne doit donc pas écraser un lien existant ici.
            user.Source = isPayZenStaffEmail ? "payzenhr_entra"
                        : isEmployeeEligible ? "entra"
                        : (user.EmployeeId.HasValue ? "entra" : "visitor_entra");

            if (isEmployeeEligible)
                user.EmployeeId = employee!.Id;
            user.IsActive = true;
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "User updated from Entra login. UserId={UserId}, Source={Source}, EmailMasked={EmailMasked}",
                user.Id,
                user.Source,
                maskedEmail);
        }

        // 3) RBAC : attribution automatique du rôle selon l'email.
        //    - Domaine autorisé (payzenhr.com + onmicrosoft.com en dev) → Admin Payzen
        //    - Employé éligible (Type C) → Employee
        //    - Sinon → Visitor
        const string adminPayZenRoleName = "Admin Payzen";

        var hasAdminPayZen = await _db.UsersRoles
            .Where(ur => ur.UserId == user.Id && ur.DeletedAt == null)
            .Join(_db.Roles.Where(r => r.DeletedAt == null),
                ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .AnyAsync(name => name == adminPayZenRoleName, ct);
        _logger.LogDebug(
            "Existing Admin Payzen role check for UserId={UserId}. HasRole={HasRole}",
            user.Id,
            hasAdminPayZen);

        if (!hasAdminPayZen)
        {
            string targetRoleName;
            if (isPayZenStaffEmail)
                targetRoleName = adminPayZenRoleName;
            else if (isEmployeeEligible)
                targetRoleName = "Employee";
            else
                targetRoleName = "Visitor";

            var targetRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.Name == targetRoleName && r.DeletedAt == null, ct);

            if (targetRole == null && targetRoleName == "Visitor")
                targetRole = await _db.Roles
                    .FirstOrDefaultAsync(r => r.Name == "Employee" && r.DeletedAt == null, ct);

            if (targetRole == null)
                return ServiceResult<LoginResponse>.Fail($"Rôle '{targetRoleName}' introuvable dans la base.");
            _logger.LogDebug(
                "Target role resolved for UserId={UserId}. Role={RoleName}, RoleId={RoleId}",
                user.Id,
                targetRoleName,
                targetRole.Id);

            var hasRole = await _db.UsersRoles
                .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == targetRole.Id && ur.DeletedAt == null, ct);

            if (!hasRole)
            {
                _db.UsersRoles.Add(new UsersRoles
                {
                    UserId = user.Id,
                    RoleId = targetRole.Id,
                    CreatedBy = 1
                });
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "Role assigned during Entra login. UserId={UserId}, RoleId={RoleId}",
                    user.Id,
                    targetRole.Id);
            }
        }

        // 4) Générer JWT + payload utilisateur (même format que /api/auth/login)
        var token = await _jwt.GenerateTokenAsync(user, ct);
        _logger.LogDebug("JWT generated for UserId={UserId}", user.Id);

        var roles = await _db.UsersRoles
            .Where(ur => ur.UserId == user.Id && ur.DeletedAt == null)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync(ct);

        var permissions = await _db.RolesPermissions
            .Where(rp => _db.UsersRoles
                .Where(ur => ur.UserId == user.Id && ur.DeletedAt == null)
                .Select(ur => ur.RoleId)
                .Contains(rp.RoleId) && rp.DeletedAt == null)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync(ct);

        var category = (isEmployeeEligible && employee?.CategoryId != null)
            ? await _db.EmployeeCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(ec => ec.Id == employee!.CategoryId && ec.DeletedAt == null, ct)
            : null;

        var companyIdForPayload = isEmployeeEligible ? (employee?.CompanyId ?? 0) : 0;
        if (companyIdForPayload == 0)
        {
            var fromAdminInvite = await TryResolveCompanyIdFromAcceptedCompanyAdminInvitationAsync(email, ct);
            if (fromAdminInvite.HasValue)
                companyIdForPayload = fromAdminInvite.Value;
        }

        var response = new LoginResponse
        {
            Message = "Connexion réussie.",
            Token = token,
            ExpiresAt = System.DateTime.UtcNow.AddHours(2),
            User = new Payzen.Domain.Entities.Auth.UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                FirstName = isEmployeeEligible ? (employee?.FirstName ?? "") : "",
                LastName = isEmployeeEligible ? (employee?.LastName ?? "") : "",
                Roles = roles,
                Permissions = permissions,
                EmployeeId = user.EmployeeId,
                EmployeeCategoryId = isEmployeeEligible ? employee?.CategoryId : null,
                Mode = category != null ? category.Mode.ToString() : null,
                IsCabinetExpert = isEmployeeEligible ? (employee?.Company?.IsCabinetExpert ?? false) : false,
                companyId = companyIdForPayload
            }
        };

        _logger.LogInformation(
            "Entra login succeeded. UserId={UserId}, RolesCount={RolesCount}, EmailMasked={EmailMasked}",
            user.Id,
            roles.Count,
            maskedEmail);
        return ServiceResult<LoginResponse>.Ok(response);
    }

    /// <summary>
    /// Admin société invité sans fiche employé : l'invitation acceptée porte le rôle « Admin » et le CompanyId.
    /// </summary>
    private async Task<int?> TryResolveCompanyIdFromAcceptedCompanyAdminInvitationAsync(string email, CancellationToken ct)
    {
        const string companyAdminRoleName = "Admin";
        var normalized = email.Trim().ToLowerInvariant();

        var invitationRow = await _db.Invitations
            .AsNoTracking()
            .Where(i => i.Email == normalized && i.Status == InvitationStatus.Accepted && i.DeletedAt == null)
            .Join(_db.Roles.Where(r => r.Name == companyAdminRoleName && r.DeletedAt == null),
                i => i.RoleId, r => r.Id, (i, _) => i)
            .OrderByDescending(i => i.Id)
            .Select(i => i.CompanyId)
            .FirstOrDefaultAsync(ct);

        if (invitationRow == 0)
            return null;

        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == invitationRow && c.DeletedAt == null, ct);

        if (company == null || !company.isActive)
            return null;
        if (!string.Equals(company.AuthType, "C", System.StringComparison.OrdinalIgnoreCase))
            return null;

        return invitationRow;
    }

    public async Task<ServiceResult<UserReadDto>> GetMeAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, ct);
        return user == null
            ? ServiceResult<UserReadDto>.Fail("Utilisateur introuvable.")
            : ServiceResult<UserReadDto>.Ok(MapUser(user));
    }

    // ── Users ────────────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<UserReadDto>>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await _db.Users
            .Where(u => u.DeletedAt == null)
            .OrderBy(u => u.Username)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<UserReadDto>>.Ok(users.Select(MapUser));
    }

    public async Task<ServiceResult<UserReadDto>> GetUserByIdAsync(int id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);
        return user == null
            ? ServiceResult<UserReadDto>.Fail("Utilisateur introuvable.")
            : ServiceResult<UserReadDto>.Ok(MapUser(user));
    }

    public async Task<ServiceResult<UserReadDto>> CreateUserAsync(UserCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        // Vérifie que l'email est unique parmi les utilisateurs actifs
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email && u.DeletedAt == null, ct))
            return ServiceResult<UserReadDto>.Fail("Un utilisateur avec cet email existe déjà.");

        // Vérifie que le username est unique parmi les utilisateurs actifs
        if (await _db.Users.AnyAsync(u => u.Username == dto.Username && u.DeletedAt == null, ct))
            return ServiceResult<UserReadDto>.Fail("Un utilisateur avec ce nom d'utilisateur existe déjà.");

        var user = new Users
        {
            Username = dto.Username,
            Email = dto.Email,
            IsActive = dto.IsActive,
            CreatedBy = createdBy
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<UserReadDto>.Ok(MapUser(user));
    }

    public async Task<ServiceResult<UserReadDto>> UpdateUserAsync(int id, UserUpdateDto dto, int updatedBy, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);
        if (user == null)
            return ServiceResult<UserReadDto>.Fail("Utilisateur introuvable.");

        // Vérifie que l'email est unique parmi les utilisateurs actifs (sauf pour l'utilisateur en cours de modification)
        if (dto.Email != null && await _db.Users.AnyAsync(u => u.Email ==
            dto.Email && u.Id != id && u.DeletedAt == null, ct))
            return ServiceResult<UserReadDto>.Fail("Un utilisateur avec cet email existe déjà.");

        // Vérifie que le username est unique parmi les utilisateurs actifs (sauf pour l'utilisateur en cours de modification)
        if (dto.Username != null && await _db.Users.AnyAsync(u => u.Username ==
            dto.Username && u.Id != id && u.DeletedAt == null, ct))
            return ServiceResult<UserReadDto>.Fail("Un utilisateur avec ce nom d'utilisateur existe déjà.");

        user.Email = dto.Email ?? user.Email;
        user.Username = dto.Username ?? user.Username;
        user.IsActive = dto.IsActive ?? user.IsActive;
        user.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<UserReadDto>.Ok(MapUser(user));
    }

    public async Task<ServiceResult> DeleteUserAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);
        if (user == null)
            return ServiceResult.Fail("Utilisateur introuvable.");
        if (user.Id == id)
            return ServiceResult.Fail("Vous ne pouvez pas supprimer votre propre compte.");
        var hasRoles = await _db.UsersRoles.AnyAsync(ur => ur.UserId == id && ur.DeletedAt == null, ct);
        if (hasRoles)
            return ServiceResult.Fail("Impossible de supprimer un utilisateur avec des rôles assignés. Révoquez d'abord les rôles.");
        user.DeletedAt = DateTimeOffset.UtcNow;
        user.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── Roles ────────────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<RoleReadDto>>> GetAllRolesAsync(CancellationToken ct = default)
    {
        var roles = await _db.Roles
            .Where(r => r.DeletedAt == null)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<RoleReadDto>>.Ok(roles.Select(MapRole));
    }

    public async Task<ServiceResult<RoleSummaryDto>> GetRoleSummaryAsync(int roleId, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId && r.DeletedAt == null, ct);
        if (role == null)
            return ServiceResult<RoleSummaryDto>.Fail("Rôle introuvable.");

        var userCount = await _db.UsersRoles.CountAsync(ur => ur.RoleId == roleId && ur.DeletedAt == null, ct);
        return ServiceResult<RoleSummaryDto>.Ok(new RoleSummaryDto
        {
            Id = role.Id,
            Name = role.Name,
            UserCount = userCount
        });
    }

    public async Task<ServiceResult<RoleReadDto>> CreateRoleAsync(RoleCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        if (await _db.Roles.AnyAsync(r => r.Name == dto.Name && r.DeletedAt == null, ct))
            return ServiceResult<RoleReadDto>.Fail("Un rôle avec ce nom existe déjà.");

        var role = new Roles { Name = dto.Name, Description = dto.Description, CreatedBy = createdBy };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<RoleReadDto>.Ok(MapRole(role));
    }

    public async Task<ServiceResult<RoleReadDto>> UpdateRoleAsync(int id, RoleUpdateDto dto, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null, ct);
        if (role == null)
            return ServiceResult<RoleReadDto>.Fail("Rôle introuvable.");
        role.Name = dto.Name ?? role.Name;
        role.Description = dto.Description ?? role.Description;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<RoleReadDto>.Ok(MapRole(role));
    }

    public async Task<ServiceResult> DeleteRoleAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null, ct);
        if (role == null)
            return ServiceResult.Fail("Rôle introuvable.");
        role.DeletedAt = DateTimeOffset.UtcNow;
        role.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── Permissions ──────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<PermissionReadDto>>> GetAllPermissionsAsync(CancellationToken ct = default)
    {
        var perms = await _db.Permissions
            .Where(p => p.DeletedAt == null)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<PermissionReadDto>>.Ok(perms.Select(MapPermission));
    }

    public async Task<ServiceResult<PermissionReadDto>> CreatePermissionAsync(PermissionCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        if (await _db.Permissions.AnyAsync(p => p.Name == dto.Name && p.DeletedAt == null, ct))
            return ServiceResult<PermissionReadDto>.Fail("Une permission avec ce nom existe déjà.");

        var perm = new Permissions
        {
            Name = dto.Name,
            Description = dto.Description,
            Resource = dto.Resource,
            Action = dto.Action,
            CreatedBy = createdBy
        };
        _db.Permissions.Add(perm);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<PermissionReadDto>.Ok(MapPermission(perm));
    }

    public async Task<ServiceResult<PermissionReadDto>> UpdatePermissionAsync(int id, PermissionUpdateDto dto, int updatedBy, CancellationToken ct = default)
    {
        var perm = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, ct);
        if (perm == null)
            return ServiceResult<PermissionReadDto>.Fail("Permission introuvable.");
        if (dto.Name != null && await _db.Permissions.AnyAsync(p => p.Name == dto.Name && p.Id != id && p.DeletedAt == null, ct))
            return ServiceResult<PermissionReadDto>.Fail("Une permission avec ce nom existe déjà.");
        perm.Name = dto.Name ?? perm.Name;
        perm.Description = dto.Description ?? perm.Description;
        perm.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<PermissionReadDto>.Ok(MapPermission(perm));
    }

    public async Task<ServiceResult> DeletePermissionAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var perm = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, ct);
        if (perm == null)
            return ServiceResult.Fail("Permission introuvable.");
        perm.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── Role ↔ Permission ────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<RolePermissionSimpleDto>>> GetPermissionsForRoleAsync(int roleId, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId && r.DeletedAt == null, ct);
        if (role == null)
            return ServiceResult<IEnumerable<RolePermissionSimpleDto>>.Fail("Role introuvable.");

        var perms = await _db.RolesPermissions
            .Where(rp => rp.RoleId == roleId && rp.DeletedAt == null)
            .Include(rp => rp.Permission)
            .Where(rp => rp.Permission.DeletedAt == null)
            .Select(rp => new RolePermissionSimpleDto
            {
                PermissionId = rp.PermissionId,
                PermissionName = rp.Permission.Name,
                PermissionDescription = rp.Permission.Description

            })
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<RolePermissionSimpleDto>>.Ok(perms);
    }

    public async Task<ServiceResult<RolePermissionReadDto>> AssignPermissionToRoleAsync(RolePermissionAssignDto dto, int createdBy, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == dto.RoleId && r.DeletedAt == null, ct);
        if (role == null)
            return ServiceResult<RolePermissionReadDto>.Fail("Rôle introuvable.");

        var perm = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == dto.PermissionId && p.DeletedAt == null, ct);
        if (perm == null)
            return ServiceResult<RolePermissionReadDto>.Fail("Permission introuvable.");

        if (await _db.RolesPermissions.AnyAsync(rp => rp.RoleId == dto.RoleId && rp.PermissionId == dto.PermissionId && rp.DeletedAt == null, ct))
            return ServiceResult<RolePermissionReadDto>.Fail("Cette permission est déjà assignée à ce rôle.");

        var rp = new RolesPermissions { RoleId = dto.RoleId, PermissionId = dto.PermissionId, CreatedBy = createdBy };
        _db.RolesPermissions.Add(rp);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(rp).Reference(x => x.Role).LoadAsync(ct);
        await _db.Entry(rp).Reference(x => x.Permission).LoadAsync(ct);

        return ServiceResult<RolePermissionReadDto>.Ok(new RolePermissionReadDto
        {
            Id = rp.Id,
            RoleId = rp.RoleId,
            PermissionId = rp.PermissionId,
            RoleName = rp.Role.Name,
            PermissionName = rp.Permission.Name,
            PermissionDescription = rp.Permission.Description,
            CreatedAt = rp.CreatedAt.DateTime
        });
    }

    public async Task<ServiceResult> BulkAssignPermissionsToRoleAsync(RolePermissionsBulkAssignDto dto, int createdBy, CancellationToken ct = default)
    {
        var roleExists = await _db.Roles
            .AsNoTracking()
            .AnyAsync(r => r.Id == dto.RoleId && r.DeletedAt == null, ct);

        if (!roleExists)
            return ServiceResult.Fail("Role not found");

        var existing = await _db.RolesPermissions
            .Where(rp => rp.RoleId == dto.RoleId)
            .ToListAsync(ct);

        // Tolère les payload front contenant des `null` dans la liste.
        // On filtre uniquement les IDs non-null et > 0.
        var permissionIds = dto.PermissionIds
            .Where(pid => pid.HasValue && pid.Value > 0)
            .Select(pid => pid!.Value)
            .Distinct()
            .ToList();

        // Sémantique "bulk-assign" côté UI : synchroniser les permissions du rôle.
        // - Réactiver celles demandées (DeletedAt != null)
        // - Soft-delete celles qui ne sont plus demandées
        var desiredSet = new HashSet<int>(permissionIds);

        foreach (var pid in permissionIds)
        {
            var relation = existing
                .FirstOrDefault(rp => rp.PermissionId == pid);

            if (relation == null)
            {
                _db.RolesPermissions.Add(new RolesPermissions
                {
                    RoleId = dto.RoleId,
                    PermissionId = pid,
                    CreatedBy = createdBy
                });
            }
            else if (relation.DeletedAt != null)
            {
                relation.DeletedAt = null;
                relation.UpdatedBy = createdBy;
                relation.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        foreach (var relation in existing)
        {
            if (!desiredSet.Contains(relation.PermissionId) && relation.DeletedAt == null)
            {
                // Révocation (soft-delete) : la permission n'est plus demandée pour ce rôle.
                relation.DeletedAt = DateTimeOffset.UtcNow;
                relation.DeletedBy = createdBy;
            }
        }

        await _db.SaveChangesAsync(ct);

        // Retourne un ServiceResult sans données, car l'interface attend Task<ServiceResult>
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RevokePermissionFromRoleAsync(int roleId, int permissionId, CancellationToken ct = default)
    {
        var rp = await _db.RolesPermissions.FirstOrDefaultAsync(r => r.RoleId == roleId && r.PermissionId == permissionId && r.DeletedAt == null, ct);
        if (rp == null)
            return ServiceResult.Fail("Association introuvable.");
        rp.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── User ↔ Role ──────────────────────────────────────────────────────────

    public async Task<ServiceResult<RoleUsersDto>> GetUsersInRoleAsync(int roleId, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId && r.DeletedAt == null, ct);
        if (role == null)
            return ServiceResult<RoleUsersDto>.Fail("Rôle introuvable.");

        var users = await _db.UsersRoles
            .Where(ur => ur.RoleId == roleId && ur.DeletedAt == null)
            .Include(ur => ur.User)
            .Where(ur => ur.User.DeletedAt == null)
            .Select(ur => new UserInRoleDto { UserId = ur.UserId, Username = ur.User.Username, Email = ur.User.Email })
            .ToListAsync(ct);

        return ServiceResult<RoleUsersDto>.Ok(new RoleUsersDto { RoleId = roleId, RoleName = role.Name, Users = users });
    }

    public async Task<ServiceResult<IEnumerable<UserRoleSimpleDto>>> GetRolesForUserAsync(int userId, CancellationToken ct = default)
    {
        var roles = await _db.UsersRoles
            .Where(ur => ur.UserId == userId && ur.DeletedAt == null)
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.DeletedAt == null)
            .Select(ur => new UserRoleSimpleDto { UserId = userId, RoleId = ur.RoleId, RoleName = ur.Role.Name })
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<UserRoleSimpleDto>>.Ok(roles);
    }

    public async Task<ServiceResult<UserRoleReadDto>> AssignRoleToUserAsync(UserRoleAssignDto dto, int createdBy, CancellationToken ct = default)
    {
        if (await _db.UsersRoles.AnyAsync(ur => ur.UserId == dto.UserId && ur.RoleId == dto.RoleId && ur.DeletedAt == null, ct))
            return ServiceResult<UserRoleReadDto>.Fail("Ce rôle est déjà assigné à cet utilisateur.");

        var ur = new UsersRoles { UserId = dto.UserId, RoleId = dto.RoleId, CreatedBy = createdBy };
        _db.UsersRoles.Add(ur);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(ur).Reference(x => x.User).LoadAsync(ct);
        await _db.Entry(ur).Reference(x => x.Role).LoadAsync(ct);

        return ServiceResult<UserRoleReadDto>.Ok(new UserRoleReadDto
        {
            Id = ur.Id,
            UserId = ur.UserId,
            RoleId = ur.RoleId,
            Username = ur.User.Username,
            UserEmail = ur.User.Email,
            RoleName = ur.Role.Name,
            RoleDescription = ur.Role.Description,
            CreatedAt = ur.CreatedAt.DateTime
        });
    }

    public async Task<ServiceResult> BulkAssignRolesToUserAsync(UserRolesBulkAssignDto dto, int createdBy, CancellationToken ct = default)
    {
        var existing = await _db.UsersRoles
            .Where(ur => ur.UserId == dto.UserId && ur.DeletedAt == null)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        var toAdd = dto.RoleIds.Except(existing).Select(rid => new UsersRoles
        {
            UserId = dto.UserId,
            RoleId = rid,
            CreatedBy = createdBy
        });
        _db.UsersRoles.AddRange(toAdd);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RevokeRoleFromUserAsync(int userId, int roleId, CancellationToken ct = default)
    {
        var ur = await _db.UsersRoles.FirstOrDefaultAsync(u => u.UserId == userId && u.RoleId == roleId && u.DeletedAt == null, ct);
        if (ur == null)
            return ServiceResult.Fail("Association introuvable.");
        ur.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RepalceRolesForUserAsync(
        UserRoleRepalceDto dto,
        int updatedBy,
        CancellationToken ct = default)
    {
        // 1. Validation user + role en parallèle
        var userTask = _db.Users
            .Where(u => u.Id == dto.UserId && u.DeletedAt == null)
            .Select(u => new { u.IsActive, u.EmployeeId })
            .FirstOrDefaultAsync(ct);

        var roleTask = _db.Roles
            .Where(r => r.Id == dto.RoleID && r.DeletedAt == null)
            .Select(r => new { r.Id, r.Name })
            .FirstOrDefaultAsync(ct);

        await Task.WhenAll(userTask, roleTask);

        var user = await userTask;
        var role = await roleTask;

        if (user == null)
            return ServiceResult.Fail("Utilisateur non trouvé");
        if (!user.IsActive)
            return ServiceResult.Fail("L'utilisateur est désactivé");
        if (role == null)
            return ServiceResult.Fail("Le rôle n'existe pas");

        // 2. Transaction explicite
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // 3. Charger tous les rôles actifs en une seule requête
            var currentRoles = await _db.UsersRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == dto.UserId && ur.DeletedAt == null)
                .ToListAsync(ct);

            var now = DateTimeOffset.UtcNow;

            // 4. Soft-delete en mémoire (pas d'await dans la boucle)
            foreach (var ur in currentRoles)
            {
                ur.DeletedAt = now;
                ur.DeletedBy = updatedBy;
            }

            // 5. Réactiver ou créer (une seule requête)
            var existing = await _db.UsersRoles
                .IgnoreQueryFilters() // si tu as des global query filters
                .FirstOrDefaultAsync(ur => ur.UserId == dto.UserId && ur.RoleId == dto.RoleID, ct);

            if (existing != null)
            {
                existing.DeletedAt = null;
                existing.DeletedBy = null;
                existing.UpdatedAt = now;
                existing.UpdatedBy = updatedBy;
            }
            else
            {
                _db.UsersRoles.Add(new UsersRoles
                {
                    UserId = dto.UserId,
                    RoleId = dto.RoleID,
                    CreatedAt = now,
                    CreatedBy = updatedBy
                });
            }

            await _db.SaveChangesAsync(ct);

            // 6. Logs après SaveChanges (DB cohérente avant de logger)
            /* if (user.EmployeeId.HasValue)
             {
                 var logTasks = currentRoles
                     .Where(ur => ur.Role != null)
                     .Select(ur => _employeeEventLogService.LogEventAsync(
                         user.EmployeeId.Value, "Role_Removed",
                         ur.Role.Name, ur.Role.Id, null, null, updatedBy))
                     .Append(_employeeEventLogService.LogEventAsync(
                         user.EmployeeId.Value, "Role_Assigned",
                         null, null, role.Name, role.Id, updatedBy));

                 await Task.WhenAll(logTasks);
             }*/ // Log is not implented yet

            await tx.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<ServiceResult<IEnumerable<UserRoleSimpleDto>>> GetRolesForEmployeeAsync(int employeeId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId && u.DeletedAt == null, ct);

        if (user == null)
            return ServiceResult<IEnumerable<UserRoleSimpleDto>>.Fail("Utilisateur non trouvé pour cet employé.");

        return await GetRolesForUserAsync(user.Id, ct);
    }

    public async Task<ServiceResult> AssignRolesToEmployeeAsync(int employeeId, IEnumerable<int> roleIds, int createdBy, CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId && u.DeletedAt == null, ct);

        if (user == null)
            return ServiceResult.Fail("Utilisateur non trouvé pour cet employé.");

        var dto = new UserRolesBulkAssignDto { UserId = user.Id, RoleIds = roleIds.ToList() };
        return await BulkAssignRolesToUserAsync(dto, createdBy, ct);
    }

    public async Task<ServiceResult> RevokeRoleFromEmployeeAsync(int employeeId, int roleId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId && u.DeletedAt == null, ct);

        if (user == null)
            return ServiceResult.Fail("Utilisateur non trouvé pour cet employé.");

        return await RevokeRoleFromUserAsync(user.Id, roleId, ct);
    }

    // ── Private mappers ──────────────────────────────────────────────────────

    private static UserReadDto MapUser(Users u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt.DateTime
    };

    private static RoleReadDto MapRole(Roles r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        CreatedAt = r.CreatedAt.DateTime
    };

    private static PermissionReadDto MapPermission(Permissions p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Resource = p.Resource,
        Action = p.Action,
        CreatedAt = p.CreatedAt.DateTime
    };

    private bool IsPayZenStaffEmail(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (_adminAllowedEmails.Contains(normalizedEmail))
            return true;

        return _adminAllowedDomains.Any(domain =>
            normalizedEmail.EndsWith(domain, StringComparison.OrdinalIgnoreCase));
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1)
            return "***";

        var local = email[..at];
        var domain = at + 1 < email.Length ? email[(at + 1)..] : "";
        var first = local[0];
        var last = local[^1];
        return $"{first}***{last}@{domain}";
    }
}
