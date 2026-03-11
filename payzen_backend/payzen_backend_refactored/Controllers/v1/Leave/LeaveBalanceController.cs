using payzen_backend.Models.Leave;
using payzen_backend.Models.LeaveBalance.Dtos;
using payzen_backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using payzen_backend.Data;
using payzen_backend.Services;
using payzen_backend.Services.Leave;

namespace payzen_backend.Controllers.v1.Leave
{
    [Route("api/v{version:apiVersion}/leave-balances")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class LeaveBalanceController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly LeaveEventLogService _leaveEventLogService;
        private readonly LeaveBalanceService _leaveBalanceService;

        public LeaveBalanceController(AppDbContext db, LeaveEventLogService leaveEventLogService, LeaveBalanceService leaveBalanceService)
        {
            _db = db;
            _leaveEventLogService = leaveEventLogService;
            _leaveBalanceService = leaveBalanceService;
        }

        /// <param name="asOf">Date de référence pour IsCarryoverExpired. Si null, utilise la date du jour.</param>
        private static LeaveBalanceReadDto MapToReadDto(LeaveBalance b, DateOnly? asOf = null)
        {
            var refDate = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var isExpired = b.CarryoverExpiresOn.HasValue && b.CarryoverExpiresOn.Value < refDate;

            return new LeaveBalanceReadDto
            {
                Id = b.Id,
                EmployeeId = b.EmployeeId,
                CompanyId = b.CompanyId,
                LeaveTypeId = b.LeaveTypeId,
                Year = b.Year,
                Month = b.Month,
                BalanceExpiresOn = b.GetBalanceExpiresOn(),
                OpeningDays = b.OpeningDays,
                AccruedDays = b.AccruedDays,
                UsedDays = b.UsedDays,
                CarryInDays = b.CarryInDays,
                CarryOutDays = b.CarryOutDays,
                ClosingDays = b.ClosingDays,
                CarryoverExpiresOn = b.CarryoverExpiresOn,
                IsCarryoverExpired = isExpired,
                LastRecalculatedAt = b.LastRecalculatedAt,
                CreatedAt = b.CreatedAt,
                ModifiedAt = b.ModifiedAt
            };
        }

        /// <summary>
        /// Récupère tous les soldes de congés avec filtres optionnels
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeaveBalanceReadDto>>> GetAll(
            [FromQuery] int? companyId = null,
            [FromQuery] int? employeeId = null,
            [FromQuery] int? year = null,
            [FromQuery] int? month = null)
        {
            var query = _db.LeaveBalances
                .AsNoTracking()
                .Where(b => b.DeletedAt == null);

            if (companyId.HasValue)
                query = query.Where(b => b.CompanyId == companyId.Value);
            if (employeeId.HasValue)
                query = query.Where(b => b.EmployeeId == employeeId.Value);
            if (year.HasValue)
                query = query.Where(b => b.Year == year.Value);
            if (month.HasValue)
                query = query.Where(b => b.Month == month.Value);

            var balances = await query
                .OrderBy(b => b.Year)
                .ThenBy(b => b.Month)
                .ThenBy(b => b.EmployeeId)
                .ThenBy(b => b.LeaveTypeId)
                .ToListAsync();

            var dtos = balances.Select(b => MapToReadDto(b)).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Récupère un solde par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveBalanceReadDto>> GetById(int id)
        {
            var balance = await _db.LeaveBalances
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);

            if (balance == null)
                return NotFound(new { Message = "Solde de congés non trouvé" });

            return Ok(MapToReadDto(balance));
        }

        /// <summary>
        /// Récupère les soldes d'un employé pour une année (optionnellement un mois).
        /// </summary>
        [HttpGet("employee/{employeeId}/year/{year}")]
        public async Task<ActionResult<IEnumerable<LeaveBalanceReadDto>>> GetByEmployeeAndYear(int employeeId, int year, [FromQuery] int? month = null)
        {
            var query = _db.LeaveBalances
                .AsNoTracking()
                .Where(b => b.EmployeeId == employeeId && b.Year == year && b.DeletedAt == null);
            if (month.HasValue)
                query = query.Where(b => b.Month == month.Value);

            var balances = await query.OrderBy(b => b.Month).ThenBy(b => b.LeaveTypeId).ToListAsync();
            return Ok(balances.Select(b => MapToReadDto(b)));
        }

        /// <summary>
        /// Récupère le solde d'un employé pour un mois donné (Year, Month).
        /// </summary>
        [HttpGet("employee/{employeeId}/year/{year}/month/{month}")]
        public async Task<ActionResult<IEnumerable<LeaveBalanceReadDto>>> GetByEmployeeYearMonth(int employeeId, int year, int month)
        {
            if (month < 1 || month > 12)
                return BadRequest(new { Message = "Le mois doit être entre 1 et 12" });

            var balances = await _db.LeaveBalances
                .AsNoTracking()
                .Where(b => b.EmployeeId == employeeId && b.Year == year && b.Month == month && b.DeletedAt == null)
                .OrderBy(b => b.LeaveTypeId)
                .ToListAsync();

            return Ok(balances.Select(b => MapToReadDto(b)));
        }

        /// <summary>
        /// Récupère un résumé des soldes par employé : total disponible (non expiré) par type de congé + détail par mois.
        /// Les soldes expirant plus de 2 ans après la fin du mois sont exclus du total disponible.
        /// </summary>
        [HttpGet("summary/{employeeId}")]
        public async Task<ActionResult<object>> GetSummary(int employeeId, [FromQuery] int? companyId = null)
        {
            var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
            var query = _db.LeaveBalances
                .AsNoTracking()
                .Where(b => b.EmployeeId == employeeId && b.DeletedAt == null);
            if (companyId.HasValue)
                query = query.Where(b => b.CompanyId == companyId.Value);

            var balances = await query.Include(b => b.LeaveType).ToListAsync();

            // Total disponible = uniquement les soldes non expirés (CarryoverExpiresOn >= asOf)
            var byLeaveType = balances
                .Where(b => b.CarryoverExpiresOn != null && b.CarryoverExpiresOn.Value >= asOf)
                .GroupBy(b => new { b.LeaveTypeId, b.LeaveType })
                .Select(g => new
                {
                    LeaveTypeId = g.Key.LeaveTypeId,
                    LeaveTypeName = g.Key.LeaveType?.LeaveNameFr,
                    LeaveTypeCode = g.Key.LeaveType?.LeaveCode,
                    TotalAvailable = g.Sum(x => x.ClosingDays),
                    ByMonth = g.Select(x => new { x.Year, x.Month, x.ClosingDays, BalanceExpiresOn = x.GetBalanceExpiresOn() }).OrderBy(x => x.Year).ThenBy(x => x.Month).ToList()
                })
                .ToList();

            return Ok(new
            {
                EmployeeId = employeeId,
                AsOf = asOf,
                TotalAvailableByLeaveType = byLeaveType,
                AllBalances = balances.OrderBy(b => b.Year).ThenBy(b => b.Month).Select(b => MapToReadDto(b, asOf)).ToList()
            });
        }

        /// <summary>
        /// Crée un nouveau solde de congés
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LeaveBalanceReadDto>> Create([FromBody] LeaveBalanceCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Vérifier que l'employé existe
            var employeeExists = await _db.Employees
                .AnyAsync(e => e.Id == dto.EmployeeId && e.DeletedAt == null);

            if (!employeeExists)
            {
                return NotFound(new { Message = "Employé non trouvé" });
            }

            // Vérifier que le LeaveType existe
            var leaveTypeExists = await _db.LeaveTypes
                .AnyAsync(lt => lt.Id == dto.LeaveTypeId && lt.DeletedAt == null);

            if (!leaveTypeExists)
            {
                return NotFound(new { Message = "Type de congé non trouvé" });
            }

            // Validation métier: un seul solde par (EmployeeId, LeaveTypeId, Year, Month)
            var balanceExists = await _db.LeaveBalances
                .AnyAsync(b => b.EmployeeId == dto.EmployeeId &&
                              b.LeaveTypeId == dto.LeaveTypeId &&
                              b.Year == dto.Year &&
                              b.Month == dto.Month &&
                              b.DeletedAt == null);

            if (balanceExists)
            {
                return Conflict(new { Message = "Un solde existe déjà pour cet employé, ce type de congé et ce mois" });
            }

            var userId = User.GetUserId();

            var closingDays = dto.OpeningDays + dto.AccruedDays + dto.CarryInDays - dto.UsedDays - dto.CarryOutDays;

            var balance = new LeaveBalance
            {
                EmployeeId = dto.EmployeeId,
                CompanyId = dto.CompanyId,
                LeaveTypeId = dto.LeaveTypeId,
                Year = dto.Year,
                Month = dto.Month,
                OpeningDays = dto.OpeningDays,
                AccruedDays = dto.AccruedDays,
                UsedDays = dto.UsedDays,
                CarryInDays = dto.CarryInDays,
                CarryOutDays = dto.CarryOutDays,
                ClosingDays = closingDays,
                CarryoverExpiresOn = dto.CarryoverExpiresOn,
                LastRecalculatedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.LeaveBalances.Add(balance);
            await _db.SaveChangesAsync();

            // Logger l'événement
            await _leaveEventLogService.LogEmployeeEventAsync(
                dto.CompanyId,
                dto.EmployeeId,
                LeaveEventLogService.EventNames.BalanceCreated,
                null,
                $"Solde créé pour LeaveType {dto.LeaveTypeId}, Year {dto.Year}",
                userId
            );

            return CreatedAtAction(nameof(GetById), new { id = balance.Id }, MapToReadDto(balance));
        }

        /// <summary>
        /// Met à jour un solde de congés
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<LeaveBalanceReadDto>> Update(int id, [FromBody] LeaveBalancePatchDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var balance = await _db.LeaveBalances
                .FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);

            if (balance == null)
                return NotFound(new { Message = "Solde de congés non trouvé" });

            var userId = User.GetUserId();
            var changes = new List<string>();

            // Mise à jour des champs
            if (dto.OpeningDays.HasValue && dto.OpeningDays != balance.OpeningDays)
            {
                changes.Add($"OpeningDays: {balance.OpeningDays} → {dto.OpeningDays}");
                balance.OpeningDays = dto.OpeningDays.Value;
            }

            if (dto.AccruedDays.HasValue && dto.AccruedDays != balance.AccruedDays)
            {
                changes.Add($"AccruedDays: {balance.AccruedDays} → {dto.AccruedDays}");
                balance.AccruedDays = dto.AccruedDays.Value;
            }

            if (dto.UsedDays.HasValue && dto.UsedDays != balance.UsedDays)
            {
                changes.Add($"UsedDays: {balance.UsedDays} → {dto.UsedDays}");
                balance.UsedDays = dto.UsedDays.Value;
            }

            if (dto.CarryInDays.HasValue && dto.CarryInDays != balance.CarryInDays)
            {
                changes.Add($"CarryInDays: {balance.CarryInDays} → {dto.CarryInDays}");
                balance.CarryInDays = dto.CarryInDays.Value;
            }

            if (dto.CarryOutDays.HasValue && dto.CarryOutDays != balance.CarryOutDays)
            {
                changes.Add($"CarryOutDays: {balance.CarryOutDays} → {dto.CarryOutDays}");
                balance.CarryOutDays = dto.CarryOutDays.Value;
            }

            if (dto.CarryoverExpiresOn.HasValue && dto.CarryoverExpiresOn != balance.CarryoverExpiresOn)
            {
                changes.Add($"CarryoverExpiresOn modifié");
                balance.CarryoverExpiresOn = dto.CarryoverExpiresOn;
            }

            if (changes.Any())
            {
                balance.ClosingDays = balance.OpeningDays + balance.AccruedDays + balance.CarryInDays - balance.UsedDays - balance.CarryOutDays;
                balance.ModifiedAt = DateTimeOffset.UtcNow;
                balance.ModifiedBy = userId;
                balance.LastRecalculatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();

                await _leaveEventLogService.LogEmployeeEventAsync(
                    balance.CompanyId,
                    balance.EmployeeId,
                    LeaveEventLogService.EventNames.BalanceAdjusted,
                    null,
                    string.Join(", ", changes),
                    userId
                );
            }

            return Ok(MapToReadDto(balance));
        }

        /// <summary>
        /// Recalcule le solde d'un employé pour un mois donné (Year, Month) via le service (accrual, used, carry-in).
        /// </summary>
        [HttpPost("recalculate/{employeeId}/{year}/{month}")]
        public async Task<ActionResult<object>> Recalculate(int employeeId, int year, int month, [FromQuery] int? companyId = null, [FromQuery] int? leaveTypeId = null)
        {
            if (month < 1 || month > 12)
                return BadRequest(new { Message = "Le mois doit être entre 1 et 12" });

            var userId = User.GetUserId();
            var asOfDate = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

            var balances = await _db.LeaveBalances
                .Where(b => b.EmployeeId == employeeId && b.Year == year && b.Month == month && b.DeletedAt == null)
                .ToListAsync();

            if (!balances.Any())
                return NotFound(new { Message = "Aucun solde trouvé pour cet employé et ce mois. Créez d'abord une demande ou un solde pour ce mois." });

            var recalculatedCount = 0;
            foreach (var b in balances)
            {
                if (companyId.HasValue && b.CompanyId != companyId.Value) continue;
                if (leaveTypeId.HasValue && b.LeaveTypeId != leaveTypeId.Value) continue;
                var recalc = await _leaveBalanceService.RecalculateAsync(b.CompanyId, employeeId, b.LeaveTypeId, asOfDate, userId);
                if (recalc.Success) recalculatedCount++;
            }

            var firstBalance = balances.First();
            if (recalculatedCount > 0)
                await _leaveEventLogService.LogEmployeeEventAsync(firstBalance.CompanyId, employeeId, LeaveEventLogService.EventNames.BalanceRecalculated, null, $"{recalculatedCount} solde(s) recalculé(s) pour {year}-{month:D2}", userId);

            return Ok(new { Message = $"{recalculatedCount} solde(s) recalculé(s)", RecalculatedCount = recalculatedCount, TotalBalances = balances.Count });
        }

        /// <summary>
        /// Supprime un solde de congés (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var balance = await _db.LeaveBalances
                .FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);

            if (balance == null)
                return NotFound(new { Message = "Solde de congés non trouvé" });

            var userId = User.GetUserId();
            balance.DeletedAt = DateTimeOffset.UtcNow;
            balance.DeletedBy = userId;

            await _db.SaveChangesAsync();

            // Logger l'événement
            await _leaveEventLogService.LogEmployeeEventAsync(
                balance.CompanyId,
                balance.EmployeeId,
                LeaveEventLogService.EventNames.BalanceUpdated,
                $"Solde ID {balance.Id} supprimé",
                null,
                userId
            );

            return NoContent();
        }
    }
}
