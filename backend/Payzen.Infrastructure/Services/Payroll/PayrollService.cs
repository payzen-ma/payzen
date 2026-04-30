using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;
using Payzen.Application.Interfaces;
using Payzen.Application.Payroll;
using Payzen.Domain.Entities.Payroll;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Payroll;

/// <summary>
/// PayrollService — chef d'orchestre du calcul de paie.
///
/// Pipeline :
///   1. Charger toutes les données employé depuis DB → EmployeePayrollDto
///   2. Appeler PayrollCalculationEngine.CalculatePayroll(dto)
///   3. Mapper PayrollCalculationResult → entité PayrollResult
///   4. Persister PayrollResult + Primes + AuditSteps
/// </summary>
public class PayrollService : IPayrollService
{
    private readonly AppDbContext _db;
    private readonly PayrollCalculationEngine _engine;
    private readonly IWebHostEnvironment _env;
    private readonly ILlmService _llmService;
    private readonly IPayrollTaxSnapshotService _snapshotService;
    private readonly ILogger<PayrollService> _logger;

    public PayrollService(
        AppDbContext db,
        PayrollCalculationEngine engine,
        IWebHostEnvironment env,
        IPayrollTaxSnapshotService snapshotService,
        ILogger<PayrollService> logger,
        ILlmService llmService
    )
    {
        _db = db;
        _engine = engine;
        _env = env;
        _llmService = llmService;
        _snapshotService = snapshotService;
        _logger = logger;
    }

    // ── Calcul ────────────────────────────────────────────────────────────────

    public async Task<ServiceResult<PayrollResultReadDto>> CalculateAsync(
        PayrollSimulateRequestDto dto,
        int userId,
        CancellationToken ct = default
    )
    {
        var eligibility = await CheckPayrollEligibilityAsync(dto.EmployeeId, dto.PayMonth, dto.PayYear, ct);
        if (!eligibility.IsEligible)
            return ServiceResult<PayrollResultReadDto>.Fail(eligibility.Reason);

        // Verification Snapshotting
        var isApproved = await _db.PayrollResults.AnyAsync(
            pr =>
                pr.DeletedAt == null
                && pr.EmployeeId == dto.EmployeeId
                && pr.Month == dto.PayMonth
                && pr.Year == dto.PayYear
                && pr.PayHalf == dto.PayHalf
                && pr.Status == PayrollResultStatus.Approved,
            ct
        );

        if (isApproved)
            return ServiceResult<PayrollResultReadDto>.Fail(
                "La paie de ce mois est verrouillée (Approuvée) et ne peut plus être recalculée."
            );

        var data = await HydrateAsync(dto.EmployeeId, dto.PayMonth, dto.PayYear, dto.PayHalf, ct);
        if (data == null)
            return ServiceResult<PayrollResultReadDto>.Fail("Employé introuvable ou données manquantes.");

        var result = _engine.CalculatePayroll(data);
        if (!result.Success)
            return ServiceResult<PayrollResultReadDto>.Fail(result.ErrorMessage ?? "Erreur de calcul.");
        var entity = await PersistResultAsync(
            dto.EmployeeId,
            dto.PayMonth,
            dto.PayYear,
            dto.PayHalf,
            userId,
            result,
            false,
            ct
        );
        return ServiceResult<PayrollResultReadDto>.Ok(MapToRead(entity, result));
    }

    public async Task<ServiceResult<PayrollResultReadDto>> SimulateAsync(
        PayrollSimulateRequestDto dto,
        CancellationToken ct = default
    )
    {
        var eligibility = await CheckPayrollEligibilityAsync(dto.EmployeeId, dto.PayMonth, dto.PayYear, ct);
        if (!eligibility.IsEligible)
            return ServiceResult<PayrollResultReadDto>.Fail(eligibility.Reason);

        var data = await HydrateAsync(dto.EmployeeId, dto.PayMonth, dto.PayYear, dto.PayHalf, ct);
        if (data == null)
            return ServiceResult<PayrollResultReadDto>.Fail("Employé introuvable ou données manquantes.");

        var result = _engine.CalculatePayroll(data);
        if (!result.Success)
            return ServiceResult<PayrollResultReadDto>.Fail(result.ErrorMessage ?? "Erreur de calcul.");

        // Simulation : on crée un DTO sans persister
        var fakeEntity = new PayrollResult
        {
            EmployeeId = dto.EmployeeId,
            CompanyId = 0,
            Month = dto.PayMonth,
            Year = dto.PayYear,
            PayHalf = dto.PayHalf,
        };
        ApplyResultToEntity(result, fakeEntity);
        return ServiceResult<PayrollResultReadDto>.Ok(MapToRead(fakeEntity, result));
    }

