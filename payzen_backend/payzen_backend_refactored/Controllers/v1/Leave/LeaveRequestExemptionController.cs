using payzen_backend.Models.Leave;
using payzen_backend.Models.Leave.Dtos;
using payzen_backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using payzen_backend.Data;
using payzen_backend.Services;
using payzen_backend.Models.Common.LeaveStatus;

namespace payzen_backend.Controllers.v1.Leave
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/leave-request-exemptions")]
    [ApiController]
    [Authorize]
    public class LeaveRequestExemptionController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly LeaveEventLogService _leaveEventLogService;

        public LeaveRequestExemptionController(AppDbContext db, LeaveEventLogService leaveEventLogService)
        {
            _db = db;
            _leaveEventLogService = leaveEventLogService;
        }

        /// <summary>
        /// Récupère les exemptions d'une demande de congé
        /// </summary>
        [HttpGet("by-request/{leaveRequestId}")]
        public async Task<ActionResult<IEnumerable<LeaveRequestExemptionReadDto>>> GetByRequest(int leaveRequestId)
        {
            var exemptions = await _db.LeaveRequestExemptions
                .AsNoTracking()
                .Where(e => e.LeaveRequestId == leaveRequestId)
                .OrderBy(e => e.ExemptionDate)
                .Select(e => new LeaveRequestExemptionReadDto
                {
                    Id = e.Id,
                    LeaveRequestId = e.LeaveRequestId,
                    ExemptionDate = e.ExemptionDate,
                    ReasonType = e.ReasonType,
                    CountsAsLeaveDay = e.CountsAsLeaveDay,
                    HolidayId = e.HolidayId,
                    EmployeeAbsenceId = e.EmployeeAbsenceId,
                    Note = e.Note,
                    CreatedAt = e.CreatedAt,
                    CreatedBy = e.CreatedBy
                })
                .ToListAsync();

            return Ok(exemptions);
        }

        /// <summary>
        /// Récupère une exemption par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveRequestExemptionReadDto>> GetById(int id)
        {
            var exemption = await _db.LeaveRequestExemptions
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new LeaveRequestExemptionReadDto
                {
                    Id = e.Id,
                    LeaveRequestId = e.LeaveRequestId,
                    ExemptionDate = e.ExemptionDate,
                    ReasonType = e.ReasonType,
                    CountsAsLeaveDay = e.CountsAsLeaveDay,
                    HolidayId = e.HolidayId,
                    EmployeeAbsenceId = e.EmployeeAbsenceId,
                    Note = e.Note,
                    CreatedAt = e.CreatedAt,
                    CreatedBy = e.CreatedBy
                })
                .FirstOrDefaultAsync();

            if (exemption == null)
                return NotFound(new { Message = "Exemption non trouvée" });

            return Ok(exemption);
        }

        /// <summary>
        /// Ajoute une exemption à une demande de congé
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LeaveRequestExemptionReadDto>> Create([FromBody] LeaveRequestExemptionCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Vérifier que la LeaveRequest existe
            var leaveRequest = await _db.LeaveRequests
                .FirstOrDefaultAsync(lr => lr.Id == dto.LeaveRequestId && lr.DeletedAt == null);

            if (leaveRequest == null)
            {
                return NotFound(new { Message = "Demande de congé non trouvée" });
            }

            // Validation métier: ExemptionDate doit être entre StartDate et EndDate
            if (dto.ExemptionDate < leaveRequest.StartDate || dto.ExemptionDate > leaveRequest.EndDate)
            {
                return BadRequest(new { Message = "La date d'exemption doit être comprise entre les dates de début et de fin de la demande" });
            }

            // Validation métier: si ReasonType = Holiday, HolidayId obligatoire
            if (dto.ReasonType == LeaveExemptionReasonType.Holiday && !dto.HolidayId.HasValue)
            {
                return BadRequest(new { Message = "HolidayId est obligatoire pour le type Holiday" });
            }

            // Validation métier: si ReasonType = EmployeeAbsence, EmployeeAbsenceId obligatoire
            if (dto.ReasonType == LeaveExemptionReasonType.EmployeeAbsence && !dto.EmployeeAbsenceId.HasValue)
            {
                return BadRequest(new { Message = "EmployeeAbsenceId est obligatoire pour le type EmployeeAbsence" });
            }

            // Vérifier qu'il n'y a pas déjà une exemption pour cette date
            var exemptionExists = await _db.LeaveRequestExemptions
                .AnyAsync(e => e.LeaveRequestId == dto.LeaveRequestId && e.ExemptionDate == dto.ExemptionDate);

            if (exemptionExists)
            {
                return Conflict(new { Message = "Une exemption existe déjà pour cette date" });
            }

            var userId = User.GetUserId();

            var exemption = new LeaveRequestExemption
            {
                LeaveRequestId = dto.LeaveRequestId,
                ExemptionDate = dto.ExemptionDate,
                ReasonType = dto.ReasonType,
                CountsAsLeaveDay = dto.CountsAsLeaveDay,
                HolidayId = dto.HolidayId,
                EmployeeAbsenceId = dto.EmployeeAbsenceId,
                Note = dto.Note?.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.LeaveRequestExemptions.Add(exemption);
            await _db.SaveChangesAsync();

            // Logger l'événement
            await _leaveEventLogService.LogLeaveRequestEventAsync(
                leaveRequest.CompanyId,
                leaveRequest.EmployeeId,
                leaveRequest.Id,
                LeaveEventLogService.EventNames.ExemptionAdded,
                null,
                $"Exemption ajoutée pour le {dto.ExemptionDate}",
                userId
            );

            var readDto = new LeaveRequestExemptionReadDto
            {
                Id = exemption.Id,
                LeaveRequestId = exemption.LeaveRequestId,
                ExemptionDate = exemption.ExemptionDate,
                ReasonType = exemption.ReasonType,
                CountsAsLeaveDay = exemption.CountsAsLeaveDay,
                HolidayId = exemption.HolidayId,
                EmployeeAbsenceId = exemption.EmployeeAbsenceId,
                Note = exemption.Note,
                CreatedAt = exemption.CreatedAt,
                CreatedBy = exemption.CreatedBy
            };

            return CreatedAtAction(nameof(GetById), new { id = exemption.Id }, readDto);
        }

        /// <summary>
        /// Met à jour une exemption
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<LeaveRequestExemptionReadDto>> Update(int id, [FromBody] LeaveRequestExemptionPatchDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exemption = await _db.LeaveRequestExemptions
                .Include(e => e.LeaveRequest)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exemption == null)
                return NotFound(new { Message = "Exemption non trouvée" });

            var userId = User.GetUserId();
            var changes = new List<string>();

            // Mise à jour de la date d'exemption
            if (dto.ExemptionDate.HasValue && dto.ExemptionDate != exemption.ExemptionDate)
            {
                // Validation: doit être dans la période de la demande
                if (dto.ExemptionDate < exemption.LeaveRequest.StartDate || dto.ExemptionDate > exemption.LeaveRequest.EndDate)
                {
                    return BadRequest(new { Message = "La date d'exemption doit être comprise entre les dates de la demande" });
                }

                changes.Add($"ExemptionDate: {exemption.ExemptionDate} → {dto.ExemptionDate}");
                exemption.ExemptionDate = dto.ExemptionDate.Value;
            }

            // Mise à jour du type de raison
            if (dto.ReasonType.HasValue && dto.ReasonType != exemption.ReasonType)
            {
                changes.Add($"ReasonType: {exemption.ReasonType} → {dto.ReasonType}");
                exemption.ReasonType = dto.ReasonType.Value;
            }

            // Mise à jour de CountsAsLeaveDay
            if (dto.CountsAsLeaveDay.HasValue && dto.CountsAsLeaveDay != exemption.CountsAsLeaveDay)
            {
                changes.Add($"CountsAsLeaveDay: {exemption.CountsAsLeaveDay} → {dto.CountsAsLeaveDay}");
                exemption.CountsAsLeaveDay = dto.CountsAsLeaveDay.Value;
            }

            // Mise à jour de HolidayId
            if (dto.HolidayId.HasValue && dto.HolidayId != exemption.HolidayId)
            {
                changes.Add($"HolidayId modifié");
                exemption.HolidayId = dto.HolidayId;
            }

            // Mise à jour de EmployeeAbsenceId
            if (dto.EmployeeAbsenceId.HasValue && dto.EmployeeAbsenceId != exemption.EmployeeAbsenceId)
            {
                changes.Add($"EmployeeAbsenceId modifié");
                exemption.EmployeeAbsenceId = dto.EmployeeAbsenceId;
            }

            // Mise à jour de la note
            if (dto.Note != null && dto.Note != exemption.Note)
            {
                changes.Add($"Note modifiée");
                exemption.Note = dto.Note.Trim();
            }

            if (changes.Any())
            {
                await _db.SaveChangesAsync();

                // Logger l'événement
                await _leaveEventLogService.LogLeaveRequestEventAsync(
                    exemption.LeaveRequest.CompanyId,
                    exemption.LeaveRequest.EmployeeId,
                    exemption.LeaveRequestId,
                    LeaveEventLogService.EventNames.ExemptionUpdated,
                    null,
                    string.Join(", ", changes),
                    userId
                );
            }

            var readDto = new LeaveRequestExemptionReadDto
            {
                Id = exemption.Id,
                LeaveRequestId = exemption.LeaveRequestId,
                ExemptionDate = exemption.ExemptionDate,
                ReasonType = exemption.ReasonType,
                CountsAsLeaveDay = exemption.CountsAsLeaveDay,
                HolidayId = exemption.HolidayId,
                EmployeeAbsenceId = exemption.EmployeeAbsenceId,
                Note = exemption.Note,
                CreatedAt = exemption.CreatedAt,
                CreatedBy = exemption.CreatedBy
            };

            return Ok(readDto);
        }

        /// <summary>
        /// Supprime une exemption
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var exemption = await _db.LeaveRequestExemptions
                .Include(e => e.LeaveRequest)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exemption == null)
                return NotFound(new { Message = "Exemption non trouvée" });

            var userId = User.GetUserId();
            var leaveRequestId = exemption.LeaveRequestId;
            var companyId = exemption.LeaveRequest.CompanyId;
            var employeeId = exemption.LeaveRequest.EmployeeId;

            _db.LeaveRequestExemptions.Remove(exemption);
            await _db.SaveChangesAsync();

            // Logger l'événement
            await _leaveEventLogService.LogLeaveRequestEventAsync(
                companyId,
                employeeId,
                leaveRequestId,
                LeaveEventLogService.EventNames.ExemptionRemoved,
                $"Exemption ID {id} supprimée",
                null,
                userId
            );

            return NoContent();
        }
    }
}
