using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;
using Payzen.Application.DTOs.Referentiel;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Payroll;
using Payzen.Domain.Entities.Payroll.Referentiel;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Payroll;

// ════════════════════════════════════════════════════════════
// PAY COMPONENT SERVICE
// ════════════════════════════════════════════════════════════

public class PayComponentService : IPayComponentService
{
    private readonly AppDbContext _db;
    public PayComponentService(AppDbContext db) => _db = db;

    public async Task<ServiceResult<IEnumerable<PayComponentReadDto>>> GetAllAsync(
        bool? isActive, CancellationToken ct = default)
    {
        var q = _db.PayComponents.Where(pc => pc.DeletedAt == null).AsQueryable();
        if (isActive.HasValue)
            q = q.Where(pc => pc.IsActive == isActive.Value);
        var list = await q.OrderBy(pc => pc.SortOrder).ThenBy(pc => pc.Code).ToListAsync(ct);
        return ServiceResult<IEnumerable<PayComponentReadDto>>.Ok(list.Select(Map));
    }

    public async Task<ServiceResult<IEnumerable<PayComponentReadDto>>> GetEffectiveAsync(DateTime? asOf, CancellationToken ct = default)
    {
        var d = asOf ?? DateTime.UtcNow;
        var q = _db.PayComponents.Where(pc => pc.DeletedAt == null && pc.IsActive && pc.ValidFrom <= d && (pc.ValidTo == null || pc.ValidTo >= d));
        var list = await q.OrderBy(pc => pc.SortOrder).ThenBy(pc => pc.Code).ToListAsync(ct);
        return ServiceResult<IEnumerable<PayComponentReadDto>>.Ok(list.Select(Map));
    }