    public async Task<ServiceResult<IEnumerable<PayrollResultReadDto>>> BatchCalculateAsync(
        PayrollBatchRequestDto dto,
        int userId,
        CancellationToken ct = default
    )
    {
        var employees = await _db.Employees.Where(e => e.CompanyId == dto.CompanyId).Select(e => e.Id).ToListAsync(ct);

        var results = new List<PayrollResultReadDto>();
        foreach (var empId in employees)
        {
            var simDto = new PayrollSimulateRequestDto
            {
                EmployeeId = empId,
                PayMonth = dto.PayMonth,
                PayYear = dto.PayYear,
                PayHalf = dto.PayHalf,
            };
            var res = await CalculateAsync(simDto, userId, ct);
            if (res.Success)
                results.Add(res.Data!);
        }
        return ServiceResult<IEnumerable<PayrollResultReadDto>>.Ok(results);
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<ServiceResult<PayrollBulletinResultsResponseDto>> GetResultsAsync(
        int? companyId,
        int month,
        int year,
        int? payHalf,
        string? statusFilter,
        CancellationToken ct = default
    )
    {
        var q = _db
            .PayrollResults.AsNoTracking()
            .Where(pr => pr.DeletedAt == null && pr.Month == month && pr.Year == year && pr.PayHalf == payHalf)
            .Include(pr => pr.Employee)
            .Include(pr => pr.Company)
            .AsQueryable();

        if (companyId is > 0)
            q = q.Where(pr => pr.CompanyId == companyId.Value);

        if (TryMapBulletinStatusFilter(statusFilter, out var st))
            q = q.Where(pr => pr.Status == st);

        var list = await q.OrderBy(pr => pr.Employee!.LastName)
            .ThenBy(pr => pr.Employee!.FirstName)
            .Select(pr => new PayrollBulletinResultItemDto
            {
                Id = pr.Id,
                EmployeeId = pr.EmployeeId,
                EmployeeName = pr.Employee!.FirstName + " " + pr.Employee.LastName,
                CompanyId = pr.CompanyId,
                CompanyName = pr.Company!.CompanyName,
                Month = pr.Month,
                Year = pr.Year,
                PayHalf = pr.PayHalf,
                Status = pr.Status,
                ErrorMessage = pr.ErrorMessage,
                SalaireBase = pr.SalaireBase,
                TotalBrut = pr.TotalBrut,
                TotalCotisationsSalariales = pr.TotalCotisationsSalariales,
                TotalCotisationsPatronales = pr.TotalCotisationsPatronales,
                ImpotRevenu = pr.ImpotRevenu,
                TotalNet = pr.TotalNet ?? pr.NetAPayer,
                TotalNet2 = pr.TotalNet2,
                ProcessedAt = pr.ProcessedAt,
                ClaudeModel = pr.ClaudeModel,
                TokensUsed = pr.TokensUsed,
            })
            .ToListAsync(ct);

        return ServiceResult<PayrollBulletinResultsResponseDto>.Ok(
            new PayrollBulletinResultsResponseDto
            {
                Count = list.Count,
                Month = month,
                Year = year,
                Results = list,
            }
        );
    }

    public async Task<ServiceResult<object>> GetStatsAsync(
        int companyId,
        int year,
        int month,
        CancellationToken ct = default
    )
    {
        var q = _db.PayrollResults.Where(pr =>
            pr.CompanyId == companyId && pr.Year == year && pr.Month == month && pr.DeletedAt == null
        );
        var count = await q.CountAsync(ct);
        var totalNet = await q.SumAsync(pr => pr.TotalNet ?? pr.NetAPayer ?? 0, ct);
        return ServiceResult<object>.Ok(new { count, totalNet });
    }

    public async Task<ServiceResult<PayrollResultReadDto>> GetResultByIdAsync(int id, CancellationToken ct = default)
    {
        var pr = await _db
            .PayrollResults.Include(pr => pr.Employee)
            .Include(pr => pr.Primes)
            .Include(pr => pr.CalculationAuditSteps)
            .FirstOrDefaultAsync(pr => pr.Id == id && pr.DeletedAt == null, ct);
        return pr == null
            ? ServiceResult<PayrollResultReadDto>.Fail("Résultat introuvable.")
            : ServiceResult<PayrollResultReadDto>.Ok(MapToRead(pr, null));
    }

    public async Task<ServiceResult<PayrollBulletinDetailDto>> GetBulletinDetailAsync(
        int id,
        CancellationToken ct = default
    )
    {
        var result = await _db
            .PayrollResults.AsNoTracking()
            .Include(pr => pr.Employee)
            .Include(pr => pr.Company)
            .Include(pr => pr.Primes)
            .Include(pr => pr.CalculationAuditSteps)
            .FirstOrDefaultAsync(pr => pr.Id == id && pr.DeletedAt == null, ct);

        if (result == null)
            return ServiceResult<PayrollBulletinDetailDto>.Fail("Résultat de paie introuvable.");

        var startOfPeriod = new DateTime(result.Year, result.Month, 1);
        var endOfPeriod = startOfPeriod.AddMonths(1).AddDays(-1);

        // Période de paie : null/mensuel, 1 => 1-15, 2 => 16-31.
        if (result.PayHalf == 1)
            endOfPeriod = startOfPeriod.AddDays(14);
        else if (result.PayHalf == 2)
            startOfPeriod = startOfPeriod.AddDays(15);

        var startDate = DateOnly.FromDateTime(startOfPeriod);
        var endDate = DateOnly.FromDateTime(endOfPeriod);

        var absences = await _db
            .EmployeeAbsences.AsNoTracking()
            .Where(ea =>
                ea.EmployeeId == result.EmployeeId
                && ea.AbsenceDate >= startDate
                && ea.AbsenceDate <= endDate
                && ea.Status == AbsenceStatus.Approved
                && ea.DeletedAt == null
            )
            .OrderBy(ea => ea.AbsenceDate)
            .Select(ea => new PayrollBulletinAbsenceDto
            {
                Id = ea.Id,
                AbsenceDate = ea.AbsenceDate.ToString("yyyy-MM-dd"),
                AbsenceType = ea.AbsenceType,
                Reason = ea.Reason,
                DurationType = ea.DurationType.ToString(),
                Status = ea.Status.ToString(),
            })
            .ToListAsync(ct);

        var overtimes = await _db
            .EmployeeOvertimes.AsNoTracking()
            .Where(eo =>
                eo.EmployeeId == result.EmployeeId
                && eo.OvertimeDate >= startDate
                && eo.OvertimeDate <= endDate
                && eo.Status == OvertimeStatus.Approved
                && eo.DeletedAt == null
            )
            .OrderBy(eo => eo.OvertimeDate)
            .Select(eo => new PayrollBulletinOvertimeDto
            {
                Id = eo.Id,
                OvertimeDate = eo.OvertimeDate.ToString("yyyy-MM-dd"),
                DurationInHours = eo.DurationInHours,
                RateMultiplierApplied = eo.RateMultiplierApplied,
            })
            .ToListAsync(ct);

        var leaves = await _db
            .LeaveRequests.AsNoTracking()
            .Include(lr => lr.LeaveType)
            .Where(lr =>
                lr.EmployeeId == result.EmployeeId
                && lr.Status == LeaveRequestStatus.Approved
                && lr.DeletedAt == null
                && lr.StartDate <= endDate
                && lr.EndDate >= startDate
            )
            .OrderBy(lr => lr.StartDate)
            .Select(lr => new PayrollBulletinLeaveDto
            {
                Id = lr.Id,
                StartDate = lr.StartDate.ToString("yyyy-MM-dd"),
                EndDate = lr.EndDate.ToString("yyyy-MM-dd"),
                WorkingDaysDeducted = lr.WorkingDaysDeducted,
                LeaveTypeName = lr.LeaveType != null ? (lr.LeaveType.LeaveNameFr ?? lr.LeaveType.LeaveCode) : null,
            })
            .ToListAsync(ct);

        var primes = result
            .Primes.OrderBy(p => p.Ordre)
            .Select(p => new PayrollBulletinDetailPrimeDto
            {
                Label = p.Label,
                Montant = p.Montant,
                Ordre = p.Ordre,
                IsTaxable = p.IsTaxable,
            })
            .ToList();

        var auditSteps = result
            .CalculationAuditSteps?.OrderBy(s => s.StepOrder)
            .Select(s => new PayrollBulletinAuditStepDto
            {
                StepOrder = s.StepOrder,
                ModuleName = s.ModuleName,
                FormulaDescription = s.FormulaDescription,
                InputsJson = s.InputsJson,
                OutputsJson = s.OutputsJson,
            })
            .ToList();

        var dto = new PayrollBulletinDetailDto
        {
            Id = result.Id,
            EmployeeId = result.EmployeeId,
            EmployeeName = $"{result.Employee.FirstName} {result.Employee.LastName}",
            CompanyId = result.CompanyId,
            CompanyName = result.Company.CompanyName,
            Month = result.Month,
            Year = result.Year,
            PayHalf = result.PayHalf,
            Status = result.Status,
            ErrorMessage = result.ErrorMessage,
            SalaireBase = result.SalaireBase,
            HeuresSupp25 = result.HeuresSupp25,
            HeuresSupp50 = result.HeuresSupp50,
            HeuresSupp100 = result.HeuresSupp100,
            Conges = result.Conges,
            JoursFeries = result.JoursFeries,
            PrimeAnciennete = result.PrimeAnciennete,
            PrimeImposable1 = result.PrimeImposable1,
            PrimeImposable2 = result.PrimeImposable2,
            PrimeImposable3 = result.PrimeImposable3,
            TotalPrimesImposables = result.TotalPrimesImposables,
            TotalBrut = result.TotalBrut,
            FraisProfessionnels = result.FraisProfessionnels,
            IndemniteRepresentation = result.IndemniteRepresentation,
            PrimeTransport = result.PrimeTransport,
            PrimePanier = result.PrimePanier,
            IndemniteDeplacement = result.IndemniteDeplacement,
            IndemniteCaisse = result.IndemniteCaisse,
            PrimeSalissure = result.PrimeSalissure,
            GratificationsFamilial = result.GratificationsFamilial,
            PrimeVoyageMecque = result.PrimeVoyageMecque,
            IndemniteLicenciement = result.IndemniteLicenciement,
            IndemniteKilometrique = result.IndemniteKilometrique,
            PrimeTourne = result.PrimeTourne,
            PrimeOutillage = result.PrimeOutillage,
            AideMedicale = result.AideMedicale,
            AutresPrimesNonImposable = result.AutresPrimesNonImposable,
            TotalIndemnites = result.TotalIndemnites,
            TotalNiExcedentImposable = result.TotalNiExcedentImposable,
            CnssPartSalariale = result.CnssPartSalariale,
            CimrPartSalariale = result.CimrPartSalariale,
            AmoPartSalariale = result.AmoPartSalariale,
            MutuellePartSalariale = result.MutuellePartSalariale,
            TotalCotisationsSalariales = result.TotalCotisationsSalariales,
            CnssPartPatronale = result.CnssPartPatronale,
            CimrPartPatronale = result.CimrPartPatronale,
            AmoPartPatronale = result.AmoPartPatronale,
            MutuellePartPatronale = result.MutuellePartPatronale,
            TotalCotisationsPatronales = result.TotalCotisationsPatronales,
            ImpotRevenu = result.ImpotRevenu,
            Arrondi = result.Arrondi,
            AvanceSurSalaire = result.AvanceSurSalaire,
            InteretSurLogement = result.InteretSurLogement,
            BrutImposable = result.BrutImposable,
            NetImposable = result.NetImposable,
            TotalGains = result.TotalGains,
            TotalRetenues = result.TotalRetenues,
            NetAPayer = result.NetAPayer,
            TotalNet = result.TotalNet,
            TotalNet2 = result.TotalNet2,
            Primes = primes,
            CalculationAuditSteps = auditSteps,
            Absences = absences,
            Overtimes = overtimes,
            Leaves = leaves,
        };

        return ServiceResult<PayrollBulletinDetailDto>.Ok(dto);
    }

    private static bool TryMapBulletinStatusFilter(string? statusFilter, out PayrollResultStatus status)
    {
        status = default;
        if (string.IsNullOrWhiteSpace(statusFilter))
            return false;

        var raw = statusFilter.Trim().ToUpperInvariant();
        switch (raw)
        {
            case "SUCCESS":
            case "OK":
            case "COMPLETED":
                status = PayrollResultStatus.OK;
                return true;
            case "ERROR":
            case "ERREUR":
            case "FAILED":
            case "FAIL":
                status = PayrollResultStatus.Error;
                return true;
            case "PENDING":
            case "EN_ATTENTE":
            case "EN ATTENTE":
                status = PayrollResultStatus.Pending;
                return true;
            default:
                return false;
        }
    }

    public async Task<ServiceResult<PayrollResultReadDto>> RecalculateForEmployeeAsync(
        int employeeId,
        int month,
        int year,
        int? payHalf,
        int userId,
        CancellationToken ct = default
    )
    {
        // Verification Snapshotting
        var isApproved = await _db.PayrollResults.AnyAsync(
            pr =>
                pr.DeletedAt == null
                && pr.EmployeeId == employeeId
                && pr.Month == month
                && pr.Year == year
                && pr.PayHalf == payHalf
                && pr.Status == PayrollResultStatus.Approved,
            ct
        );

        if (isApproved)
            return ServiceResult<PayrollResultReadDto>.Fail(
                "La paie de ce mois est verrouillée (Approuvée) et ne peut plus être recalculée."
            );

        // Supprimer le résultat existant s'il existe
        var existing = await _db.PayrollResults.FirstOrDefaultAsync(
            pr => pr.EmployeeId == employeeId && pr.Month == month && pr.Year == year && pr.PayHalf == payHalf,
            ct
        );
        if (existing != null)
        {
            existing.DeletedAt = DateTimeOffset.UtcNow;
            existing.DeletedBy = userId;
        }

        var simDto = new PayrollSimulateRequestDto
        {
            EmployeeId = employeeId,
            PayMonth = month,
            PayYear = year,
            PayHalf = payHalf,
        };
        return await CalculateAsync(simDto, userId, ct);
    }

    public async Task<ServiceResult<List<PayrollCustomRuleDto>>> GetCustomRulesAsync(
        int companyId,
        CancellationToken ct = default
    )
    {
        var items = await _db
            .PayrollCustomRules.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.DeletedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PayrollCustomRuleDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                DslSnippet = x.DslSnippet,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync(ct);

        return ServiceResult<List<PayrollCustomRuleDto>>.Ok(items);
    }

    public async Task<ServiceResult<string>> PreviewCustomRuleAsync(
        CreatePayrollCustomRuleRequestDto dto,
        CancellationToken ct = default
    )
    {
        var title = dto.Title?.Trim();
        var description = dto.Description?.Trim();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
            return ServiceResult<string>.Fail("Le titre et la description sont requis.");

        var safeTitle = title.Replace("\r", " ").Replace("\n", " ");
        string generatedDsl;
        try
        {
            generatedDsl = await _llmService.GenerateDslFromNaturalLanguageAsync(safeTitle, description, ct);
        }
        catch (Exception ex)
        {
            return ServiceResult<string>.Fail(
                $"Le service IA est temporairement indisponible ({ex.Message}). Veuillez réessayer plus tard."
            );
        }

        if (string.IsNullOrWhiteSpace(generatedDsl))
            return ServiceResult<string>.Fail("L'IA n'a pas réussi à générer le code DSL.");

        return ServiceResult<string>.Ok(generatedDsl.Trim());
    }

    public async Task<ServiceResult<PayrollCustomRuleDto>> CreateCustomRuleAsync(
        int companyId,
        CreatePayrollCustomRuleRequestDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var title = dto.Title?.Trim();
        var description = dto.Description?.Trim();
        var dslSnippet = dto.DslSnippet?.Trim();

        if (
            string.IsNullOrWhiteSpace(title)
            || string.IsNullOrWhiteSpace(description)
            || string.IsNullOrWhiteSpace(dslSnippet)
        )
            return ServiceResult<PayrollCustomRuleDto>.Fail(
                "Le titre, la description et la règle générée sont requis."
            );

        var entity = new PayrollCustomRule
        {
            CompanyId = companyId,
            Title = title,
            Description = description,
            DslSnippet = dslSnippet,
            CreatedBy = createdBy,
            GeneratedFilePath = string.Empty,
        };
        _db.PayrollCustomRules.Add(entity);
        await _db.SaveChangesAsync(ct);

        var generatedPath = await GenerateRulesFileForCompanyAsync(companyId, ct);
        entity.GeneratedFilePath = generatedPath;
        entity.UpdatedBy = createdBy;
        await _db.SaveChangesAsync(ct);

        return ServiceResult<PayrollCustomRuleDto>.Ok(
            new PayrollCustomRuleDto
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                DslSnippet = entity.DslSnippet,
                CreatedAt = entity.CreatedAt,
            }
        );
    }

