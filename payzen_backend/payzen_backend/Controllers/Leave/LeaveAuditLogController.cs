using payzen_backend.Models.Leave.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using payzen_backend.Data;

namespace payzen_backend.Controllers.Leave
{
    [Route("api/leave-audit-logs")]
    [ApiController]
    [Authorize]
    public class LeaveAuditLogController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LeaveAuditLogController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Récupère les logs d'audit avec filtres
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeaveAuditLogReadDto>>> GetAll(
            [FromQuery] int? companyId = null,
            [FromQuery] int? employeeId = null,
            [FromQuery] int? leaveRequestId = null,
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            var query = _db.LeaveAuditLogs
                .AsNoTracking();

            if (companyId.HasValue)
            {
                query = query.Where(log => log.CompanyId == companyId.Value);
            }

            if (employeeId.HasValue)
            {
                query = query.Where(log => log.EmployeeId == employeeId.Value);
            }

            if (leaveRequestId.HasValue)
            {
                query = query.Where(log => log.LeaveRequestId == leaveRequestId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(log => log.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(log => log.CreatedAt <= endDate.Value);
            }

            var logs = await query
                .OrderByDescending(log => log.CreatedAt)
                .Select(log => new LeaveAuditLogReadDto
                {
                    Id = log.Id,
                    CompanyId = log.CompanyId,
                    EmployeeId = log.EmployeeId,
                    LeaveRequestId = log.LeaveRequestId,
                    EventName = log.EventName,
                    OldValue = log.OldValue,
                    NewValue = log.NewValue,
                    CreatedAt = log.CreatedAt,
                    CreatedBy = log.CreatedBy
                })
                .Take(1000) // Limiter à 1000 résultats pour éviter de surcharger
                .ToListAsync();

            return Ok(logs);
        }

        /// <summary>
        /// Récupère un log d'audit par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveAuditLogReadDto>> GetById(int id)
        {
            var log = await _db.LeaveAuditLogs
                .AsNoTracking()
                .Where(log => log.Id == id)
                .Select(log => new LeaveAuditLogReadDto
                {
                    Id = log.Id,
                    CompanyId = log.CompanyId,
                    EmployeeId = log.EmployeeId,
                    LeaveRequestId = log.LeaveRequestId,
                    EventName = log.EventName,
                    OldValue = log.OldValue,
                    NewValue = log.NewValue,
                    CreatedAt = log.CreatedAt,
                    CreatedBy = log.CreatedBy
                })
                .FirstOrDefaultAsync();

            if (log == null)
                return NotFound(new { Message = "Log d'audit non trouvé" });

            return Ok(log);
        }

        /// <summary>
        /// Récupère les logs d'un employé
        /// </summary>
        [HttpGet("by-employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<LeaveAuditLogReadDto>>> GetByEmployee(
            int employeeId,
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            var query = _db.LeaveAuditLogs
                .AsNoTracking()
                .Where(log => log.EmployeeId == employeeId);

            if (startDate.HasValue)
            {
                query = query.Where(log => log.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(log => log.CreatedAt <= endDate.Value);
            }

            var logs = await query
                .OrderByDescending(log => log.CreatedAt)
                .Select(log => new LeaveAuditLogReadDto
                {
                    Id = log.Id,
                    CompanyId = log.CompanyId,
                    EmployeeId = log.EmployeeId,
                    LeaveRequestId = log.LeaveRequestId,
                    EventName = log.EventName,
                    OldValue = log.OldValue,
                    NewValue = log.NewValue,
                    CreatedAt = log.CreatedAt,
                    CreatedBy = log.CreatedBy
                })
                .Take(500)
                .ToListAsync();

            return Ok(logs);
        }

        /// <summary>
        /// Récupère les logs d'une demande de congé
        /// </summary>
        [HttpGet("by-request/{leaveRequestId}")]
        public async Task<ActionResult<IEnumerable<LeaveAuditLogReadDto>>> GetByRequest(int leaveRequestId)
        {
            var logs = await _db.LeaveAuditLogs
                .AsNoTracking()
                .Where(log => log.LeaveRequestId == leaveRequestId)
                .OrderBy(log => log.CreatedAt)
                .Select(log => new LeaveAuditLogReadDto
                {
                    Id = log.Id,
                    CompanyId = log.CompanyId,
                    EmployeeId = log.EmployeeId,
                    LeaveRequestId = log.LeaveRequestId,
                    EventName = log.EventName,
                    OldValue = log.OldValue,
                    NewValue = log.NewValue,
                    CreatedAt = log.CreatedAt,
                    CreatedBy = log.CreatedBy
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}