    public async Task<ServiceResult<PayComponentReadDto>> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var pc = await _db.PayComponents.FirstOrDefaultAsync(p => p.Code == code && p.DeletedAt == null, ct);
        return pc == null ? ServiceResult<PayComponentReadDto>.Fail("Composante introuvable.") : ServiceResult<PayComponentReadDto>.Ok(Map(pc));
    }

    public async Task<ServiceResult<PayComponentReadDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var pc = await _db.PayComponents.FindAsync(new object[] { id }, ct);
        return pc == null
            ? ServiceResult<PayComponentReadDto>.Fail("Composante introuvable.")
            : ServiceResult<PayComponentReadDto>.Ok(Map(pc));
    }

    public async Task<ServiceResult<PayComponentReadDto>> CreateAsync(
        PayComponentWriteDto dto, int createdBy, CancellationToken ct = default)
    {
        if (await _db.PayComponents.AnyAsync(pc => pc.Code == dto.Code && pc.DeletedAt == null, ct))
            return ServiceResult<PayComponentReadDto>.Fail($"Un PayComponent avec le code '{dto.Code}' existe déjà.");

        var pc = new PayComponent
        {
            Code = dto.Code,
            NameFr = dto.NameFr,
            NameAr = dto.NameAr,
            NameEn = dto.NameEn,
            Type = dto.Type,
            IsTaxable = dto.IsTaxable,
            IsSocial = dto.IsSocial,
            IsCIMR = dto.IsCIMR,
            ExemptionLimit = dto.ExemptionLimit,
            IsRegulated = dto.IsRegulated,
            ValidFrom = dto.ValidFrom ?? DateTime.UtcNow,
            ValidTo = dto.ValidTo,
            SortOrder = dto.SortOrder ?? 0,
            IsActive = true,
            CreatedBy = createdBy
        };
        _db.PayComponents.Add(pc);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<PayComponentReadDto>.Ok(Map(pc));
    }

    public async Task<ServiceResult<PayComponentReadDto>> UpdateAsync(
        int id, PayComponentWriteDto dto, int updatedBy, CancellationToken ct = default)
    {
        var pc = await _db.PayComponents.FindAsync(new object[] { id }, ct);
        if (pc == null)
            return ServiceResult<PayComponentReadDto>.Fail("Composante introuvable.");

        pc.NameFr = dto.NameFr;
        pc.NameAr = dto.NameAr;
        pc.NameEn = dto.NameEn;
        pc.Type = dto.Type;
        pc.IsTaxable = dto.IsTaxable;
        pc.IsSocial = dto.IsSocial;
        pc.IsCIMR = dto.IsCIMR;
        pc.ExemptionLimit = dto.ExemptionLimit;
        pc.IsRegulated = dto.IsRegulated;
        pc.ValidTo = dto.ValidTo;
        pc.SortOrder = dto.SortOrder ?? pc.SortOrder;
        pc.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<PayComponentReadDto>.Ok(Map(pc));
    }

    public async Task<ServiceResult<PayComponentReadDto>> NewVersionAsync(int id, int userId, CancellationToken ct = default)
    {
        var src = await _db.PayComponents.FindAsync(new object[] { id }, ct);
        if (src == null)
            return ServiceResult<PayComponentReadDto>.Fail("Composante introuvable.");
        var next = new PayComponent { Code = src.Code, NameFr = src.NameFr, NameAr = src.NameAr, NameEn = src.NameEn, Type = src.Type, IsTaxable = src.IsTaxable, IsSocial = src.IsSocial, IsCIMR = src.IsCIMR, ExemptionLimit = src.ExemptionLimit, IsRegulated = src.IsRegulated, ValidFrom = DateTime.UtcNow, ValidTo = null, SortOrder = src.SortOrder, IsActive = true, CreatedBy = userId };
        _db.PayComponents.Add(next);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<PayComponentReadDto>.Ok(Map(next));
    }

    public async Task<ServiceResult<PayComponentReadDto>> DeactivateAsync(int id, int userId, CancellationToken ct = default)
    {
        var pc = await _db.PayComponents.FindAsync(new object[] { id }, ct);
        if (pc == null)
            return ServiceResult<PayComponentReadDto>.Fail("Composante introuvable.");
        pc.IsActive = false;
        pc.UpdatedBy = userId;
        pc.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<PayComponentReadDto>.Ok(Map(pc));
    }

    public async Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var pc = await _db.PayComponents.FindAsync(new object[] { id }, ct);
        if (pc == null)
            return ServiceResult.Fail("Composante introuvable.");
        pc.DeletedAt = DateTimeOffset.UtcNow;
        pc.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    private static PayComponentReadDto Map(PayComponent pc) => new()
    {
        Id = pc.Id,
        Code = pc.Code,
        NameFr = pc.NameFr,
        NameAr = pc.NameAr,
        NameEn = pc.NameEn,
        Type = pc.Type,
        IsTaxable = pc.IsTaxable,
        IsSocial = pc.IsSocial,
        IsCIMR = pc.IsCIMR,
        ExemptionLimit = pc.ExemptionLimit,
        IsActive = pc.IsActive
    };
}

// ════════════════════════════════════════════════════════════
// REFERENTIEL PAYROLL SERVICE
// Gère : éléments, règles d'exonération, paramètres légaux
// ════════════════════════════════════════════════════════════

public class ReferentielPayrollService : IReferentielPayrollService
{
    private readonly AppDbContext _db;
    public ReferentielPayrollService(AppDbContext db) => _db = db;

