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
    [Route("api/v{version:apiVersion}/leave-carryover-agreements")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class LeaveCarryOverAgreementController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly LeaveEventLogService _leaveEventLogService;

        public LeaveCarryOverAgreementController(AppDbContext db, LeaveEventLogService leaveEventLogService)
        {
            _db = db;
            _leaveEventLogService = leaveEventLogService;
        }

        /// <summary>
        /// Récupère tous les accords de report avec filtres
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeaveCarryOverAgreementReadDto>>> GetAll(
            [FromQuery] int? companyId = null,
            [FromQuery] int? employeeId = null)
        {
            var query = _db.LeaveCarryOverAgreements
                .AsNoTracking()
                .Where(a => a.DeletedAt == null);

            if (companyId.HasValue)
            {
                query = query.Where(a => a.CompanyId == companyId.Value);
            }

            if (employeeId.HasValue)
            {
                query = query.Where(a => a.EmployeeId == employeeId.Value);
            }

            var agreements = await query
                .OrderByDescending(a => a.ToYear)
                .ThenBy(a => a.EmployeeId)
                .Select(a => new LeaveCarryOverAgreementReadDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    CompanyId = a.CompanyId,
                    LeaveTypeId = a.LeaveTypeId,
                    FromYear = a.FromYear,
                    ToYear = a.ToYear,
                    AgreementDate = a.AgreementDate,
                    AgreementDocRef = a.AgreementDocRef,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(agreements);
        }

        /// <summary>
        /// Récupère un accord de report par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveCarryOverAgreementReadDto>> GetById(int id)
        {
            var agreement = await _db.LeaveCarryOverAgreements
                .AsNoTracking()
                .Where(a => a.Id == id && a.DeletedAt == null)
                .Select(a => new LeaveCarryOverAgreementReadDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    CompanyId = a.CompanyId,
                    LeaveTypeId = a.LeaveTypeId,
                    FromYear = a.FromYear,
                    ToYear = a.ToYear,
                    AgreementDate = a.AgreementDate,
                    AgreementDocRef = a.AgreementDocRef,
                    CreatedAt = a.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (agreement == null)
                return NotFound(new { Message = "Accord de report non trouvé" });

            return Ok(agreement);
        }

        /// <summary>
        /// Récupère les accords d'un employé
        /// </summary>
        [HttpGet("employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<LeaveCarryOverAgreementReadDto>>> GetByEmployee(int employeeId)
        {
            var agreements = await _db.LeaveCarryOverAgreements
                .AsNoTracking()
                .Where(a => a.EmployeeId == employeeId && a.DeletedAt == null)
                .OrderByDescending(a => a.ToYear)
                .Select(a => new LeaveCarryOverAgreementReadDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    CompanyId = a.CompanyId,
                    LeaveTypeId = a.LeaveTypeId,
                    FromYear = a.FromYear,
                    ToYear = a.ToYear,
                    AgreementDate = a.AgreementDate,
                    AgreementDocRef = a.AgreementDocRef,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(agreements);
        }

        /// <summary>
        /// Crée un nouvel accord de report
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LeaveCarryOverAgreementReadDto>> Create([FromBody] LeaveCarryOverAgreementCreateDto dto)
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
            var leaveType = await _db.LeaveTypes
                .Include(lt => lt.Policies)
                .FirstOrDefaultAsync(lt => lt.Id == dto.LeaveTypeId && lt.DeletedAt == null);

            if (leaveType == null)
            {
                return NotFound(new { Message = "Type de congé non trouvé" });
            }

            // Validation métier: FromYear < ToYear
            if (dto.FromYear >= dto.ToYear)
            {
                return BadRequest(new { Message = "FromYear doit être antérieur à ToYear" });
            }

            // Vérifier que la policy du LeaveType autorise le carryover
            var policy = await _db.LeaveTypePolicies
                .FirstOrDefaultAsync(p => p.LeaveTypeId == dto.LeaveTypeId && 
                                         p.CompanyId == dto.CompanyId && 
                                         p.DeletedAt == null);

            if (policy != null && !policy.AllowCarryover)
            {
                return BadRequest(new { Message = "Le type de congé n'autorise pas le report de congés" });
            }

            // Vérifier le MaxCarryoverYears
            if (policy != null)
            {
                var yearsDifference = dto.ToYear - dto.FromYear;
                if (yearsDifference > policy.MaxCarryoverYears)
                {
                    return BadRequest(new { Message = $"Le report ne peut pas dépasser {policy.MaxCarryoverYears} année(s)" });
                }
            }

            var userId = User.GetUserId();

            var agreement = new LeaveCarryOverAgreement
            {
                EmployeeId = dto.EmployeeId,
                CompanyId = dto.CompanyId,
                LeaveTypeId = dto.LeaveTypeId,
                FromYear = dto.FromYear,
                ToYear = dto.ToYear,
                AgreementDate = dto.AgreementDate,
                AgreementDocRef = dto.AgreementDocRef?.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.LeaveCarryOverAgreements.Add(agreement);
            await _db.SaveChangesAsync();

            // Logger l'événement
            await _leaveEventLogService.LogEmployeeEventAsync(
                dto.CompanyId,
                dto.EmployeeId,
                LeaveEventLogService.EventNames.CarryOverCreated,
                null,
                $"Accord de report créé: {dto.FromYear} → {dto.ToYear}",
                userId
            );

            var readDto = new LeaveCarryOverAgreementReadDto
            {
                Id = agreement.Id,
                EmployeeId = agreement.EmployeeId,
                CompanyId = agreement.CompanyId,
                LeaveTypeId = agreement.LeaveTypeId,
                FromYear = agreement.FromYear,
                ToYear = agreement.ToYear,
                AgreementDate = agreement.AgreementDate,
                AgreementDocRef = agreement.AgreementDocRef,
                CreatedAt = agreement.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = agreement.Id }, readDto);
        }

        /// <summary>
        /// Met à jour un accord de report
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<LeaveCarryOverAgreementReadDto>> Update(int id, [FromBody] LeaveCarryOverAgreementPatchDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var agreement = await _db.LeaveCarryOverAgreements
                .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null);

            if (agreement == null)
                return NotFound(new { Message = "Accord de report non trouvé" });

            var userId = User.GetUserId();
            var changes = new List<string>();

            // Mise à jour de FromYear
            if (dto.FromYear.HasValue && dto.FromYear != agreement.FromYear)
            {
                changes.Add($"FromYear: {agreement.FromYear} → {dto.FromYear}");
                agreement.FromYear = dto.FromYear.Value;
            }

            // Mise à jour de ToYear
            if (dto.ToYear.HasValue && dto.ToYear != agreement.ToYear)
            {
                changes.Add($"ToYear: {agreement.ToYear} → {dto.ToYear}");
                agreement.ToYear = dto.ToYear.Value;
            }

            // Validation: FromYear < ToYear
            if (agreement.FromYear >= agreement.ToYear)
            {
                return BadRequest(new { Message = "FromYear doit être antérieur à ToYear" });
            }

            // Mise à jour de AgreementDate
            if (dto.AgreementDate.HasValue && dto.AgreementDate != agreement.AgreementDate)
            {
                changes.Add($"AgreementDate modifiée");
                agreement.AgreementDate = dto.AgreementDate.Value;
            }

            // Mise à jour de AgreementDocRef
            if (dto.AgreementDocRef != null && dto.AgreementDocRef != agreement.AgreementDocRef)
            {
                changes.Add($"AgreementDocRef modifié");
                agreement.AgreementDocRef = dto.AgreementDocRef.Trim();
            }

            if (changes.Any())
            {
                await _db.SaveChangesAsync();

                // Logger l'événement
                await _leaveEventLogService.LogEmployeeEventAsync(
                    agreement.CompanyId,
                    agreement.EmployeeId,
                    LeaveEventLogService.EventNames.CarryOverUpdated,
                    null,
                    string.Join(", ", changes),
                    userId
                );
            }

            var readDto = new LeaveCarryOverAgreementReadDto
            {
                Id = agreement.Id,
                EmployeeId = agreement.EmployeeId,
                CompanyId = agreement.CompanyId,
                LeaveTypeId = agreement.LeaveTypeId,
                FromYear = agreement.FromYear,
                ToYear = agreement.ToYear,
                AgreementDate = agreement.AgreementDate,
                AgreementDocRef = agreement.AgreementDocRef,
                CreatedAt = agreement.CreatedAt
            };

            return Ok(readDto);
        }

        /// <summary>
        /// Supprime un accord de report (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var agreement = await _db.LeaveCarryOverAgreements
                .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null);

            if (agreement == null)
                return NotFound(new { Message = "Accord de report non trouvé" });

            var userId = User.GetUserId();
            agreement.DeletedAt = DateTimeOffset.UtcNow;
            agreement.DeletedBy = userId;

            await _db.SaveChangesAsync();

            // Logger l'événement
            await _leaveEventLogService.LogEmployeeEventAsync(
                agreement.CompanyId,
                agreement.EmployeeId,
                LeaveEventLogService.EventNames.CarryOverDeleted,
                $"Accord ID {agreement.Id} supprimé",
                null,
                userId
            );

            return NoContent();
        }
    }
}