    public async Task<ServiceResult> DeleteCustomRuleAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var rule = await _db.PayrollCustomRules.FindAsync(new object[] { id }, ct);
        if (rule == null || rule.DeletedAt != null)
            return ServiceResult.Fail("Règle personnalisée introuvable.");

        rule.DeletedAt = DateTimeOffset.UtcNow;
        rule.DeletedBy = deletedBy;

        await _db.SaveChangesAsync(ct);

        await GenerateRulesFileForCompanyAsync(rule.CompanyId, ct);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateResultStatusAsync(
        int id,
        PayrollResultStatus status,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var pr = await _db.PayrollResults.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (pr == null)
            return ServiceResult.Fail("Résultat introuvable.");

        pr.Status = status;
        pr.UpdatedAt = DateTimeOffset.UtcNow;
        pr.UpdatedBy = updatedBy;

        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteResultAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var pr = await _db.PayrollResults.FindAsync(new object[] { id }, ct);
        if (pr == null)
            return ServiceResult.Fail("Résultat introuvable.");
        if (pr.Status == PayrollResultStatus.Approved)
            return ServiceResult.Fail("La paie est verrouillée, suppression interdite.");
        pr.DeletedAt = DateTimeOffset.UtcNow;
        pr.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ApprovePeriodAsync(
        int companyId,
        int month,
        int year,
        int? payHalf,
        int userId,
        CancellationToken ct = default
    )
    {
        if (companyId <= 0)
            return ServiceResult.Fail("companyId invalide.");
        if (month < 1 || month > 12)
            return ServiceResult.Fail("month invalide (1-12).");
        if (year < 2000 || year > 2100)
            return ServiceResult.Fail("year invalide.");

        var q = _db.PayrollResults.Where(pr =>
            pr.CompanyId == companyId && pr.Year == year && pr.Month == month && pr.DeletedAt == null
        );
        if (payHalf.HasValue)
            q = q.Where(pr => pr.PayHalf == payHalf.Value);

        var toApprove = await q.Where(pr => pr.Status == PayrollResultStatus.OK).ToListAsync(ct);
        if (toApprove == null || toApprove.Count == 0)
            return ServiceResult.Fail($"Aucun bulletin OK à approuver pour {month:D2}/{year}.");

        var now = DateTimeOffset.UtcNow;
        foreach (var pr in toApprove)
        {
            pr.Status = PayrollResultStatus.Approved;
            pr.UpdatedAt = now;
            pr.UpdatedBy = userId;
        }

        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    private async Task<(bool IsEligible, string? Reason)> CheckPayrollEligibilityAsync(
        int employeeId,
        int month,
        int year,
        CancellationToken ct = default
    )
    {
        if (month < 1 || month > 12)
            return (false, "Mois invalide.");
        if (year < 2000 || year > 2100)
            return (false, "Année invalide.");
        var emp = await _db.Employees.FindAsync(new object[] { employeeId }, ct);
        if (emp == null)
            return (false, "Employé introuvable.");
        return (true, null);
    }

    private async Task<string> GenerateRulesFileForCompanyAsync(int companyId, CancellationToken ct = default)
    {
        try
        {
            var snippets = await _db
                .PayrollCustomRules.AsNoTracking()
                .Where(r => r.CompanyId == companyId && r.DeletedAt == null)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => r.DslSnippet)
                .ToListAsync(ct);

            var content = string.Join("\n\n", snippets);
            var dir = Path.Combine(_env.ContentRootPath ?? ".", "payroll-rules");
            Directory.CreateDirectory(dir);
            var fileName = $"payroll-rules-{companyId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.dsl";
            var path = Path.Combine(dir, fileName);
            await File.WriteAllTextAsync(path, content, ct);
            return path;
        }
        catch
        {
            return string.Empty;
        }
    }

    // ── Hydratation ───────────────────────────────────────────────────────────

    /// <summary>
    /// Charge toutes les données nécessaires au moteur depuis la DB
    /// et construit le EmployeePayrollDto.
    /// </summary>
    private async Task<EmployeePayrollDto?> HydrateAsync(
        int employeeId,
        int month,
        int year,
        int? payHalf,
        CancellationToken ct
    )
    {
        var employee = await _db
            .Employees.Include(e => e.MaritalStatus)
            .Include(e => e.Children)
            .Include(e => e.Spouses)
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct);

        if (employee == null)
            return null;

        // Contrat actif à la date de paie
        var payDate = new DateTime(year, month, 1);
        var contract = await _db
            .EmployeeContracts.Include(c => c.ContractType)
                .ThenInclude(ct2 => ct2!.LegalContractType)
            .Include(c => c.ContractType)
                .ThenInclude(ct2 => ct2!.StateEmploymentProgram)
            .Include(c => c.JobPosition)
            .Where(c =>
                c.EmployeeId == employeeId && c.StartDate <= payDate && (c.EndDate == null || c.EndDate >= payDate)
            )
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(ct);

        if (contract == null)
            return null;

        // Salaire actif
        var salary = await _db
            .EmployeeSalaries.Include(s => s.Components)
            .Where(s =>
                s.EmployeeId == employeeId && s.EffectiveDate <= payDate && (s.EndDate == null || s.EndDate >= payDate)
            )
            .OrderByDescending(s => s.EffectiveDate)
            .FirstOrDefaultAsync(ct);

        var periodStart = DateOnly.FromDateTime(new DateTime(year, month, 1));
        var monthEnd = DateOnly.FromDateTime(new DateTime(year, month, DateTime.DaysInMonth(year, month)));

        var periodEnd = monthEnd;
        if (payHalf == 1)
            periodEnd = periodStart.AddDays(14); // 1-15
        else if (payHalf == 2)
            periodStart = periodStart.AddDays(15); // 16-31

        // Package salarial actif
        var assignment = await _db
            .SalaryPackageAssignments.Include(a => a.SalaryPackage)
                .ThenInclude(sp => sp!.Items)
            .Where(a =>
                a.EmployeeId == employeeId && a.EffectiveDate <= payDate && (a.EndDate == null || a.EndDate >= payDate)
            )
            .OrderByDescending(a => a.EffectiveDate)
            .FirstOrDefaultAsync(ct);

        // Absences du mois
        var startOfMonth = periodStart;
        var endOfMonth = periodEnd;

        var absences = await _db
            .EmployeeAbsences.Where(a =>
                a.EmployeeId == employeeId
                && a.AbsenceDate >= startOfMonth
                && a.AbsenceDate <= endOfMonth
                && a.Status == AbsenceStatus.Approved
            )
            .ToListAsync(ct);

        // Heures supplémentaires du mois
        var overtimes = await _db
            .EmployeeOvertimes.Where(o =>
                o.EmployeeId == employeeId
                && o.OvertimeDate >= startOfMonth
                && o.OvertimeDate <= endOfMonth
                && o.Status == OvertimeStatus.Approved
            )
            .ToListAsync(ct);

        // Congés approuvés chevauchant le mois
        var leaveStart = startOfMonth;
        var leaveEnd = endOfMonth;
        var leaves = await _db
            .LeaveRequests.Include(lr => lr.LeaveType)
            .Where(lr =>
                lr.EmployeeId == employeeId
                && lr.Status == LeaveRequestStatus.Approved
                && lr.StartDate <= leaveEnd
                && lr.EndDate >= leaveStart
            )
            .ToListAsync(ct);

        // Pointage (heures travaillées)
        var totalWorkedHours = await _db
            .EmployeeAttendances.Where(a =>
                a.EmployeeId == employeeId
                && a.WorkDate >= startOfMonth
                && a.WorkDate <= endOfMonth
                && a.Status == AttendanceStatus.Present
            )
            .SumAsync(a => (decimal?)a.WorkedHours ?? 0m, ct);

        var anciennete = (int)Math.Floor((payDate - contract.StartDate).TotalDays / 365.25);

        return new EmployeePayrollDto
        {
            FullName = $"{employee.FirstName} {employee.LastName}",
            CinNumber = employee.CinNumber,
            CnssNumber = employee.CnssNumber,
            CimrNumber = employee.CimrNumber,
            CimrEmployeeRate = employee.CimrEmployeeRate,
            CimrCompanyRate = employee.CimrCompanyRate,
            HasPrivateInsurance = employee.HasPrivateInsurance,
            PrivateInsuranceRate = employee.PrivateInsuranceRate,
            DisableAmo = employee.DisableAmo,
            MaritalStatus = employee.MaritalStatus?.Code,
            NumberOfChildren = employee.Children?.Count(c => c.IsDependent) ?? 0,
            HasSpouse = employee.Spouses?.Any(s => s.IsDependent) ?? false,

            ContractType = contract.ContractType?.ContractTypeName,
            LegalContractType = contract.ContractType?.LegalContractType?.Code,
            StateEmploymentProgram = contract.ContractType?.StateEmploymentProgram?.Code,
            JobPosition = contract.JobPosition?.Name,
            ContractStartDate = contract.StartDate,
            AncienneteYears = anciennete,

            BaseSalary = salary?.BaseSalary ?? 0m,
            BaseSalaryHourly = salary?.BaseSalaryHourly,
            SalaryComponents =
                salary
                    ?.Components?.Where(c =>
                        DateOnly.FromDateTime(c.EffectiveDate) <= periodEnd
                        && (c.EndDate == null || DateOnly.FromDateTime(c.EndDate.Value) >= periodStart)
                    )
                    .Select(c => new PayrollSalaryComponentDto
                    {
                        ComponentType = c.ComponentType,
                        Amount = c.Amount,
                        IsTaxable = c.IsTaxable,
                        IsSocial = c.IsSocial,
                        IsCIMR = c.IsCIMR,
                    })
                    .ToList()
                ?? new(),

            SalaryPackageName = assignment?.SalaryPackage?.Name,
            PackageItems =
                assignment
                    ?.SalaryPackage?.Items?.Select(i => new PayrollPackageItemDto
                    {
                        Label = i.Label,
                        DefaultValue = i.DefaultValue,
                        Type = i.Type,
                        IsTaxable = i.IsTaxable,
                        IsSocial = i.IsSocial,
                        IsCIMR = i.IsCIMR,
                        ExemptionLimit = i.ExemptionLimit,
                    })
                    .ToList()
                ?? new(),

            Absences = absences
                .Select(a => new PayrollAbsenceDto
                {
                    AbsenceType = a.AbsenceType,
                    AbsenceDate = a.AbsenceDate.ToDateTime(TimeOnly.MinValue),
                    DurationType = a.DurationType.ToString(),
                    Status = a.Status.ToString(),
                })
                .ToList(),

            Overtimes = overtimes
                .Select(o => new PayrollOvertimeDto
                {
                    OvertimeDate = o.OvertimeDate.ToDateTime(TimeOnly.MinValue),
                    DurationInHours = o.DurationInHours,
                    RateMultiplier = o.RateMultiplierApplied,
                })
                .ToList(),

            Leaves = leaves
                .Select(lr => new PayrollLeaveDto
                {
                    LeaveType = lr.LeaveType?.LeaveNameFr,
                    StartDate = lr.StartDate,
                    EndDate = lr.EndDate,
                    DaysCount = lr.WorkingDaysDeducted,
                })
                .ToList(),

            PayMonth = month,
            PayYear = year,
            PayHalf = payHalf,
            TotalWorkedHours = totalWorkedHours,
        };
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    private async Task<PayrollResult> PersistResultAsync(
        int employeeId,
        int month,
        int year,
        int? payHalf,
        int userId,
        PayrollCalculationResult calc,
        bool isBatch,
        CancellationToken ct
    )
    {
        var employee = await _db.Employees.FindAsync(new object[] { employeeId }, ct);

        // Règle métier: un seul résultat "actif" par employé/période.
        // Avant de persister, on soft-delete tout résultat existant (quel que soit le statut).
        var existingResults = await _db
            .PayrollResults.Where(pr =>
                pr.DeletedAt == null
                && pr.EmployeeId == employeeId
                && pr.Month == month
                && pr.Year == year
                && pr.PayHalf == payHalf
            )
            .ToListAsync(ct);

        if (existingResults.Count > 0)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var pr in existingResults)
            {
                pr.DeletedAt = now;
                pr.DeletedBy = userId;
            }
        }

