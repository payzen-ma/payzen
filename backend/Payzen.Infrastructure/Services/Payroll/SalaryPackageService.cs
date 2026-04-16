using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Payroll;
using Payzen.Domain.Entities.Payroll.Referentiel;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Payroll;

public class SalaryPackageService : ISalaryPackageService
{
    private readonly AppDbContext _db;

    public SalaryPackageService(AppDbContext db) => _db = db;

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<SalaryPackageReadDto>>> GetAllAsync(
        int? companyId,
        string? scope,
        string? status,
        CancellationToken ct = default
    )
    {
        var q = _db
            .SalaryPackages.Include(sp => sp.Company)
            .Include(sp => sp.BusinessSector)
            .Include(sp => sp.Items)
            .Where(sp => sp.DeletedAt == null)
            .AsQueryable();

        if (companyId.HasValue)
            q = q.Where(sp => sp.CompanyId == companyId || sp.CompanyId == null);

        if (!string.IsNullOrEmpty(status))
            q = q.Where(sp => sp.Status == status);

        if (!string.IsNullOrEmpty(scope))
        {
            if (scope == "global")
                q = q.Where(sp => sp.CompanyId == null);
            if (scope == "company")
                q = q.Where(sp => sp.CompanyId != null);
        }

        var list = await q.OrderBy(sp => sp.Name).ToListAsync(ct);
        return ServiceResult<IEnumerable<SalaryPackageReadDto>>.Ok(list.Select(Map));
    }

    public async Task<ServiceResult<IEnumerable<SalaryPackageReadDto>>> GetTemplatesAsync(
        CancellationToken ct = default
    )
    {
        var list = await _db
            .SalaryPackages.Where(sp => sp.DeletedAt == null && sp.CompanyId == null)
            .Include(sp => sp.Company)
            .Include(sp => sp.BusinessSector)
            .Include(sp => sp.Items)
            .OrderBy(sp => sp.Name)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<SalaryPackageReadDto>>.Ok(list.Select(Map));
    }

    public async Task<ServiceResult<SalaryPackageReadDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var sp = await _db
            .SalaryPackages.Include(sp => sp.Company)
            .Include(sp => sp.BusinessSector)
            .Include(sp => sp.Items)
                .ThenInclude(i => i.PayComponent)
            .Include(sp => sp.Items)
                .ThenInclude(i => i.ReferentielElement)
            .FirstOrDefaultAsync(sp => sp.Id == id, ct);
        return sp == null
            ? ServiceResult<SalaryPackageReadDto>.Fail("Package introuvable.")
            : ServiceResult<SalaryPackageReadDto>.Ok(Map(sp));
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public async Task<ServiceResult<SalaryPackageReadDto>> CreateAsync(
        SalaryPackageCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var resolvedSectorId = await ResolveBusinessSectorIdAsync(dto.BusinessSectorId ?? 0, dto.Category, ct);
        if (!resolvedSectorId.HasValue)
            return ServiceResult<SalaryPackageReadDto>.Fail(
                "Secteur d'activité invalide. Veuillez sélectionner un secteur valide."
            );

        var sp = new SalaryPackage
        {
            Name = dto.Name,
            Category = dto.Category,
            Description = dto.Description,
            BaseSalary = dto.BaseSalary,
            Status = "draft",
            CompanyId = dto.CompanyId,
            BusinessSectorId = resolvedSectorId.Value,
            TemplateType = dto.TemplateType ?? "OFFICIAL",
            RegulationVersion = dto.RegulationVersion ?? "MA_2025",
            AutoRulesJson = dto.AutoRules != null ? System.Text.Json.JsonSerializer.Serialize(dto.AutoRules) : null,
            CimrConfigJson = dto.CimrConfig != null ? System.Text.Json.JsonSerializer.Serialize(dto.CimrConfig) : null,
            CreatedBy = createdBy,
        };

        if (dto.Items?.Any() == true)
        {
            sp.Items = dto
                .Items.Select(
                    (i, idx) =>
                        new SalaryPackageItem
                        {
                            Label = i.Label,
                            DefaultValue = i.DefaultValue,
                            SortOrder = (i.SortOrder ?? 0) > 0 ? i.SortOrder!.Value : idx + 1,
                            Type = i.Type ?? "allowance",
                            IsTaxable = i.IsTaxable,
                            IsSocial = i.IsSocial,
                            IsCIMR = i.IsCIMR,
                            IsVariable = i.IsVariable,
                            ExemptionLimit = i.ExemptionLimit,
                            PayComponentId = i.PayComponentId,
                            ReferentielElementId = i.ReferentielElementId,
                            CreatedBy = createdBy,
                        }
                )
                .ToList();
        }

        _db.SalaryPackages.Add(sp);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<SalaryPackageReadDto>.Ok(Map(sp));
    }