    // ── Éléments ──────────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<ReferentielElementListDto>>> GetElementsAsync(
        bool? isActive, CancellationToken ct = default)
    {
        var q = _db.ReferentielElements
            .Include(re => re.Category)
            .Include(re => re.Rules)
            .AsQueryable();
        if (isActive.HasValue)
            q = q.Where(re => re.IsActive == isActive.Value);

        var list = await q.OrderBy(re => re.Category.SortOrder).ThenBy(re => re.Name).ToListAsync(ct);
        return ServiceResult<IEnumerable<ReferentielElementListDto>>.Ok(list.Select(re => new ReferentielElementListDto
        {
            Id = re.Id,
            Code = re.Code,
            Name = re.Name,
            CategoryName = re.Category.Name,
            DefaultFrequency = re.DefaultFrequency,
            Status = re.Status,
            IsActive = re.IsActive,
            HasConvergence = re.HasConvergence,
            RuleCount = re.Rules.Count
        }));
    }

    public async Task<ServiceResult<ReferentielElementDto>> GetElementByIdAsync(int id, CancellationToken ct = default)
    {
        var re = await _db.ReferentielElements
            .Include(re => re.Category)
            .Include(re => re.Rules).ThenInclude(r => r.Authority)
            .Include(re => re.Rules).ThenInclude(r => r.Cap)
            .Include(re => re.Rules).ThenInclude(r => r.Percentage)
            .Include(re => re.Rules).ThenInclude(r => r.Formula).ThenInclude(f => f!.Parameter)
            .FirstOrDefaultAsync(re => re.Id == id, ct);
        if (re == null)
            return ServiceResult<ReferentielElementDto>.Fail("Élément introuvable.");

        return ServiceResult<ReferentielElementDto>.Ok(new ReferentielElementDto
        {
            Id = re.Id,
            Code = re.Code,
            Name = re.Name,
            CategoryId = re.CategoryId,
            CategoryName = re.Category.Name,
            Description = re.Description,
            DefaultFrequency = re.DefaultFrequency,
            Status = re.Status,
            IsActive = re.IsActive,
            Rules = re.Rules.Select(r => new ElementRuleDto
            {
                Id = r.Id,
                ElementId = r.ElementId,
                AuthorityId = r.AuthorityId,
                AuthorityName = r.Authority.Name,
                ExemptionType = r.ExemptionType,
                SourceRef = r.SourceRef,
                EffectiveFrom = r.EffectiveFrom,
                EffectiveTo = r.EffectiveTo,
                IsActive = r.IsActive()
            }).ToList()
        });
    }

    public async Task<ServiceResult<ReferentielElementDto>> CreateElementAsync(
        CreateReferentielElementDto dto, int createdBy, CancellationToken ct = default)
    {
        var re = new ReferentielElement
        {
            Code = dto.Code,
            Name = dto.Name,
            CategoryId = dto.CategoryId,
            Description = dto.Description,
            DefaultFrequency = dto.DefaultFrequency,
            Status = dto.Status,
            IsActive = true,
            CreatedBy = createdBy
        };
        _db.ReferentielElements.Add(re);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(re).Reference(r => r.Category).LoadAsync(ct);
        return ServiceResult<ReferentielElementDto>.Ok(new ReferentielElementDto
        {
            Id = re.Id,
            Code = re.Code,
            Name = re.Name,
            CategoryId = re.CategoryId,
            CategoryName = re.Category.Name,
            Description = re.Description,
            DefaultFrequency = re.DefaultFrequency,
            Status = re.Status,
            IsActive = re.IsActive
        });
    }

    public async Task<ServiceResult<ReferentielElementDto>> UpdateElementAsync(
        int id, UpdateReferentielElementDto dto, int updatedBy, CancellationToken ct = default)
    {
        var re = await _db.ReferentielElements.Include(re => re.Category).FirstOrDefaultAsync(re => re.Id == id, ct);
        if (re == null)
            return ServiceResult<ReferentielElementDto>.Fail("Élément introuvable.");

        if (dto.Name != null)
            re.Name = dto.Name;
        if (dto.Description != null)
            re.Description = dto.Description;
        re.IsActive = dto.IsActive;
        re.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);

        return ServiceResult<ReferentielElementDto>.Ok(new ReferentielElementDto
        {
            Id = re.Id,
            Code = re.Code,
            Name = re.Name,
            CategoryId = re.CategoryId,
            CategoryName = re.Category.Name,
            Description = re.Description,
            DefaultFrequency = re.DefaultFrequency,
            Status = re.Status,
            IsActive = re.IsActive
        });
    }

    public async Task<ServiceResult> DeleteElementAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var re = await _db.ReferentielElements.FindAsync(new object[] { id }, ct);
        if (re == null)
            return ServiceResult.Fail("Élément introuvable.");
        re.DeletedAt = DateTimeOffset.UtcNow;
        re.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── Règles ────────────────────────────────────────────────────────────────

    public async Task<ServiceResult<ElementRuleDto>> CreateRuleAsync(
        CreateElementRuleDto dto, int createdBy, CancellationToken ct = default)
    {
        var rule = new ElementRule
        {
            ElementId = dto.ElementId,
            AuthorityId = dto.AuthorityId,
            ExemptionType = dto.ExemptionType,
            RuleDetails = dto.RuleDetails ?? "{}",
            SourceRef = dto.SourceRef,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            Status = dto.Status,
            CreatedBy = createdBy
        };
        _db.ElementRules.Add(rule);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(rule).Reference(r => r.Authority).LoadAsync(ct);
        return ServiceResult<ElementRuleDto>.Ok(new ElementRuleDto
        {
            Id = rule.Id,
            ElementId = rule.ElementId,
            AuthorityId = rule.AuthorityId,
            AuthorityName = rule.Authority.Name,
            ExemptionType = rule.ExemptionType,
            SourceRef = rule.SourceRef,
            EffectiveFrom = rule.EffectiveFrom,
            EffectiveTo = rule.EffectiveTo,
            IsActive = rule.IsActive()
        });
    }

    public async Task<ServiceResult<ElementRuleDto>> UpdateRuleAsync(
        int id, UpdateElementRuleDto dto, int updatedBy, CancellationToken ct = default)
    {
        var rule = await _db.ElementRules.Include(r => r.Authority).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (rule == null)
            return ServiceResult<ElementRuleDto>.Fail("Règle introuvable.");
        if (dto.EffectiveTo != null)
            rule.EffectiveTo = dto.EffectiveTo;
        if (dto.SourceRef != null)
            rule.SourceRef = dto.SourceRef;
        if (dto.Status != null)
            rule.Status = dto.Status.Value;
        rule.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<ElementRuleDto>.Ok(new ElementRuleDto
        {
            Id = rule.Id,
            ElementId = rule.ElementId,
            AuthorityId = rule.AuthorityId,
            AuthorityName = rule.Authority.Name,
            ExemptionType = rule.ExemptionType,
            SourceRef = rule.SourceRef,
            EffectiveFrom = rule.EffectiveFrom,
            EffectiveTo = rule.EffectiveTo,
            IsActive = rule.IsActive()
        });
    }

    public async Task<ServiceResult> DeleteRuleAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var rule = await _db.ElementRules.FindAsync(new object[] { id }, ct);
        if (rule == null)
            return ServiceResult.Fail("Règle introuvable.");
        rule.DeletedAt = DateTimeOffset.UtcNow;
        rule.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── Paramètres légaux ────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<LegalParameterDto>>> GetLegalParametersAsync(CancellationToken ct = default)
    {
        var list = await _db.LegalParameters
            .OrderBy(lp => lp.Code)
            .ThenByDescending(lp => lp.EffectiveFrom)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<LegalParameterDto>>.Ok(list.Select(MapParam));
    }

    public async Task<ServiceResult<LegalParameterDto>> CreateLegalParameterAsync(
        CreateLegalParameterDto dto, int createdBy, CancellationToken ct = default)
    {
        // Fermer la version précédente
        var prev = await _db.LegalParameters
            .Where(lp => lp.Code == dto.Code && lp.EffectiveTo == null)
            .FirstOrDefaultAsync(ct);
        if (prev != null)
        {
            prev.EffectiveTo = dto.EffectiveFrom.AddDays(-1);
            prev.UpdatedBy = createdBy;
        }

        var lp = new LegalParameter
        {
            Code = dto.Code ?? dto.Name.ToUpperInvariant().Replace(" ", "_"),
            Label = dto.Name,
            Value = dto.Value,
            Unit = dto.Unit,
            Source = dto.Source,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            CreatedBy = createdBy
        };
        _db.LegalParameters.Add(lp);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<LegalParameterDto>.Ok(MapParam(lp));
    }

    private static LegalParameterDto MapParam(LegalParameter lp) => new()
    {
        Id = lp.Id,
        Code = lp.Code,
        Name = lp.Label,
        Description = lp.Source,
        Source = lp.Source,
        Value = lp.Value,
        Unit = lp.Unit,
        EffectiveFrom = lp.EffectiveFrom,
        EffectiveTo = lp.EffectiveTo
    };

    // ── Ancienneté ────────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<AncienneteRateSetDto>>> GetRateSetsAsync(CancellationToken ct = default)
    {
        var list = await _db.AncienneteRateSets
            .Include(rs => rs.Rates)
            .Where(rs => rs.DeletedAt == null)
            .OrderByDescending(rs => rs.EffectiveFrom)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<AncienneteRateSetDto>>.Ok(list.Select(MapRateSet));
    }

    public async Task<ServiceResult<AncienneteRateSetDto>> CreateRateSetAsync(CreateAncienneteRateSetDto dto, int createdBy, CancellationToken ct = default)
    {
        var rs = new AncienneteRateSet
        {
            Code = $"CUSTOM_{Guid.NewGuid():N}"[..20],
            Name = dto.Name,
            IsLegalDefault = dto.IsLegalDefault,
            Source = dto.Source,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            CreatedBy = createdBy
        };
        foreach (var r in dto.Rates)
            rs.Rates.Add(new AncienneteRate { MinYears = r.MinYears, MaxYears = r.MaxYears, Rate = r.Rate, CreatedBy = createdBy });
        _db.AncienneteRateSets.Add(rs);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<AncienneteRateSetDto>.Ok(MapRateSet(rs));
    }

    public async Task<ServiceResult<AncienneteRateSetDto>> CustomizeCompanyRatesAsync(CustomizeCompanyRatesDto dto, int userId, CancellationToken ct = default)
    {
        var legal = await _db.AncienneteRateSets
            .Where(rs => rs.IsLegalDefault && rs.DeletedAt == null)
            .OrderByDescending(rs => rs.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        var rs = new AncienneteRateSet
        {
            Code = $"CO{dto.CompanyId}_{DateTime.Today:yyyyMM}",
            Name = $"Personnalisé entreprise {dto.CompanyId}",
            IsLegalDefault = false,
            CompanyId = dto.CompanyId,
            ClonedFromId = legal?.Id,
            EffectiveFrom = DateOnly.FromDateTime(DateTime.Today),
            CreatedBy = userId
        };
        foreach (var r in dto.Rates)
            rs.Rates.Add(new AncienneteRate { MinYears = r.MinYears, MaxYears = r.MaxYears, Rate = r.Rate, CreatedBy = userId });
        _db.AncienneteRateSets.Add(rs);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<AncienneteRateSetDto>.Ok(MapRateSet(rs));
    }

    private static AncienneteRateSetDto MapRateSet(AncienneteRateSet rs) => new()
    {
        Id = rs.Id,
        Name = rs.Name,
        IsLegalDefault = rs.IsLegalDefault,
        Source = rs.Source,
        EffectiveFrom = rs.EffectiveFrom,
        EffectiveTo = rs.EffectiveTo,
        IsActive = rs.DeletedAt == null,
        CompanyId = rs.CompanyId,
        ClonedFromId = rs.ClonedFromId,
        Rates = rs.Rates.Select(r => new AncienneteRateDto { Id = r.Id, MinYears = r.MinYears, MaxYears = r.MaxYears, Rate = r.Rate }).ToList()
    };
}
