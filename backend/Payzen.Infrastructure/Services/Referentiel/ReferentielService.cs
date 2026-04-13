using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Referentiel;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Referentiel;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Referentiel;

public class ReferentielService : IReferentielService
{
    private readonly AppDbContext _db;
    public ReferentielService(AppDbContext db) => _db = db;

    // ── Country ───────────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<CountryReadDto>>> GetCountriesAsync(CancellationToken ct = default)
    {
        var list = await _db.Countries.OrderBy(c => c.CountryName)
            .Select(c => MapCountry(c)).ToListAsync(ct);
        return ServiceResult<IEnumerable<CountryReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<CountryReadDto>> GetCountryByIdAsync(int id, CancellationToken ct = default)
    {
        var c = await _db.Countries.FindAsync(new object[] { id }, ct);
        if (c == null)
            return ServiceResult<CountryReadDto>.Fail("Pays introuvable.");
        return ServiceResult<CountryReadDto>.Ok(MapCountry(c));
    }

    public async Task<ServiceResult<CountryReadDto>> CreateCountryAsync(CountryCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        var c = new Country { CountryName = dto.CountryName, CountryCode = dto.CountryCode, CountryPhoneCode = dto.CountryPhoneCode, CreatedBy = createdBy };
        _db.Countries.Add(c);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<CountryReadDto>.Ok(MapCountry(c));
    }

    public async Task<ServiceResult<CountryReadDto>> UpdateCountryAsync(int id, CountryUpdateDto dto, int updatedBy, CancellationToken ct = default)
    {
        var c = await _db.Countries.FindAsync(new object[] { id }, ct);
        if (c == null)
            return ServiceResult<CountryReadDto>.Fail("Pays introuvable.");
        if (dto.CountryName != null)
            c.CountryName = dto.CountryName;
        if (dto.CountryCode != null)
            c.CountryCode = dto.CountryCode;
        if (dto.CountryPhoneCode != null)
            c.CountryPhoneCode = dto.CountryPhoneCode;
        c.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<CountryReadDto>.Ok(MapCountry(c));
    }

    public async Task<ServiceResult> DeleteCountryAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var c = await _db.Countries.FindAsync(new object[] { id }, ct);
        if (c == null)
            return ServiceResult.Fail("Pays introuvable.");
        c.DeletedAt = DateTimeOffset.UtcNow;
        c.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── City ──────────────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<CityReadDto>>> GetCitiesAsync(int? countryId, CancellationToken ct = default)
    {
        var q = _db.Cities.AsQueryable();
        if (countryId.HasValue)
            q = q.Where(c => c.CountryId == countryId.Value);
        var list = await q.OrderBy(c => c.CityName).Select(c => MapCity(c)).ToListAsync(ct);
        return ServiceResult<IEnumerable<CityReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<CityReadDto>> GetCityByIdAsync(int id, CancellationToken ct = default)
    {
        var c = await _db.Cities.FindAsync(new object[] { id }, ct);
        if (c == null)
            return ServiceResult<CityReadDto>.Fail("Ville introuvable.");
        return ServiceResult<CityReadDto>.Ok(MapCity(c));
    }

    public async Task<ServiceResult<CityReadDto>> CreateCityAsync(CityCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        var c = new City { CityName = dto.CityName, CountryId = dto.CountryId, CreatedBy = createdBy };
        _db.Cities.Add(c);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<CityReadDto>.Ok(MapCity(c));
    }

    public async Task<ServiceResult<CityReadDto>> UpdateCityAsync(int id, CityUpdateDto dto, int updatedBy, CancellationToken ct = default)
    {
        var c = await _db.Cities.FindAsync(new object[] { id }, ct);
        if (c == null)
            return ServiceResult<CityReadDto>.Fail("Ville introuvable.");
        if (dto.CityName != null)
            c.CityName = dto.CityName;
        c.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<CityReadDto>.Ok(MapCity(c));
    }

    public async Task<ServiceResult> DeleteCityAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var c = await _db.Cities.FindAsync(new object[] { id }, ct);
        if (c == null)
            return ServiceResult.Fail("Ville introuvable.");
        c.DeletedAt = DateTimeOffset.UtcNow;
        c.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── Nationality ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<NationalityReadDto>>> GetNationalitiesAsync(CancellationToken ct = default)
    {
        var list = await _db.Nationalities.OrderBy(n => n.Name)
            .Select(n => new NationalityReadDto { Id = n.Id, Name = n.Name }).ToListAsync(ct);
        return ServiceResult<IEnumerable<NationalityReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<NationalityReadDto>> CreateNationalityAsync(NationalityCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        var n = new Nationality { Name = dto.Name, CreatedBy = createdBy };
        _db.Nationalities.Add(n);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<NationalityReadDto>.Ok(new NationalityReadDto { Id = n.Id, Name = n.Name });
    }

    public async Task<ServiceResult> DeleteNationalityAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var n = await _db.Nationalities.FindAsync(new object[] { id }, ct);
        if (n == null)
            return ServiceResult.Fail("Nationalité introuvable.");
        n.DeletedAt = DateTimeOffset.UtcNow;
        n.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── MaritalStatus ──────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<MaritalStatusReadDto>>> GetMaritalStatusesAsync(CancellationToken ct = default)
    {
        var list = await _db.MaritalStatuses.OrderBy(m => m.NameFr)
            .Select(m => new MaritalStatusReadDto { Id = m.Id, Code = m.Code, NameFr = m.NameFr, NameAr = m.NameAr, NameEn = m.NameEn, IsActive = true }).ToListAsync(ct);
        return ServiceResult<IEnumerable<MaritalStatusReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<MaritalStatusReadDto>> CreateMaritalStatusAsync(MaritalStatusCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        var nameFr = (dto.NameFr ?? dto.Name ?? "").Trim();
        if (string.IsNullOrEmpty(nameFr))
            return ServiceResult<MaritalStatusReadDto>.Fail("Le libellé (NameFr ou Name) est requis.");
        var code = !string.IsNullOrWhiteSpace(dto.Code) ? dto.Code.Trim().ToUpperInvariant() : string.Concat(nameFr.ToUpperInvariant().Replace(" ", "_").Where(c => char.IsLetterOrDigit(c) || c == '_')).TrimStart('_');
        if (code.Length > 50)
            code = code[..50];
        if (string.IsNullOrEmpty(code))
            code = "MS_" + Guid.NewGuid().ToString("N")[..6];
        var entity = new MaritalStatus
        {
            Code = code,
            NameFr = nameFr,
            NameAr = dto.NameAr?.Trim() ?? nameFr,
            NameEn = dto.NameEn?.Trim() ?? nameFr,
            CreatedBy = createdBy,
        };
        _db.MaritalStatuses.Add(entity);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<MaritalStatusReadDto>.Ok(new MaritalStatusReadDto { Id = entity.Id, Code = entity.Code, NameFr = entity.NameFr, NameAr = entity.NameAr, NameEn = entity.NameEn, IsActive = true });
    }

    public async Task<ServiceResult<MaritalStatusReadDto>> GetMaritalStatusByIdAsync(int id, CancellationToken ct = default)
    {
        var m = await _db.MaritalStatuses.Where(x => x.DeletedAt == null).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m == null)
            return ServiceResult<MaritalStatusReadDto>.Fail("Statut marital introuvable.");
        return ServiceResult<MaritalStatusReadDto>.Ok(new MaritalStatusReadDto { Id = m.Id, Code = m.Code, NameFr = m.NameFr, NameAr = m.NameAr, NameEn = m.NameEn, IsActive = true });
    }

    public async Task<ServiceResult<MaritalStatusReadDto>> UpdateMaritalStatusAsync(int id, MaritalStatusUpdateDto dto, int updatedBy, CancellationToken ct = default)
    {
        var m = await _db.MaritalStatuses.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (m == null)
            return ServiceResult<MaritalStatusReadDto>.Fail("Statut marital introuvable.");
        if (dto.NameFr != null)
            m.NameFr = dto.NameFr;
        if (dto.NameAr != null)
            m.NameAr = dto.NameAr;
        if (dto.NameEn != null)
            m.NameEn = dto.NameEn;
        m.UpdatedBy = updatedBy;
        m.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<MaritalStatusReadDto>.Ok(new MaritalStatusReadDto { Id = m.Id, Code = m.Code, NameFr = m.NameFr, NameAr = m.NameAr, NameEn = m.NameEn, IsActive = true });
    }

    public async Task<ServiceResult> DeleteMaritalStatusAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var m = await _db.MaritalStatuses.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (m == null)
            return ServiceResult.Fail("Statut marital introuvable.");
        m.DeletedAt = DateTimeOffset.UtcNow;
        m.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── Gender ─────────────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<GenderReadDto>>> GetGendersAsync(CancellationToken ct = default)
    {
        var list = await _db.Genders.Where(g => g.DeletedAt == null).OrderBy(g => g.NameFr).Select(g => new GenderReadDto { Id = g.Id, Code = g.Code, NameFr = g.NameFr, NameAr = g.NameAr, NameEn = g.NameEn, IsActive = g.IsActive }).ToListAsync(ct);
        return ServiceResult<IEnumerable<GenderReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<GenderReadDto>> GetGenderByIdAsync(int id, CancellationToken ct = default)
    {
        var g = await _db.Genders.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (g == null)
            return ServiceResult<GenderReadDto>.Fail("Genre introuvable.");
        return ServiceResult<GenderReadDto>.Ok(new GenderReadDto { Id = g.Id, Code = g.Code, NameFr = g.NameFr, NameAr = g.NameAr, NameEn = g.NameEn, IsActive = g.IsActive });
    }

    public async Task<ServiceResult<GenderReadDto>> CreateGenderAsync(GenderCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        var code = !string.IsNullOrWhiteSpace(dto.Code) ? dto.Code.Trim().ToUpperInvariant() : ("G_" + (dto.NameFr ?? "").Trim().ToUpperInvariant().Replace(" ", "_"));
        var g = new Gender { Code = code, NameFr = dto.NameFr, NameAr = dto.NameAr ?? dto.NameFr, NameEn = dto.NameEn ?? dto.NameFr, IsActive = true, CreatedBy = createdBy };
        _db.Genders.Add(g);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<GenderReadDto>.Ok(new GenderReadDto { Id = g.Id, Code = g.Code, NameFr = g.NameFr, NameAr = g.NameAr, NameEn = g.NameEn, IsActive = g.IsActive });
    }

    public async Task<ServiceResult<GenderReadDto>> UpdateGenderAsync(int id, GenderUpdateDto dto, int updatedBy, CancellationToken ct = default)
    {
        var g = await _db.Genders.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (g == null)
            return ServiceResult<GenderReadDto>.Fail("Genre introuvable.");
        if (dto.NameFr != null)
            g.NameFr = dto.NameFr;
        if (dto.NameAr != null)
            g.NameAr = dto.NameAr;
        if (dto.NameEn != null)
            g.NameEn = dto.NameEn;
        if (dto.IsActive.HasValue)
            g.IsActive = dto.IsActive.Value;
        g.UpdatedBy = updatedBy;
        g.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<GenderReadDto>.Ok(new GenderReadDto { Id = g.Id, Code = g.Code, NameFr = g.NameFr, NameAr = g.NameAr, NameEn = g.NameEn, IsActive = g.IsActive });
    }

    public async Task<ServiceResult> DeleteGenderAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var g = await _db.Genders.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (g == null)
            return ServiceResult.Fail("Genre introuvable.");
        g.DeletedAt = DateTimeOffset.UtcNow;
        g.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── Status ─────────────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<StatusReadDto>>> GetStatusesAsync(CancellationToken ct = default)
    {
        var list = await _db.Statuses.Where(s => s.DeletedAt == null).OrderBy(s => s.NameFr).Select(s => new StatusReadDto { Id = s.Id, Code = s.Code, NameFr = s.NameFr, NameAr = s.NameAr, NameEn = s.NameEn, IsActive = s.IsActive, AffectsAccess = s.AffectsAccess, AffectsPayroll = s.AffectsPayroll, AffectsAttendance = s.AffectsAttendance }).ToListAsync(ct);
        return ServiceResult<IEnumerable<StatusReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<StatusReadDto>> GetStatusByIdAsync(int id, CancellationToken ct = default)
    {
        var s = await _db.Statuses.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (s == null)
            return ServiceResult<StatusReadDto>.Fail("Statut introuvable.");
        return ServiceResult<StatusReadDto>.Ok(new StatusReadDto { Id = s.Id, Code = s.Code, NameFr = s.NameFr, NameAr = s.NameAr, NameEn = s.NameEn, IsActive = s.IsActive, AffectsAccess = s.AffectsAccess, AffectsPayroll = s.AffectsPayroll, AffectsAttendance = s.AffectsAttendance });
    }

    public async Task<ServiceResult<StatusReadDto>> CreateStatusAsync(StatusCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        var code = !string.IsNullOrWhiteSpace(dto.Code) ? dto.Code.Trim().ToUpperInvariant() : ("ST_" + (dto.NameFr ?? "").Trim().ToUpperInvariant().Replace(" ", "_"));
        var s = new Status { Code = code, NameFr = dto.NameFr, NameAr = dto.NameAr ?? dto.NameFr, NameEn = dto.NameEn ?? dto.NameFr, IsActive = true, AffectsAccess = dto.AffectsAccess, AffectsPayroll = dto.AffectsPayroll, AffectsAttendance = dto.AffectsAttendance, CreatedBy = createdBy };
        _db.Statuses.Add(s);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<StatusReadDto>.Ok(new StatusReadDto { Id = s.Id, Code = s.Code, NameFr = s.NameFr, NameAr = s.NameAr, NameEn = s.NameEn, IsActive = s.IsActive, AffectsAccess = s.AffectsAccess, AffectsPayroll = s.AffectsPayroll, AffectsAttendance = s.AffectsAttendance });
    }

    public async Task<ServiceResult<StatusReadDto>> UpdateStatusAsync(int id, StatusUpdateDto dto, int updatedBy, CancellationToken ct = default)
    {
        var s = await _db.Statuses.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (s == null)
            return ServiceResult<StatusReadDto>.Fail("Statut introuvable.");
        if (dto.NameFr != null)
            s.NameFr = dto.NameFr;
        if (dto.NameAr != null)
            s.NameAr = dto.NameAr;
        if (dto.NameEn != null)
            s.NameEn = dto.NameEn;
        if (dto.IsActive.HasValue)
            s.IsActive = dto.IsActive.Value;
        if (dto.AffectsAccess.HasValue)
            s.AffectsAccess = dto.AffectsAccess.Value;
        if (dto.AffectsPayroll.HasValue)
            s.AffectsPayroll = dto.AffectsPayroll.Value;
        if (dto.AffectsAttendance.HasValue)
            s.AffectsAttendance = dto.AffectsAttendance.Value;
        s.UpdatedBy = updatedBy;
        s.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<StatusReadDto>.Ok(new StatusReadDto { Id = s.Id, Code = s.Code, NameFr = s.NameFr, NameAr = s.NameAr, NameEn = s.NameEn, IsActive = s.IsActive, AffectsAccess = s.AffectsAccess, AffectsPayroll = s.AffectsPayroll, AffectsAttendance = s.AffectsAttendance });
    }

    public async Task<ServiceResult> DeleteStatusAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var s = await _db.Statuses.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (s == null)
            return ServiceResult.Fail("Statut introuvable.");
        s.DeletedAt = DateTimeOffset.UtcNow;
        s.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── EducationLevel ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<EducationLevelReadDto>>> GetEducationLevelsAsync(CancellationToken ct = default)
    {
        var list = await _db.EducationLevels.Where(e => e.DeletedAt == null).OrderBy(e => e.LevelOrder).ThenBy(e => e.NameFr).Select(e => new EducationLevelReadDto { Id = e.Id, Code = e.Code, NameFr = e.NameFr, NameAr = e.NameAr, NameEn = e.NameEn, LevelOrder = e.LevelOrder, IsActive = e.IsActive }).ToListAsync(ct);
        return ServiceResult<IEnumerable<EducationLevelReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<EducationLevelReadDto>> GetEducationLevelByIdAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.EducationLevels.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (e == null)
            return ServiceResult<EducationLevelReadDto>.Fail("Niveau d'éducation introuvable.");
        return ServiceResult<EducationLevelReadDto>.Ok(new EducationLevelReadDto { Id = e.Id, Code = e.Code, NameFr = e.NameFr, NameAr = e.NameAr, NameEn = e.NameEn, LevelOrder = e.LevelOrder, IsActive = e.IsActive });
    }

    public async Task<ServiceResult<EducationLevelReadDto>> CreateEducationLevelAsync(EducationLevelCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        var code = !string.IsNullOrWhiteSpace(dto.Code) ? dto.Code.Trim().ToUpperInvariant() : ("ED_" + (dto.NameFr ?? "").Trim().ToUpperInvariant().Replace(" ", "_"));
        var e = new EducationLevel { Code = code, NameFr = dto.NameFr, NameAr = dto.NameAr ?? dto.NameFr, NameEn = dto.NameEn ?? dto.NameFr, LevelOrder = dto.LevelOrder, IsActive = true, CreatedBy = createdBy };
        _db.EducationLevels.Add(e);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EducationLevelReadDto>.Ok(new EducationLevelReadDto { Id = e.Id, Code = e.Code, NameFr = e.NameFr, NameAr = e.NameAr, NameEn = e.NameEn, LevelOrder = e.LevelOrder, IsActive = e.IsActive });
    }

    public async Task<ServiceResult<EducationLevelReadDto>> UpdateEducationLevelAsync(int id, EducationLevelUpdateDto dto, int updatedBy, CancellationToken ct = default)
    {
        var e = await _db.EducationLevels.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (e == null)
            return ServiceResult<EducationLevelReadDto>.Fail("Niveau d'éducation introuvable.");
        if (dto.NameFr != null)
            e.NameFr = dto.NameFr;
        if (dto.NameAr != null)
            e.NameAr = dto.NameAr;
        if (dto.NameEn != null)
            e.NameEn = dto.NameEn;
        if (dto.LevelOrder.HasValue)
            e.LevelOrder = dto.LevelOrder.Value;
        if (dto.IsActive.HasValue)
            e.IsActive = dto.IsActive.Value;
        e.UpdatedBy = updatedBy;
        e.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EducationLevelReadDto>.Ok(new EducationLevelReadDto { Id = e.Id, Code = e.Code, NameFr = e.NameFr, NameAr = e.NameAr, NameEn = e.NameEn, LevelOrder = e.LevelOrder, IsActive = e.IsActive });
    }

    public async Task<ServiceResult> DeleteEducationLevelAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var e = await _db.EducationLevels.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (e == null)
            return ServiceResult.Fail("Niveau d'éducation introuvable.");
        e.DeletedAt = DateTimeOffset.UtcNow;
        e.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── LegalContractType ──────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<LegalContractTypeReadDtos>>> GetLegalContractTypesAsync(CancellationToken ct = default)
    {
        var list = await _db.LegalContractTypes.Where(l => l.DeletedAt == null).OrderBy(l => l.Code).Select(l => new LegalContractTypeReadDtos { Id = l.Id, Code = l.Code, Name = l.Name }).ToListAsync(ct);
        return ServiceResult<IEnumerable<LegalContractTypeReadDtos>>.Ok(list);
    }

    public async Task<ServiceResult<LegalContractTypeReadDtos>> GetLegalContractTypeByIdAsync(int id, CancellationToken ct = default)
    {
        var l = await _db.LegalContractTypes.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (l == null)
            return ServiceResult<LegalContractTypeReadDtos>.Fail("Type de contrat légal introuvable.");
        return ServiceResult<LegalContractTypeReadDtos>.Ok(new LegalContractTypeReadDtos { Id = l.Id, Code = l.Code, Name = l.Name });
    }

    public async Task<ServiceResult<LegalContractTypeReadDtos>> CreateLegalContractTypeAsync(LegalContractTypeCreateDtos dto, int createdBy, CancellationToken ct = default)
    {
        var l = new LegalContractType { Code = dto.Code, Name = dto.Name, CreatedBy = createdBy };
        _db.LegalContractTypes.Add(l);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<LegalContractTypeReadDtos>.Ok(new LegalContractTypeReadDtos { Id = l.Id, Code = l.Code, Name = l.Name });
    }

    public async Task<ServiceResult<LegalContractTypeReadDtos>> UpdateLegalContractTypeAsync(int id, LegalContractTypeUpdateDtos dto, int updatedBy, CancellationToken ct = default)
    {
        var l = await _db.LegalContractTypes.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (l == null)
            return ServiceResult<LegalContractTypeReadDtos>.Fail("Type de contrat légal introuvable.");
        l.Code = dto.Code;
        l.Name = dto.Name;
        l.UpdatedBy = updatedBy;
        l.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<LegalContractTypeReadDtos>.Ok(new LegalContractTypeReadDtos { Id = l.Id, Code = l.Code, Name = l.Name });
    }

    public async Task<ServiceResult> DeleteLegalContractTypeAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var l = await _db.LegalContractTypes.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (l == null)
            return ServiceResult.Fail("Type de contrat légal introuvable.");
        l.DeletedAt = DateTimeOffset.UtcNow;
        l.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── StateEmploymentProgram ────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<StateEmploymentProgramReadDto>>> GetStateEmploymentProgramsAsync(CancellationToken ct = default)
    {
        var list = await _db.StateEmploymentPrograms
            .Where(s => s.DeletedAt == null)
            .Select(s => new StateEmploymentProgramReadDto { Id = s.Id, Name = s.Name }).ToListAsync(ct);
        return ServiceResult<IEnumerable<StateEmploymentProgramReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<StateEmploymentProgramReadDto>> GetStateEmploymentProgramByIdAsync(int id, CancellationToken ct = default)
    {
        var s = await _db.StateEmploymentPrograms
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (s == null)
            return ServiceResult<StateEmploymentProgramReadDto>.Fail("Programme introuvable.");
        return ServiceResult<StateEmploymentProgramReadDto>.Ok(new StateEmploymentProgramReadDto { Id = s.Id, Name = s.Name });
    }

    public async Task<ServiceResult<StateEmploymentProgramReadDto>> CreateStateEmploymentProgramAsync(StateEmploymentProgramCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        var s = new StateEmploymentProgram { Code = dto.Code, Name = dto.Name, CreatedBy = createdBy };
        _db.StateEmploymentPrograms.Add(s);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<StateEmploymentProgramReadDto>.Ok(new StateEmploymentProgramReadDto { Id = s.Id, Name = s.Name });
    }

    public async Task<ServiceResult<StateEmploymentProgramReadDto>> UpdateStateEmploymentProgramAsync(int id, StateEmploymentProgramUpdateDto dto, int updatedBy, CancellationToken ct = default)
    {
        var s = await _db.StateEmploymentPrograms.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (s == null)
            return ServiceResult<StateEmploymentProgramReadDto>.Fail("Programme introuvable.");
        if (dto.Name != null)
            s.Name = dto.Name;
        s.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<StateEmploymentProgramReadDto>.Ok(new StateEmploymentProgramReadDto { Id = s.Id, Name = s.Name });
    }

    public async Task<ServiceResult> DeleteStateEmploymentProgramAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var s = await _db.StateEmploymentPrograms.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (s == null)
            return ServiceResult.Fail("Programme introuvable.");
        s.DeletedAt = DateTimeOffset.UtcNow;
        s.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── OvertimeRateRule ──────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<OvertimeRateRuleReadDto>>> GetOvertimeRateRulesAsync(bool? isActive, CancellationToken ct = default)
    {
        var q = _db.OvertimeRateRules.AsQueryable();
        if (isActive.HasValue)
            q = q.Where(r => r.IsActive == isActive.Value);
        var list = await q.Select(r => MapOvertimeRule(r)).ToListAsync(ct);
        return ServiceResult<IEnumerable<OvertimeRateRuleReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<OvertimeRateRuleReadDto>> GetOvertimeRateRuleByIdAsync(int id, CancellationToken ct = default)
    {
        var r = await _db.OvertimeRateRules.FindAsync(new object[] { id }, ct);
        if (r == null)
            return ServiceResult<OvertimeRateRuleReadDto>.Fail("Règle introuvable.");
        return ServiceResult<OvertimeRateRuleReadDto>.Ok(MapOvertimeRule(r));
    }

    public async Task<ServiceResult<IEnumerable<string>>> GetOvertimeRateRuleCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _db.OvertimeRateRules
            .Where(r => r.DeletedAt == null && r.Category != null)
            .Select(r => r.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<string>>.Ok(categories);
    }

    public async Task<ServiceResult<OvertimeRateRuleReadDto>> CreateOvertimeRateRuleAsync(OvertimeRateRuleCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        var r = new OvertimeRateRule
        {
            Code = dto.Code,
            NameFr = dto.NameFr,
            NameAr = dto.NameAr,
            NameEn = dto.NameEn,
            AppliesTo = dto.AppliesTo,
            Multiplier = dto.Multiplier,
            Priority = dto.Priority,
            IsActive = dto.IsActive,
            TimeRangeType = dto.TimeRangeType,
            CumulationStrategy = dto.CumulationStrategy,
            CreatedBy = createdBy
        };
        _db.OvertimeRateRules.Add(r);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<OvertimeRateRuleReadDto>.Ok(MapOvertimeRule(r));
    }

    public async Task<ServiceResult<OvertimeRateRuleReadDto>> UpdateOvertimeRateRuleAsync(int id, OvertimeRateRuleUpdateDto dto, int updatedBy, CancellationToken ct = default)
    {
        var r = await _db.OvertimeRateRules.FindAsync(new object[] { id }, ct);
        if (r == null)
            return ServiceResult<OvertimeRateRuleReadDto>.Fail("Règle introuvable.");
        if (dto.Multiplier != null)
            r.Multiplier = dto.Multiplier.Value;
        if (dto.IsActive != null)
            r.IsActive = dto.IsActive.Value;
        if (dto.NameFr != null)
            r.NameFr = dto.NameFr;
        if (dto.NameAr != null)
            r.NameAr = dto.NameAr;
        if (dto.NameEn != null)
            r.NameEn = dto.NameEn;
        r.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<OvertimeRateRuleReadDto>.Ok(MapOvertimeRule(r));
    }

    public async Task<ServiceResult> DeleteOvertimeRateRuleAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var r = await _db.OvertimeRateRules.FindAsync(new object[] { id }, ct);
        if (r == null)
            return ServiceResult.Fail("Règle introuvable.");
        r.DeletedAt = DateTimeOffset.UtcNow;
        r.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static CountryReadDto MapCountry(Country c) => new()
    {
        Id = c.Id,
        CountryName = c.CountryName,
        CountryCode = c.CountryCode,
        CountryPhoneCode = c.CountryPhoneCode,
        CreatedAt = c.CreatedAt.DateTime
    };

    private static CityReadDto MapCity(City c) => new()
    {
        Id = c.Id,
        CityName = c.CityName,
        CountryId = c.CountryId,
        CreatedAt = c.CreatedAt.DateTime
    };

    private static OvertimeRateRuleReadDto MapOvertimeRule(OvertimeRateRule r) => new()
    {
        Id = r.Id,
        Code = r.Code,
        NameFr = r.NameFr,
        NameAr = r.NameAr,
        NameEn = r.NameEn,
        AppliesTo = r.AppliesTo,
        Multiplier = r.Multiplier,
        Priority = r.Priority,
        IsActive = r.IsActive,
        TimeRangeType = r.TimeRangeType,
        CumulationStrategy = r.CumulationStrategy,
        CreatedAt = r.CreatedAt.DateTime
    };
}