    public async Task<ServiceResult<SalaryPackageReadDto>> UpdateAsync(
        int id,
        SalaryPackageUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var sp = await _db.SalaryPackages.Include(sp => sp.Items).FirstOrDefaultAsync(sp => sp.Id == id, ct);
        if (sp == null)
            return ServiceResult<SalaryPackageReadDto>.Fail("Package introuvable.");
        if (sp.IsLocked)
            return ServiceResult<SalaryPackageReadDto>.Fail("Package verrouillé — impossible de modifier.");

        if (dto.Name != null)
            sp.Name = dto.Name;
        if (dto.Description != null)
            sp.Description = dto.Description;
        if (dto.BaseSalary != null)
            sp.BaseSalary = dto.BaseSalary.Value;
        if (dto.Status != null)
            sp.Status = dto.Status;
        if (dto.AutoRules != null)
            sp.AutoRulesJson = System.Text.Json.JsonSerializer.Serialize(dto.AutoRules);
        if (dto.CimrConfig != null)
            sp.CimrConfigJson = System.Text.Json.JsonSerializer.Serialize(dto.CimrConfig);
        sp.UpdatedBy = updatedBy;

        // Remplacement complet des items si fournis
        if (dto.Items != null)
        {
            // Soft-delete anciens items
            foreach (var item in sp.Items)
            {
                item.DeletedAt = DateTimeOffset.UtcNow;
                item.DeletedBy = updatedBy;
            }
            // Nouveaux items
            var newItems = dto.Items.Select(
                (i, idx) =>
                    new SalaryPackageItem
                    {
                        SalaryPackageId = id,
                        Label = i.Label,
                        DefaultValue = i.DefaultValue,
                        SortOrder = (i.SortOrder ?? 0) > 0 ? i.SortOrder!.Value : idx + 1,
                        Type = i.Type ?? "allowance",
                        IsTaxable = i.IsTaxable,
                        IsSocial = i.IsSocial,
                        IsCIMR = i.IsCIMR,
                        IsVariable = i.IsVariable,
                        ExemptionLimit = i.ExemptionLimit,
                        PayComponentId = i.PayComponentId,
                        ReferentielElementId = i.ReferentielElementId,
                        CreatedBy = updatedBy,
                    }
            );
            _db.SalaryPackageItems.AddRange(newItems);
        }

        await _db.SaveChangesAsync(ct);
        return ServiceResult<SalaryPackageReadDto>.Ok(Map(sp));
    }

    public async Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var sp = await _db.SalaryPackages.FindAsync(new object[] { id }, ct);
        if (sp == null)
            return ServiceResult.Fail("Package introuvable.");
        if (sp.IsLocked)
            return ServiceResult.Fail("Package verrouillé — impossible de supprimer.");
        sp.DeletedAt = DateTimeOffset.UtcNow;
        sp.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── Clone / Duplicate ────────────────────────────────────────────────────

    public async Task<ServiceResult<SalaryPackageReadDto>> CloneAsync(
        int id,
        SalaryPackageCloneDto dto,
        int userId,
        CancellationToken ct = default
    )
    {
        var source = await _db.SalaryPackages.Include(sp => sp.Items).FirstOrDefaultAsync(sp => sp.Id == id, ct);
        if (source == null)
            return ServiceResult<SalaryPackageReadDto>.Fail("Package source introuvable.");

        var clone = new SalaryPackage
        {
            Name = dto.Name ?? $"{source.Name} (Copie)",
            Category = source.Category,
            Description = source.Description,
            BaseSalary = source.BaseSalary,
            Status = "draft",
            CompanyId = dto.CompanyId > 0 ? dto.CompanyId : source.CompanyId,
            BusinessSectorId = source.BusinessSectorId,
            TemplateType = source.TemplateType,
            RegulationVersion = source.RegulationVersion,
            AutoRulesJson = source.AutoRulesJson,
            CimrConfigJson = source.CimrConfigJson,
            OriginType = "CLONE",
            SourceTemplateId = source.Id,
            SourceTemplateVersion = source.Version,
            SourceTemplateNameSnapshot = source.Name,
            CopiedAt = DateTime.UtcNow,
            CreatedBy = userId,
            Items = source
                .Items.Select(i => new SalaryPackageItem
                {
                    Label = i.Label,
                    DefaultValue = i.DefaultValue,
                    SortOrder = i.SortOrder,
                    Type = i.Type,
                    IsTaxable = i.IsTaxable,
                    IsSocial = i.IsSocial,
                    IsCIMR = i.IsCIMR,
                    IsVariable = i.IsVariable,
                    ExemptionLimit = i.ExemptionLimit,
                    PayComponentId = i.PayComponentId,
                    ReferentielElementId = i.ReferentielElementId,
                    CreatedBy = userId,
                })
                .ToList(),
        };
        _db.SalaryPackages.Add(clone);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<SalaryPackageReadDto>.Ok(Map(clone));
    }