        var entity = new PayrollResult
        {
            EmployeeId = employeeId,
            CompanyId = employee?.CompanyId ?? 0,
            Month = month,
            Year = year,
            PayHalf = payHalf,
            Status = PayrollResultStatus.OK,
            ProcessedAt = DateTime.UtcNow,
            CreatedBy = userId,
        };

        ApplyResultToEntity(calc, entity);
        _db.PayrollResults.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Primes détaillées (libellé + montant + ordre) pour affichage fidèle sur bulletin.
        if (calc.PrimesImposablesDetail.Count > 0)
        {
            var primes = calc.PrimesImposablesDetail
                .Where(p => p.Montant > 0m)
                .Select(p => new PayrollResultPrime
                {
                    PayrollResultId = entity.Id,
                    Label = p.Label,
                    Montant = p.Montant,
                    Ordre = p.Ordre,
                    IsTaxable = true,
                    CreatedBy = userId,
                });
            _db.PayrollResultPrimes.AddRange(primes);
        }

        // Audit steps
        if (calc.AuditSteps?.Any() == true)
        {
            var steps = calc.AuditSteps.Select(s => new PayrollCalculationAuditStep
            {
                PayrollResultId = entity.Id,
                StepOrder = s.StepOrder,
                ModuleName = s.ModuleName,
                FormulaDescription = s.FormulaDescription,
                InputsJson = s.InputsJson,
                OutputsJson = s.OutputsJson,
                CreatedBy = userId,
            });
            _db.PayrollCalculationAuditSteps.AddRange(steps);
        }

