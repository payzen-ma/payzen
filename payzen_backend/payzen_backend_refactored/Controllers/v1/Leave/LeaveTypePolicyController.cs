using payzen_backend.Models.Leave;
using payzen_backend.Models.Leave.Dtos;
using payzen_backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using payzen_backend.Data;
using payzen_backend.Services;

namespace payzen_backend.Controllers.v1.Leave
{
    [Route("api/v{version:apiVersion}/leave-type-policies")]
    [ApiController]
    [ApiVersion("1.0")]
    //[Authorize]
    public class LeaveTypePolicyController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly LeaveEventLogService _leaveEventLogService;

        public LeaveTypePolicyController(AppDbContext db, LeaveEventLogService leaveEventLogService)
        {
            _db = db;
            _leaveEventLogService = leaveEventLogService;
        }

        /// <summary>
        /// Récupère toutes les politiques de congés
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeaveTypePolicyReadDto>>> GetAll([FromQuery] int? companyId = null)
        {
            var query = _db.LeaveTypePolicies
                .AsNoTracking()
                .Where(p => p.DeletedAt == null);

            if (companyId.HasValue)
            {
                query = query.Where(p => p.CompanyId == companyId.Value);
            }

            var policies = await query
                .OrderBy(p => p.CompanyId)
                .ThenBy(p => p.LeaveTypeId)
                .Select(p => new LeaveTypePolicyReadDto
                {
                    Id = p.Id,
                    CompanyId = p.CompanyId,
                    LeaveTypeId = p.LeaveTypeId,
                    IsEnabled = p.IsEnabled,
                    AccrualMethod = p.AccrualMethod,
                    DaysPerMonthAdult = p.DaysPerMonthAdult,
                    DaysPerMonthMinor = p.DaysPerMonthMinor,
                    RequiresBalance = p.RequiresBalance,
                    RequiresEligibility6Months = p.RequiresEligibility6Months,
                    BonusDaysPerYearAfter5Years = p.BonusDaysPerYearAfter5Years,
                    AnnualCapDays = p.AnnualCapDays,
                    AllowCarryover = p.AllowCarryover,
                    MaxCarryoverYears = p.MaxCarryoverYears,
                    MinConsecutiveDays = p.MinConsecutiveDays,
                    UseWorkingCalendar = p.UseWorkingCalendar
                })
                .ToListAsync();

            return Ok(policies);
        }

        /// <summary>
        /// Récupère une politique par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveTypePolicyReadDto>> GetById(int id)
        {
            var policy = await _db.LeaveTypePolicies
                .AsNoTracking()
                .Where(p => p.Id == id && p.DeletedAt == null)
                .Select(p => new LeaveTypePolicyReadDto
                {
                    Id = p.Id,
                    CompanyId = p.CompanyId,
                    LeaveTypeId = p.LeaveTypeId,
                    IsEnabled = p.IsEnabled,
                    AccrualMethod = p.AccrualMethod,
                    DaysPerMonthAdult = p.DaysPerMonthAdult,
                    DaysPerMonthMinor = p.DaysPerMonthMinor,
                    RequiresBalance = p.RequiresBalance,
                    RequiresEligibility6Months = p.RequiresEligibility6Months,
                    BonusDaysPerYearAfter5Years = p.BonusDaysPerYearAfter5Years,
                    AnnualCapDays = p.AnnualCapDays,
                    AllowCarryover = p.AllowCarryover,
                    MaxCarryoverYears = p.MaxCarryoverYears,
                    MinConsecutiveDays = p.MinConsecutiveDays,
                    UseWorkingCalendar = p.UseWorkingCalendar
                })
                .FirstOrDefaultAsync();

            if (policy == null)
                return NotFound(new { Message = "Politique de congés non trouvée" });

            return Ok(policy);
        }