    public async Task<ServiceResult<SalaryPackageReadDto>> DuplicateAsync(
        int id,
        SalaryPackageDuplicateDto dto,
        int userId,
        CancellationToken ct = default
    )
    {
        var dupDto = new SalaryPackageCloneDto { Name = dto.Name, CompanyId = 0 };
        return await CloneAsync(id, dupDto, userId, ct);
    }

    public async Task<ServiceResult<SalaryPackageReadDto>> NewVersionAsync(
        int id,
        int userId,
        CancellationToken ct = default
    )
    {
        var sp = await _db
            .SalaryPackages.Include(sp => sp.Items)
            .FirstOrDefaultAsync(sp => sp.Id == id && sp.DeletedAt == null, ct);
        if (sp == null)
            return ServiceResult<SalaryPackageReadDto>.Fail("Package introuvable.");
        var next = new SalaryPackage
        {
            Name = sp.Name,
            Category = sp.Category,
            Description = sp.Description,
            BaseSalary = sp.BaseSalary,
            Status = "draft",
            CompanyId = sp.CompanyId,
            BusinessSectorId = sp.BusinessSectorId,
            TemplateType = sp.TemplateType,
            RegulationVersion = sp.RegulationVersion,
            AutoRulesJson = sp.AutoRulesJson,
            CimrConfigJson = sp.CimrConfigJson,
            Version = sp.Version + 1,
            SourceTemplateId = sp.SourceTemplateId ?? sp.Id,
            SourceTemplateVersion = sp.Version,
            CreatedBy = userId,
            Items = sp
                .Items.Where(i => i.DeletedAt == null)
                .Select(i => new SalaryPackageItem
                {
                    Label = i.Label,
                    DefaultValue = i.DefaultValue,
                    SortOrder = i.SortOrder,
                    Type = i.Type,
                    IsTaxable = i.IsTaxable,
                    IsSocial = i.IsSocial,
                    IsCIMR = i.IsCIMR,
                    IsVariable = i.IsVariable,
                    ExemptionLimit = i.ExemptionLimit,
                    PayComponentId = i.PayComponentId,
                    ReferentielElementId = i.ReferentielElementId,
                    CreatedBy = userId,
                })
                .ToList(),
        };
        _db.SalaryPackages.Add(next);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<SalaryPackageReadDto>.Ok(Map(next));
    }

    public async Task<ServiceResult<SalaryPackageReadDto>> PublishAsync(
        int id,
        int userId,
        CancellationToken ct = default
    )
    {
        var sp = await _db.SalaryPackages.FirstOrDefaultAsync(sp => sp.Id == id && sp.DeletedAt == null, ct);
        if (sp == null)
            return ServiceResult<SalaryPackageReadDto>.Fail("Package introuvable.");
        if (sp.Status != "draft")
            return ServiceResult<SalaryPackageReadDto>.Fail("Seul un brouillon peut être publié.");
        sp.Status = "published";
        sp.UpdatedBy = userId;
        sp.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _db.Entry(sp).Reference(s => s.Company).LoadAsync(ct);
        await _db.Entry(sp).Reference(s => s.BusinessSector).LoadAsync(ct);
        await _db.Entry(sp).Collection(s => s.Items).LoadAsync(ct);
        return ServiceResult<SalaryPackageReadDto>.Ok(Map(sp));
    }