        await _db.SaveChangesAsync(ct);

        // ══════════════════════════════════════════════════════
        // NOUVEAU — Snapshot cumulé IR
        // ══════════════════════════════════════════════════════
        var snapResult = await _snapshotService.BuildAndSaveAsync(entity, ct);
        if (!snapResult.Success)
            // Non bloquant — on logue mais on ne fait pas échouer le calcul
            _logger.LogWarning(
                "Snapshot IR non sauvegardé pour emp {EmpId} {Month}/{Year} : {Error}",
                employeeId, month, year, snapResult.Error);
        // ══════════════════════════════════════════════════════

        return entity;
    }

    private static void ApplyResultToEntity(PayrollCalculationResult calc, PayrollResult entity)
    {
        entity.SalaireBase = calc.SalaireBase26j;
        entity.PrimeAnciennete = calc.PrimeAnciennete;
        entity.PrimeAnciennteRate = calc.TauxAnciennete;
        entity.HeuresSupp25 = calc.MontHsupp25;
        entity.HeuresSupp50 = calc.MontHsupp50;
        entity.HeuresSupp100 = calc.MontHsupp100;
        entity.Conges = calc.JoursConge;
        entity.JoursFeries = calc.JoursFeries;
        entity.PrimeImposable1 = calc.PrimeImposable1;
        entity.PrimeImposable2 = calc.PrimeImposable2;
        entity.PrimeImposable3 = calc.PrimeImposable3;
        entity.TotalPrimesImposables = calc.TotalPrimesImposables;
        entity.TotalBrut = calc.SalaireBrutImposable;
        entity.BrutImposable = calc.SalaireBrutImposable;
        entity.TotalNiExcedentImposable = calc.TotalNiExcedentImposable;

        entity.IndemniteRepresentation = calc.NiLineRepresentation;
        entity.PrimeTransport = calc.NiLineTransport;
        entity.PrimePanier = calc.NiLinePanier;
        entity.IndemniteCaisse = calc.NiLineCaisse;
        entity.PrimeSalissure = calc.NiLineSalissure;
        entity.GratificationsFamilial = calc.NiLineGratifSociale;
        entity.IndemniteKilometrique = calc.NiLineKilometrique;
        entity.PrimeTourne = calc.NiLineTournee;
        entity.PrimeOutillage = calc.NiLineOutillage;
        entity.AideMedicale = calc.NiLineAideMedicale;
        entity.AutresPrimesNonImposable = calc.NiLineAutres + calc.NiLineLait;
        entity.TotalIndemnites = calc.TotalNiExonere;

        entity.FraisProfessionnels = calc.MontantFp;
        entity.CnssPartSalariale = calc.CnssRgSalarial;
        entity.AmoPartSalariale = calc.CnssAmoSalarial;
        entity.CnssBase = calc.BaseCnssRg;
        entity.CimrPartSalariale = calc.CimrSalarial;
        entity.CimrBase = calc.BaseCimr;
        entity.AmoBase = calc.SalaireBrutImposable;
        entity.MutuelleBase = calc.SalaireBrutImposable;
        entity.CnssPartPatronale = calc.CnssRgPatronal;
        entity.AmoPartPatronale = calc.CnssAmoPatronal;
        entity.CimrPartPatronale = calc.CimrPatronal;
        entity.TotalCotisationsSalariales = calc.TotalCnssSalarial + calc.CimrSalarial + calc.MutuelleSalarialeAmount;
        entity.TotalCotisationsPatronales = calc.TotalCnssPatronal + calc.CimrPatronal + calc.MutuellePatronaleAmount;
        entity.MutuellePartSalariale = calc.MutuelleSalarialeAmount;
        entity.MutuellePartPatronale = calc.MutuellePatronaleAmount;
        entity.ImpotRevenu = calc.IrFinal;
        entity.IrTaux = calc.TauxIr;
        entity.NetImposable = calc.RevenuNetImposable;
        entity.AvanceSurSalaire = calc.AvanceSalaire;
        entity.InteretSurLogement = calc.InteretPretLogement;
        entity.TotalGains = calc.SalaireBrutImposable + calc.TotalNiExonere;
        entity.TotalRetenues = calc.TotalRetenuesSalariales;
        entity.NetAPayer = calc.SalaireNet;
        entity.TotalNet = calc.SalaireNet;
        entity.TotalNet2 = calc.SalaireNet;
        entity.ErrorMessage = calc.ErrorMessage;
        entity.Status = calc.Success ? PayrollResultStatus.OK : PayrollResultStatus.Error;
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static PayrollResultReadDto MapToRead(PayrollResult pr, PayrollCalculationResult? calc) =>
        new()
        {
            Id = pr.Id,
            EmployeeId = pr.EmployeeId,
            EmployeeFullName = pr.Employee != null ? $"{pr.Employee.FirstName} {pr.Employee.LastName}" : string.Empty,
            CompanyId = pr.CompanyId,
            Month = pr.Month,
            Year = pr.Year,
            PayHalf = pr.PayHalf,
            Status = pr.Status,
            ErrorMessage = pr.ErrorMessage,
            SalaireBase = pr.SalaireBase,
            PrimeAnciennete = pr.PrimeAnciennete,
            PrimeAnciennteRate = pr.PrimeAnciennteRate,
            HeuresSupp25 = pr.HeuresSupp25,
            HeuresSupp50 = pr.HeuresSupp50,
            HeuresSupp100 = pr.HeuresSupp100,
            TotalPrimesImposables = pr.TotalPrimesImposables,
            BrutImposable = pr.BrutImposable,
            FraisProfessionnels = pr.FraisProfessionnels,
            BaseCnss = pr.CnssBase,
            CnssRgSalarial = pr.CnssPartSalariale,
            CnssAmoSalarial = pr.AmoPartSalariale,
            CimrSalarial = pr.CimrPartSalariale,
            MutuelleSalariale = pr.MutuellePartSalariale,
            IrTaux = pr.IrTaux,
            IR = pr.ImpotRevenu,
            RevenuNetImposable = pr.NetImposable,
            SalaireNet = pr.NetAPayer,
            CnssRgPatronal = pr.CnssPartPatronale,
            AmoPatronal = pr.AmoPartPatronale,
            CimrPatronal = pr.CimrPartPatronale,
            TotalChargesPatronales = pr.TotalCotisationsPatronales,
            Primes =
                pr.Primes?.Select(p => new PayrollResultPrimeDto
                    {
                        Label = p.Label,
                        Montant = p.Montant,
                        IsTaxable = p.IsTaxable,
                    })
                    .ToList()
                ?? new(),
            AuditSteps = pr
                .CalculationAuditSteps?.OrderBy(s => s.StepOrder)
                .Select(s => new PayrollAuditStepDto
                {
                    StepOrder = s.StepOrder,
                    ModuleName = s.ModuleName,
                    FormulaDescription = s.FormulaDescription,
                })
                .ToList(),
        };
}
