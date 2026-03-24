using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Common.LeaveStatus;
using payzen_backend.Models.Common.OvertimeEnums;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Leave;
using payzen_backend.Models.Payroll;
using payzen_backend.Services.Payroll;

namespace payzen_backend.Controllers.Payroll
{
    [Route("api/payroll")]
    [ApiController]
    [Authorize]
    public class PayrollController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PaieService _paieService;
        private readonly ILogger<PayrollController> _logger;

        public PayrollController(
            AppDbContext db,
            PaieService paieService,
            ILogger<PayrollController> logger)
        {
            _db = db;
            _paieService = paieService;
            _logger = logger;
        }

        /// <summary>
        /// Lance le calcul de paie pour tous les employés d'une période donnée
        /// </summary>
        /// <param name="companyId">ID de l'entreprise</param>
        /// <param name="month">Mois (1-12)</param>
        /// <param name="year">Année (ex: 2025)</param>
        /// <param name="useNativeEngine">Si true, utilise le moteur natif C# au lieu du LLM (par défaut: true)</param>
        [HttpPost("calculate")]
        public async Task<ActionResult> CalculatePayroll(
            [FromQuery] int companyId,
            [FromQuery] int month, 
            [FromQuery] int year,
            [FromQuery] bool useNativeEngine = true,
            [FromQuery] int? half = null)
        {
            try
            {
                if (companyId <= 0)
                    return BadRequest(new { error = "L'ID de l'entreprise est requis." });

                if (month < 1 || month > 12)
                    return BadRequest(new { error = "Le mois doit être entre 1 et 12." });

                if (year < 2020 || year > 2100)
                    return BadRequest(new { error = "Année invalide." });

                var engineType = useNativeEngine ? "Moteur natif C#" : "LLM (Gemini)";
                _logger.LogInformation("Début du calcul de paie ({Engine}) pour l'entreprise {CompanyId}, {Month}/{Year}", 
                    engineType, companyId, month, year);

                // Lancer le traitement (async)
                await _paieService.TraiterTousLesSalariesAsync(companyId, month, year, useNativeEngine, half);

                _logger.LogInformation("Calcul de paie terminé pour l'entreprise {CompanyId}, {Month}/{Year}", 
                    companyId, month, year);

                return Ok(new
                {
                    message = $"Calcul de paie terminé pour l'entreprise {companyId}, {month}/{year}.",
                    engine = engineType,
                    companyId,
                    month,
                    year
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul de paie pour l'entreprise {CompanyId}, {Month}/{Year}", 
                    companyId, month, year);
                return StatusCode(500, new { error = "Erreur lors du calcul de paie.", details = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les résultats de paie pour une période donnée
        /// </summary>
        /// <param name="month">Mois (1-12)</param>
        /// <param name="year">Année</param>
        /// <param name="companyId">Filtrer par entreprise (optionnel)</param>
        /// <param name="status">Filtrer par statut (optionnel)</param>
        [HttpGet("results")]
        public async Task<ActionResult> GetPayrollResults(
            [FromQuery] int month,
            [FromQuery] int year,
            [FromQuery] int? companyId = null,
            [FromQuery] PayrollResultStatus? status = null)
        {
            try
            {
                var query = _db.PayrollResults
                    .Include(pr => pr.Employee)
                    .Include(pr => pr.Company)
                    .Where(pr => pr.Month == month && pr.Year == year);

                if (companyId.HasValue)
                    query = query.Where(pr => pr.CompanyId == companyId.Value);

                if (status.HasValue)
                    query = query.Where(pr => pr.Status == status.Value);

                var results = await query
                    .OrderBy(pr => pr.Employee.LastName)
                    .ThenBy(pr => pr.Employee.FirstName)
                    .Select(pr => new
                    {
                        pr.Id,
                        pr.EmployeeId,
                        EmployeeName = $"{pr.Employee.FirstName} {pr.Employee.LastName}",
                        pr.CompanyId,
                        pr.Company.CompanyName,
                        pr.Month,
                        pr.Year,
                        pr.Status,
                        pr.ErrorMessage,
                        pr.SalaireBase,
                        pr.TotalBrut,
                        pr.TotalCotisationsSalariales,
                        pr.TotalCotisationsPatronales,
                        pr.ImpotRevenu,
                        pr.TotalNet,
                        pr.TotalNet2,
                        pr.ProcessedAt,
                        pr.ClaudeModel,
                        pr.TokensUsed
                    })
                    .ToListAsync();

                return Ok(new
                {
                    count = results.Count,
                    month,
                    year,
                    results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des résultats de paie");
                return StatusCode(500, new { error = "Erreur lors de la récupération des résultats." });
            }
        }

        /// <summary>
        /// Récupère le détail complet d'une fiche de paie (données salariales uniquement, sans métadonnées).
        /// </summary>
        /// <param name="id">ID du résultat de paie</param>
        [HttpGet("results/{id}")]
        public async Task<ActionResult> GetPayrollDetail(int id)
        {
            try
            {
                var result = await _db.PayrollResults
                    .Include(pr => pr.Employee)
                    .Include(pr => pr.Company)
                    .Include(pr => pr.Primes)
                    .Include(pr => pr.CalculationAuditSteps)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(pr => pr.Id == id);

                if (result == null)
                    return NotFound(new { error = "Résultat de paie introuvable." });

                var primes = result.Primes
                    .OrderBy(p => p.Ordre)
                    .Select(p => new { p.Label, p.Montant, p.Ordre, p.IsTaxable })
                    .ToList();

                var auditSteps = result.CalculationAuditSteps?
                    .OrderBy(s => s.StepOrder)
                    .Select(s => new
                    {
                        s.StepOrder,
                        s.ModuleName,
                        s.FormulaDescription,
                        s.InputsJson,
                        s.OutputsJson
                    }).ToList();

                var startOfMonth = new DateTime(result.Year, result.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                var startDate = DateOnly.FromDateTime(startOfMonth);
                var endDate = DateOnly.FromDateTime(endOfMonth);

                // Absences ayant un impact sur la paie : uniquement les absences approuvées
                var absences = await _db.EmployeeAbsences
                    .AsNoTracking()
                    .Where(ea => ea.EmployeeId == result.EmployeeId
                        && ea.AbsenceDate >= startDate
                        && ea.AbsenceDate <= endDate
                        && ea.Status == AbsenceStatus.Approved
                        && ea.DeletedAt == null)
                    .OrderBy(ea => ea.AbsenceDate)
                    .Select(ea => new
                    {
                        ea.Id,
                        ea.AbsenceDate,
                        ea.AbsenceType,
                        ea.Reason,
                        DurationType = ea.DurationType.ToString(),
                        Status = ea.Status.ToString()
                    })
                    .ToListAsync();

                var overtimes = await _db.EmployeeOvertimes
                    .AsNoTracking()
                    .Where(eo => eo.EmployeeId == result.EmployeeId
                        && eo.OvertimeDate >= startDate
                        && eo.OvertimeDate <= endDate
                        && eo.Status == OvertimeStatus.Approved
                        && eo.DeletedAt == null)
                    .OrderBy(eo => eo.OvertimeDate)
                    .Select(eo => new
                    {
                        eo.Id,
                        eo.OvertimeDate,
                        eo.DurationInHours,
                        eo.RateMultiplierApplied
                    })
                    .ToListAsync();

                var leaves = await _db.LeaveRequests
                    .Include(lr => lr.LeaveType)
                    .AsNoTracking()
                    .Where(lr => lr.EmployeeId == result.EmployeeId
                        && lr.Status == LeaveRequestStatus.Approved
                        && lr.DeletedAt == null
                        && lr.StartDate <= endDate
                        && lr.EndDate >= startDate)
                    .OrderBy(lr => lr.StartDate)
                    .Select(lr => new
                    {
                        lr.Id,
                        lr.StartDate,
                        lr.EndDate,
                        lr.WorkingDaysDeducted,
                        LeaveTypeName = lr.LeaveType != null ? (lr.LeaveType.LeaveNameFr ?? lr.LeaveType.LeaveCode) : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    result.Id,
                    result.EmployeeId,
                    EmployeeName = $"{result.Employee.FirstName} {result.Employee.LastName}",
                    result.CompanyId,
                    CompanyName = result.Company.CompanyName,
                    result.Month,
                    result.Year,
                    result.Status,
                    result.ErrorMessage,
                    // Salaire de base et heures
                    result.SalaireBase,
                    result.HeuresSupp25,
                    result.HeuresSupp50,
                    result.HeuresSupp100,
                    result.Conges,
                    result.JoursFeries,
                    result.PrimeAnciennete,
                    // Primes imposables
                    result.PrimeImposable1,
                    result.PrimeImposable2,
                    result.PrimeImposable3,
                    result.TotalPrimesImposables,
                    result.TotalBrut,
                    // Frais et indemnités
                    result.FraisProfessionnels,
                    result.IndemniteRepresentation,
                    result.PrimeTransport,
                    result.PrimePanier,
                    result.IndemniteDeplacement,
                    result.IndemniteCaisse,
                    result.PrimeSalissure,
                    result.GratificationsFamilial,
                    result.PrimeVoyageMecque,
                    result.IndemniteLicenciement,
                    result.IndemniteKilometrique,
                    result.PrimeTourne,
                    result.PrimeOutillage,
                    result.AideMedicale,
                    result.AutresPrimesNonImposable,
                    result.TotalIndemnites,
                    result.TotalNiExcedentImposable,
                    // Cotisations salariales
                    result.CnssPartSalariale,
                    result.CimrPartSalariale,
                    result.AmoPartSalariale,
                    result.MutuellePartSalariale,
                    result.TotalCotisationsSalariales,
                    // Cotisations patronales
                    result.CnssPartPatronale,
                    result.CimrPartPatronale,
                    result.AmoPartPatronale,
                    result.MutuellePartPatronale,
                    result.TotalCotisationsPatronales,
                    result.ImpotRevenu,
                    result.Arrondi,
                    result.AvanceSurSalaire,
                    result.InteretSurLogement,
                    result.BrutImposable,
                    result.NetImposable,
                    result.TotalGains,
                    result.TotalRetenues,
                    result.NetAPayer,
                    result.TotalNet,
                    result.TotalNet2,
                    Primes = primes,
                    CalculationAuditSteps = auditSteps,
                    Absences = absences,
                    Overtimes = overtimes,
                    Leaves = leaves
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du détail de paie {Id}", id);
                return StatusCode(500, new { error = "Erreur lors de la récupération du détail." });
            }
        }

        /// <summary>
        /// Récupère les statistiques de paie pour une période
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult> GetPayrollStats(
            [FromQuery] int month,
            [FromQuery] int year,
            [FromQuery] int? companyId = null)
        {
            try
            {
                var query = _db.PayrollResults
                    .Where(pr => pr.Month == month && pr.Year == year);

                if (companyId.HasValue)
                    query = query.Where(pr => pr.CompanyId == companyId.Value);

                var stats = await query
                    .GroupBy(pr => pr.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        TotalBrut = g.Sum(pr => pr.TotalBrut ?? 0),
                        TotalNet = g.Sum(pr => pr.TotalNet ?? 0)
                    })
                    .ToListAsync();

                var total = await query.CountAsync();
                var totalMontantBrut = await query.SumAsync(pr => pr.TotalBrut ?? 0);
                var totalMontantNet = await query.SumAsync(pr => pr.TotalNet ?? 0);

                return Ok(new
                {
                    month,
                    year,
                    companyId,
                    Total = total,
                    TotalMontantBrut = totalMontantBrut,
                    TotalMontantNet = totalMontantNet,
                    ParStatut = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques");
                return StatusCode(500, new { error = "Erreur lors de la récupération des statistiques." });
            }
        }

        /// <summary>
        /// Supprime un résultat de paie (soft delete)
        /// </summary>
        [HttpDelete("results/{id}")]
        public async Task<ActionResult> DeletePayrollResult(int id)
        {
            try
            {
                var result = await _db.PayrollResults.FindAsync(id);
                if (result == null)
                    return NotFound(new { error = "Résultat de paie introuvable." });

                result.DeletedAt = DateTimeOffset.UtcNow;
                result.DeletedBy = 0; // TODO: récupérer l'utilisateur connecté

                await _db.SaveChangesAsync();

                return Ok(new { message = "Résultat de paie supprimé avec succès." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du résultat {Id}", id);
                return StatusCode(500, new { error = "Erreur lors de la suppression." });
            }
        }

        /// <summary>
        /// Recalcule la paie pour un employé spécifique
        /// </summary>
        [HttpPost("recalculate/{employeeId}")]
        public async Task<ActionResult> RecalculateForEmployee(
            int employeeId,
            [FromQuery] int month,
            [FromQuery] int year,
            [FromQuery] bool useNativeEngine = true,
            [FromQuery] int? half = null)
        {
            try
            {
                var employee = await _db.Employees.FindAsync(employeeId);
                if (employee == null)
                    return NotFound(new { error = "Employé introuvable." });

                // Supprimer le résultat existant s'il existe (soft delete)
                var existing = await _db.PayrollResults
                    .FirstOrDefaultAsync(pr => pr.EmployeeId == employeeId 
                        && pr.Month == month 
                        && pr.Year == year);

                if (existing != null)
                {
                    existing.DeletedAt = DateTimeOffset.UtcNow;
                    existing.DeletedBy = 0;
                    await _db.SaveChangesAsync();
                }

                // Relancer le calcul pour cet employé (moteur natif ou LLM selon useNativeEngine)
                var result = await _paieService.TraiterUnSeulEmployeAsync(employeeId, month, year, useNativeEngine, half);

                return Ok(new
                {
                    message = "Recalcul terminé avec succès.",
                    employeeId,
                    month,
                    year,
                    status = result.Status.ToString(),
                    errorMessage = result.ErrorMessage,
                    resultId = result.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du recalcul pour l'employé {EmployeeId}", employeeId);
                return StatusCode(500, new { error = "Erreur lors du recalcul.", details = ex.Message });
            }
        }
    }
}