    public async Task<ServiceResult<SalaryPackageReadDto>> DeprecateAsync(
        int id,
        int userId,
        CancellationToken ct = default
    )
    {
        var sp = await _db.SalaryPackages.FirstOrDefaultAsync(sp => sp.Id == id && sp.DeletedAt == null, ct);
        if (sp == null)
            return ServiceResult<SalaryPackageReadDto>.Fail("Package introuvable.");
        sp.Status = "deprecated";
        sp.UpdatedBy = userId;
        sp.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _db.Entry(sp).Reference(s => s.Company).LoadAsync(ct);
        await _db.Entry(sp).Reference(s => s.BusinessSector).LoadAsync(ct);
        await _db.Entry(sp).Collection(s => s.Items).LoadAsync(ct);
        return ServiceResult<SalaryPackageReadDto>.Ok(Map(sp));
    }

    public async Task<ServiceResult<IEnumerable<SalaryPackageItemReadDto>>> GetItemsAsync(
        int packageId,
        CancellationToken ct = default
    )
    {
        var items = await _db
            .SalaryPackageItems.Where(i => i.SalaryPackageId == packageId && i.DeletedAt == null)
            .OrderBy(i => i.SortOrder)
            .Include(i => i.PayComponent)
            .Include(i => i.ReferentielElement)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<SalaryPackageItemReadDto>>.Ok(
            items.Select(i => new SalaryPackageItemReadDto
            {
                Id = i.Id,
                PayComponentId = i.PayComponentId,
                PayComponentCode = i.PayComponent?.Code,
                ReferentielElementId = i.ReferentielElementId,
                ReferentielElementName = i.ReferentielElement?.Name,
                Label = i.Label,
                DefaultValue = i.DefaultValue,
                SortOrder = i.SortOrder,
                Type = i.Type,
                IsTaxable = i.IsTaxable,
                IsSocial = i.IsSocial,
                IsCIMR = i.IsCIMR,
                IsVariable = i.IsVariable,
                ExemptionLimit = i.ExemptionLimit,
            })
        );
    }

    public async Task<ServiceResult<SalaryPackageItemReadDto>> AddItemAsync(
        int packageId,
        SalaryPackageItemWriteDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var pkg = await _db.SalaryPackages.FindAsync(new object[] { packageId }, ct);
        if (pkg == null)
            return ServiceResult<SalaryPackageItemReadDto>.Fail("Package introuvable.");
        var maxOrder =
            await _db
                .SalaryPackageItems.Where(i => i.SalaryPackageId == packageId && i.DeletedAt == null)
                .MaxAsync(i => (int?)i.SortOrder, ct)
            ?? 0;
        var item = new SalaryPackageItem
        {
            SalaryPackageId = packageId,
            Label = dto.Label,
            DefaultValue = dto.DefaultValue,
            SortOrder = dto.SortOrder ?? maxOrder + 1,
            Type = dto.Type ?? "allowance",
            IsTaxable = dto.IsTaxable,
            IsSocial = dto.IsSocial,
            IsCIMR = dto.IsCIMR,
            IsVariable = dto.IsVariable,
            ExemptionLimit = dto.ExemptionLimit,
            PayComponentId = dto.PayComponentId,
            ReferentielElementId = dto.ReferentielElementId,
            CreatedBy = createdBy,
        };
        _db.SalaryPackageItems.Add(item);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(item).Reference(i => i.PayComponent).LoadAsync(ct);
        await _db.Entry(item).Reference(i => i.ReferentielElement).LoadAsync(ct);
        return ServiceResult<SalaryPackageItemReadDto>.Ok(
            new SalaryPackageItemReadDto
            {
                Id = item.Id,
                PayComponentId = item.PayComponentId,
                PayComponentCode = item.PayComponent?.Code,
                ReferentielElementId = item.ReferentielElementId,
                ReferentielElementName = item.ReferentielElement?.Name,
                Label = item.Label,
                DefaultValue = item.DefaultValue,
                SortOrder = item.SortOrder,
                Type = item.Type,
                IsTaxable = item.IsTaxable,
                IsSocial = item.IsSocial,
                IsCIMR = item.IsCIMR,
                IsVariable = item.IsVariable,
                ExemptionLimit = item.ExemptionLimit,
            }
        );
    }