        /// <summary>
        /// Récupère les politiques par type de congé
        /// </summary>
        [HttpGet("by-leave-type/{leaveTypeId}")]
        public async Task<ActionResult<IEnumerable<LeaveTypePolicyReadDto>>> GetByLeaveType(int leaveTypeId)
        {
            var policies = await _db.LeaveTypePolicies
                .AsNoTracking()
                .Where(p => p.LeaveTypeId == leaveTypeId && p.DeletedAt == null)
                .Select(p => new LeaveTypePolicyReadDto
                {
                    Id = p.Id,
                    CompanyId = p.CompanyId,
                    LeaveTypeId = p.LeaveTypeId,
                    IsEnabled = p.IsEnabled,
                    AccrualMethod = p.AccrualMethod,
                    DaysPerMonthAdult = p.DaysPerMonthAdult,
                    DaysPerMonthMinor = p.DaysPerMonthMinor,
                    RequiresBalance = p.RequiresBalance,
                    RequiresEligibility6Months = p.RequiresEligibility6Months,
                    BonusDaysPerYearAfter5Years = p.BonusDaysPerYearAfter5Years,
                    AnnualCapDays = p.AnnualCapDays,
                    AllowCarryover = p.AllowCarryover,
                    MaxCarryoverYears = p.MaxCarryoverYears,
                    MinConsecutiveDays = p.MinConsecutiveDays,
                    UseWorkingCalendar = p.UseWorkingCalendar
                })
                .ToListAsync();

            return Ok(policies);
        }