    public async Task<ServiceResult<SalaryPackageItemReadDto>> UpdateItemAsync(
        int itemId,
        SalaryPackageItemWriteDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var item = await _db
            .SalaryPackageItems.Include(i => i.PayComponent)
            .Include(i => i.ReferentielElement)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.DeletedAt == null, ct);
        if (item == null)
            return ServiceResult<SalaryPackageItemReadDto>.Fail("Ligne introuvable.");
        item.Label = dto.Label;
        item.DefaultValue = dto.DefaultValue;
        if (dto.SortOrder.HasValue)
            item.SortOrder = dto.SortOrder.Value;
        if (dto.Type != null)
            item.Type = dto.Type;
        item.IsTaxable = dto.IsTaxable;
        item.IsSocial = dto.IsSocial;
        item.IsCIMR = dto.IsCIMR;
        item.IsVariable = dto.IsVariable;
        item.ExemptionLimit = dto.ExemptionLimit;
        item.PayComponentId = dto.PayComponentId;
        item.ReferentielElementId = dto.ReferentielElementId;
        item.UpdatedBy = updatedBy;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<SalaryPackageItemReadDto>.Ok(
            new SalaryPackageItemReadDto
            {
                Id = item.Id,
                PayComponentId = item.PayComponentId,
                PayComponentCode = item.PayComponent?.Code,
                ReferentielElementId = item.ReferentielElementId,
                ReferentielElementName = item.ReferentielElement?.Name,
                Label = item.Label,
                DefaultValue = item.DefaultValue,
                SortOrder = item.SortOrder,
                Type = item.Type,
                IsTaxable = item.IsTaxable,
                IsSocial = item.IsSocial,
                IsCIMR = item.IsCIMR,
                IsVariable = item.IsVariable,
                ExemptionLimit = item.ExemptionLimit,
            }
        );
    }

    public async Task<ServiceResult> DeleteItemAsync(int itemId, int deletedBy, CancellationToken ct = default)
    {
        var item = await _db.SalaryPackageItems.FirstOrDefaultAsync(i => i.Id == itemId && i.DeletedAt == null, ct);
        if (item == null)
            return ServiceResult.Fail("Ligne introuvable.");
        item.DeletedAt = DateTimeOffset.UtcNow;
        item.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── Assignments ──────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<SalaryPackageAssignmentReadDto>>> GetAssignmentsAsync(
        int employeeId,
        CancellationToken ct = default
    )
    {
        var list = await _db
            .SalaryPackageAssignments.Where(a => a.EmployeeId == employeeId && a.DeletedAt == null)
            .Include(a => a.SalaryPackage)
            .Include(a => a.Employee)
            .OrderByDescending(a => a.EffectiveDate)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<SalaryPackageAssignmentReadDto>>.Ok(list.Select(MapAssignment));
    }

    public async Task<ServiceResult<IEnumerable<SalaryPackageAssignmentReadDto>>> GetAllAssignmentsAsync(
        int? companyId,
        int? employeeId,
        CancellationToken ct = default
    )
    {
        var q = _db
            .SalaryPackageAssignments.Where(a => a.DeletedAt == null)
            .Include(a => a.SalaryPackage)
            .Include(a => a.Employee)
            .AsQueryable();
        if (employeeId.HasValue)
            q = q.Where(a => a.EmployeeId == employeeId.Value);
        if (companyId.HasValue)
            q = q.Where(a => a.SalaryPackage != null && a.SalaryPackage.CompanyId == companyId.Value);
        var list = await q.OrderByDescending(a => a.EffectiveDate).ToListAsync(ct);
        return ServiceResult<IEnumerable<SalaryPackageAssignmentReadDto>>.Ok(list.Select(MapAssignment));
    }

    public async Task<ServiceResult<SalaryPackageAssignmentReadDto>> GetAssignmentByIdAsync(
        int id,
        CancellationToken ct = default
    )
    {
        var a = await _db
            .SalaryPackageAssignments.Include(x => x.SalaryPackage)
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        return a == null
            ? ServiceResult<SalaryPackageAssignmentReadDto>.Fail("Assignation introuvable.")
            : ServiceResult<SalaryPackageAssignmentReadDto>.Ok(MapAssignment(a));
    }

    public async Task<ServiceResult<SalaryPackageAssignmentReadDto>> AssignAsync(
        SalaryPackageAssignmentCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        // Fermer l'assignation active précédente
        var active = await _db.SalaryPackageAssignments.FirstOrDefaultAsync(
            a => a.EmployeeId == dto.EmployeeId && a.EndDate == null,
            ct
        );
        if (active != null)
        {
            active.EndDate = dto.EffectiveDate.AddDays(-1);
            active.UpdatedBy = createdBy;
        }

        var sp = await _db.SalaryPackages.FindAsync(new object[] { dto.SalaryPackageId }, ct);
        var employeeSalaryId = await ResolveEmployeeSalaryIdAsync(dto, ct);
        if (!employeeSalaryId.HasValue)
            return ServiceResult<SalaryPackageAssignmentReadDto>.Fail(
                "Impossible d'affecter le package: aucun salaire employe valide n'a ete trouve pour ce contrat."
            );

        var assignment = new SalaryPackageAssignment
        {
            SalaryPackageId = dto.SalaryPackageId,
            EmployeeId = dto.EmployeeId,
            ContractId = dto.ContractId,
            EmployeeSalaryId = employeeSalaryId.Value,
            EffectiveDate = dto.EffectiveDate,
            PackageVersion = sp?.Version ?? 1,
            CreatedBy = createdBy,
        };
        _db.SalaryPackageAssignments.Add(assignment);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(assignment).Reference(a => a.SalaryPackage).LoadAsync(ct);
        await _db.Entry(assignment).Reference(a => a.Employee).LoadAsync(ct);
        return ServiceResult<SalaryPackageAssignmentReadDto>.Ok(MapAssignment(assignment));
    }

    private async Task<int?> ResolveEmployeeSalaryIdAsync(SalaryPackageAssignmentCreateDto dto, CancellationToken ct)
    {
        // 1) Salaire actif a la date d'effet pour le contrat cible
        var salaryId = await _db
            .EmployeeSalaries.AsNoTracking()
            .Where(s =>
                s.EmployeeId == dto.EmployeeId
                && s.ContractId == dto.ContractId
                && s.DeletedAt == null
                && s.EffectiveDate <= dto.EffectiveDate
                && (s.EndDate == null || s.EndDate >= dto.EffectiveDate)
            )
            .OrderByDescending(s => s.EffectiveDate)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct);

        if (salaryId.HasValue)
            return salaryId.Value;

        // 2) Fallback: dernier salaire connu sur le contrat
        salaryId = await _db
            .EmployeeSalaries.AsNoTracking()
            .Where(s => s.EmployeeId == dto.EmployeeId && s.ContractId == dto.ContractId && s.DeletedAt == null)
            .OrderByDescending(s => s.EffectiveDate)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct);

        if (salaryId.HasValue)
            return salaryId.Value;

        // 3) Fallback final: dernier salaire connu de l'employe
        salaryId = await _db
            .EmployeeSalaries.AsNoTracking()
            .Where(s => s.EmployeeId == dto.EmployeeId && s.DeletedAt == null)
            .OrderByDescending(s => s.EffectiveDate)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct);

        return salaryId;
    }

    public async Task<ServiceResult<SalaryPackageAssignmentReadDto>> UpdateAssignmentAsync(
        int id,
        SalaryPackageAssignmentUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var a = await _db
            .SalaryPackageAssignments.Include(a => a.SalaryPackage)
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (a == null)
            return ServiceResult<SalaryPackageAssignmentReadDto>.Fail("Assignation introuvable.");
        if (dto.EndDate != null)
            a.EndDate = dto.EndDate;
        a.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<SalaryPackageAssignmentReadDto>.Ok(MapAssignment(a));
    }

    public async Task<ServiceResult> RevokeAssignmentAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var a = await _db.SalaryPackageAssignments.FindAsync(new object[] { id }, ct);
        if (a == null)
            return ServiceResult.Fail("Assignation introuvable.");
        a.DeletedAt = DateTimeOffset.UtcNow;
        a.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── Preview ──────────────────────────────────────────────────────────────

    public async Task<ServiceResult<PayrollSummaryDto>> PreviewAsync(
        SalaryPreviewRequestDto dto,
        CancellationToken ct = default
    )
    {
        var baseSalary = dto.BaseSalary;
        var allowances = dto.Items.Where(i => i.Type == "allowance").Sum(i => i.DefaultValue);
        var bonuses = dto.Items.Where(i => i.Type == "bonus").Sum(i => i.DefaultValue);

        return ServiceResult<PayrollSummaryDto>.Ok(
            new PayrollSummaryDto
            {
                BaseSalary = baseSalary,
                Allowances = allowances,
                Bonuses = bonuses,
                GrossSalary = baseSalary + allowances + bonuses,
            }
        );
    }

    private async Task<int?> ResolveBusinessSectorIdAsync(int requestedSectorId, string? category, CancellationToken ct)
    {
        // 1) If a sector ID is provided, keep it only when it exists.
        if (requestedSectorId > 0)
        {
            var exists = await _db.BusinessSectors.AsNoTracking().AnyAsync(bs => bs.Id == requestedSectorId, ct);
            if (exists)
                return requestedSectorId;
        }

        // 2) Try to infer from category (exact match on sector name or code).
        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = category.Trim();
            var match = await _db
                .BusinessSectors.AsNoTracking()
                .Where(bs => bs.Name == normalized || bs.Code == normalized)
                .OrderBy(bs => bs.SortOrder)
                .Select(bs => (int?)bs.Id)
                .FirstOrDefaultAsync(ct);

            if (match.HasValue)
                return match.Value;
        }

        // 3) Safe fallback to a standard/default sector.
        var standard = await _db
            .BusinessSectors.AsNoTracking()
            .Where(bs => bs.IsStandard)
            .OrderBy(bs => bs.SortOrder)
            .Select(bs => (int?)bs.Id)
            .FirstOrDefaultAsync(ct);
        if (standard.HasValue)
            return standard.Value;

        // 4) Last fallback: any available sector.
        var existing = await _db
            .BusinessSectors.AsNoTracking()
            .OrderBy(bs => bs.SortOrder)
            .Select(bs => (int?)bs.Id)
            .FirstOrDefaultAsync(ct);

        if (existing.HasValue)
            return existing.Value;

        // 5) No business sector configured yet:
        // auto-create a default one so salary package creation does not fail.
        var defaultSector = new BusinessSector
        {
            Code = "GENERAL",
            Name = "Secteur général",
            IsStandard = true,
            SortOrder = 1,
        };

        _db.BusinessSectors.Add(defaultSector);
        await _db.SaveChangesAsync(ct);
        return defaultSector.Id;
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static SalaryPackageReadDto Map(SalaryPackage sp) =>
        new()
        {
            Id = sp.Id,
            Name = sp.Name,
            Category = sp.Category,
            Description = sp.Description,
            BaseSalary = sp.BaseSalary,
            Status = sp.Status,
            CompanyId = sp.CompanyId,
            CompanyName = sp.Company?.CompanyName,
            BusinessSectorId = sp.BusinessSectorId,
            BusinessSectorName = sp.BusinessSector?.Name,
            TemplateType = sp.TemplateType,
            RegulationVersion = sp.RegulationVersion,
            OriginType = sp.OriginType,
            SourceTemplateNameSnapshot = sp.SourceTemplateNameSnapshot,
            CopiedAt = sp.CopiedAt,
            Version = sp.Version,
            IsLocked = sp.IsLocked,
            Items = sp
                .Items?.Select(i => new SalaryPackageItemReadDto
                {
                    Id = i.Id,
                    PayComponentId = i.PayComponentId,
                    PayComponentCode = i.PayComponent?.Code,
                    ReferentielElementId = i.ReferentielElementId,
                    ReferentielElementName = i.ReferentielElement?.Name,
                    Label = i.Label,
                    DefaultValue = i.DefaultValue,
                    SortOrder = i.SortOrder,
                    Type = i.Type,
                    IsTaxable = i.IsTaxable,
                    IsSocial = i.IsSocial,
                    IsCIMR = i.IsCIMR,
                    IsVariable = i.IsVariable,
                    ExemptionLimit = i.ExemptionLimit,
                })
                .OrderBy(i => i.SortOrder)
                .ToList(),
        };

    private static SalaryPackageAssignmentReadDto MapAssignment(SalaryPackageAssignment a) =>
        new()
        {
            Id = a.Id,
            SalaryPackageId = a.SalaryPackageId,
            SalaryPackageName = a.SalaryPackage?.Name ?? string.Empty,
            EmployeeId = a.EmployeeId,
            EmployeeFullName = a.Employee != null ? $"{a.Employee.FirstName} {a.Employee.LastName}" : string.Empty,
            ContractId = a.ContractId,
            EmployeeSalaryId = a.EmployeeSalaryId,
            EffectiveDate = a.EffectiveDate,
            EndDate = a.EndDate,
            PackageVersion = a.PackageVersion,
        };
}