        /// <summary>
        /// Crée une nouvelle politique de congés
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LeaveTypePolicyReadDto>> Create([FromBody] LeaveTypePolicyCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Vérifier que le LeaveType existe
            var leaveTypeExists = await _db.LeaveTypes
                .AnyAsync(lt => lt.Id == dto.LeaveTypeId && lt.DeletedAt == null);

            if (!leaveTypeExists)
            {
                return NotFound(new { Message = "Type de congé non trouvé" });
            }

            // Vérifier l'unicité: une seule policy active par (CompanyId, LeaveTypeId)
            var policyExists = await _db.LeaveTypePolicies
                .AnyAsync(p => p.CompanyId == dto.CompanyId && p.LeaveTypeId == dto.LeaveTypeId && p.DeletedAt == null);

            if (policyExists)
            {
                return Conflict(new { Message = "Une politique existe déjà pour ce type de congé et cette entreprise" });
            }

            // Validation métier
            if (dto.MinConsecutiveDays < 0)
            {
                return BadRequest(new { Message = "MinConsecutiveDays doit être supérieur ou égal à 0" });
            }

            var userId = User.GetUserId();

            var policy = new LeaveTypePolicy
            {
                CompanyId = dto.CompanyId,
                LeaveTypeId = dto.LeaveTypeId,
                IsEnabled = dto.IsEnabled,
                AccrualMethod = dto.AccrualMethod,
                DaysPerMonthAdult = dto.DaysPerMonthAdult,
                DaysPerMonthMinor = dto.DaysPerMonthMinor,
                RequiresBalance = dto.RequiresBalance,
                RequiresEligibility6Months = dto.RequiresEligibility6Months,
                BonusDaysPerYearAfter5Years = dto.BonusDaysPerYearAfter5Years,
                AnnualCapDays = dto.AnnualCapDays,
                AllowCarryover = dto.AllowCarryover,
                MaxCarryoverYears = dto.MaxCarryoverYears,
                MinConsecutiveDays = dto.MinConsecutiveDays,
                UseWorkingCalendar = dto.UseWorkingCalendar,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.LeaveTypePolicies.Add(policy);
            await _db.SaveChangesAsync();

            // Logger l'événement (companyId explicite)
            await _leaveEventLogService.LogSimpleEventAsync(
                dto.CompanyId ?? 0,
                LeaveEventLogService.EventNames.PolicyCreated,
                null,
                $"Politique créée pour LeaveType {dto.LeaveTypeId}",
                userId
            );

            var readDto = new LeaveTypePolicyReadDto
            {
                Id = policy.Id,
                CompanyId = policy.CompanyId,
                LeaveTypeId = policy.LeaveTypeId,
                IsEnabled = policy.IsEnabled,
                AccrualMethod = policy.AccrualMethod,
                DaysPerMonthAdult = policy.DaysPerMonthAdult,
                DaysPerMonthMinor = policy.DaysPerMonthMinor,
                RequiresBalance = policy.RequiresBalance,
                RequiresEligibility6Months = policy.RequiresEligibility6Months,
                BonusDaysPerYearAfter5Years = policy.BonusDaysPerYearAfter5Years,
                AnnualCapDays = policy.AnnualCapDays,
                AllowCarryover = policy.AllowCarryover,
                MaxCarryoverYears = policy.MaxCarryoverYears,
                MinConsecutiveDays = policy.MinConsecutiveDays,
                UseWorkingCalendar = policy.UseWorkingCalendar
            };

            return CreatedAtAction(nameof(GetById), new { id = policy.Id }, readDto);
        }

        /// <summary>
        /// Met à jour une politique de congés
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<LeaveTypePolicyReadDto>> Update(int id, [FromBody] LeaveTypePolicyPatchDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var policy = await _db.LeaveTypePolicies
                .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

            if (policy == null)
                return NotFound(new { Message = "Politique de congés non trouvée" });

            var userId = User.GetUserId();
            var changes = new List<string>();

            // Mise à jour des champs
            if (dto.IsEnabled.HasValue && dto.IsEnabled != policy.IsEnabled)
            {
                changes.Add($"IsEnabled: {policy.IsEnabled} → {dto.IsEnabled}");
                policy.IsEnabled = dto.IsEnabled.Value;
            }

            if (dto.AccrualMethod.HasValue && dto.AccrualMethod != policy.AccrualMethod)
            {
                changes.Add($"AccrualMethod: {policy.AccrualMethod} → {dto.AccrualMethod}");
                policy.AccrualMethod = dto.AccrualMethod.Value;
            }

            if (dto.DaysPerMonthAdult.HasValue && dto.DaysPerMonthAdult != policy.DaysPerMonthAdult)
            {
                changes.Add($"DaysPerMonth: {policy.DaysPerMonthAdult} → {dto.DaysPerMonthAdult}");
                policy.DaysPerMonthAdult = dto.DaysPerMonthAdult.Value;
            }

            if (dto.DaysPerMonthMinor.HasValue && dto.DaysPerMonthMinor != policy.DaysPerMonthMinor)
            {
                changes.Add($"DaysPerMonthMinor: {policy.DaysPerMonthMinor} → {dto.DaysPerMonthMinor}");
                policy.DaysPerMonthMinor = dto.DaysPerMonthMinor.Value;
            }

            if (dto.BonusDaysPerYearAfter5Years.HasValue && dto.BonusDaysPerYearAfter5Years != policy.BonusDaysPerYearAfter5Years)
            {
                changes.Add($"DaysPerService5Years: {policy.BonusDaysPerYearAfter5Years} → {dto.BonusDaysPerYearAfter5Years}");
                policy.BonusDaysPerYearAfter5Years = dto.BonusDaysPerYearAfter5Years.Value;
            }

            

            if (dto.AnnualCapDays.HasValue && dto.AnnualCapDays != policy.AnnualCapDays)
            {
                changes.Add($"AnnualCapDays: {policy.AnnualCapDays} → {dto.AnnualCapDays}");
                policy.AnnualCapDays = dto.AnnualCapDays.Value;
            }

            if (dto.AllowCarryover.HasValue && dto.AllowCarryover != policy.AllowCarryover)
            {
                changes.Add($"AllowCarryover: {policy.AllowCarryover} → {dto.AllowCarryover}");
                policy.AllowCarryover = dto.AllowCarryover.Value;
            }

            if (dto.MaxCarryoverYears.HasValue && dto.MaxCarryoverYears != policy.MaxCarryoverYears)
            {
                changes.Add($"MaxCarryoverYears: {policy.MaxCarryoverYears} → {dto.MaxCarryoverYears}");
                policy.MaxCarryoverYears = dto.MaxCarryoverYears.Value;
            }

            if (dto.MinConsecutiveDays.HasValue && dto.MinConsecutiveDays != policy.MinConsecutiveDays)
            {
                if (dto.MinConsecutiveDays < 0)
                {
                    return BadRequest(new { Message = "MinConsecutiveDays doit être supérieur ou égal à 0" });
                }
                changes.Add($"MinConsecutiveDays: {policy.MinConsecutiveDays} → {dto.MinConsecutiveDays}");
                policy.MinConsecutiveDays = dto.MinConsecutiveDays.Value;
            }
            if (dto.RequiresEligibility6Months.HasValue && dto.RequiresEligibility6Months != policy.RequiresEligibility6Months)
            {
                changes.Add($"RequiresEligibility6Months: {policy.RequiresEligibility6Months} → {dto.RequiresEligibility6Months}");
                policy.RequiresEligibility6Months = dto.RequiresEligibility6Months.Value;
            }

            if (dto.RequiresBalance.HasValue && dto.RequiresBalance != policy.RequiresBalance)
            {
                changes.Add($"RequiresBalance: {policy.RequiresBalance} → {dto.RequiresBalance}");
                policy.RequiresBalance = dto.RequiresBalance.Value;
            }

            if (dto.UseWorkingCalendar.HasValue && dto.UseWorkingCalendar != policy.UseWorkingCalendar)
            {
                changes.Add($"UseWorkingCalendar: {policy.UseWorkingCalendar} → {dto.UseWorkingCalendar}");
                policy.UseWorkingCalendar = dto.UseWorkingCalendar.Value;
            }

            if (changes.Any())
            {
                policy.ModifiedAt = DateTimeOffset.UtcNow;
                policy.ModifiedBy = userId;
                await _db.SaveChangesAsync();

                await _leaveEventLogService.LogSimpleEventAsync(
                    policy.CompanyId ?? 0,
                    LeaveEventLogService.EventNames.PolicyUpdated,
                    null,
                    string.Join(", ", changes),
                    userId
                );
            }

            var readDto = new LeaveTypePolicyReadDto
            {
                Id = policy.Id,
                CompanyId = policy.CompanyId,
                LeaveTypeId = policy.LeaveTypeId,
                IsEnabled = policy.IsEnabled,
                AccrualMethod = policy.AccrualMethod,
                DaysPerMonthAdult = policy.DaysPerMonthAdult,
                DaysPerMonthMinor = policy.DaysPerMonthMinor,
                RequiresBalance = policy.RequiresBalance,
                RequiresEligibility6Months = policy.RequiresEligibility6Months,
                BonusDaysPerYearAfter5Years = policy.BonusDaysPerYearAfter5Years,
                AnnualCapDays = policy.AnnualCapDays,
                AllowCarryover = policy.AllowCarryover,
                MaxCarryoverYears = policy.MaxCarryoverYears,
                MinConsecutiveDays = policy.MinConsecutiveDays,
                UseWorkingCalendar = policy.UseWorkingCalendar
            };

            return Ok(readDto);
        }

        /// <summary>
        /// Supprime une politique de congés (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var policy = await _db.LeaveTypePolicies
                .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

            if (policy == null)
                return NotFound(new { Message = "Politique de congés non trouvée" });

            var userId = User.GetUserId();
            policy.DeletedAt = DateTimeOffset.UtcNow;
            policy.DeletedBy = userId;

            await _db.SaveChangesAsync();

            await _leaveEventLogService.LogSimpleEventAsync(
                policy.CompanyId ?? 0,
                LeaveEventLogService.EventNames.PolicyDeleted,
                $"Politique ID {policy.Id} supprimée",
                null,
                userId
            );

            return NoContent();
        }
    }
}
