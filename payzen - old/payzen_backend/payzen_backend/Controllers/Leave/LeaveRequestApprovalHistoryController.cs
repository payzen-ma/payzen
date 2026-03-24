using payzen_backend.Models.Leave.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using payzen_backend.Data;

namespace payzen_backend.Controllers.Leave
{
    [Route("api/leave-request-approval-history")]
    [ApiController]
    [Authorize]
    public class LeaveRequestApprovalHistoryController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LeaveRequestApprovalHistoryController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Récupère l'historique d'approbation d'une demande de congé
        /// </summary>
        [HttpGet("by-request/{leaveRequestId}")]
        public async Task<ActionResult<IEnumerable<LeaveRequestApprovalHistoryReadDto>>> GetByRequest(int leaveRequestId)
        {
            var history = await _db.LeaveRequestApprovalHistories
                .AsNoTracking()
                .Where(h => h.LeaveRequestId == leaveRequestId)
                .OrderBy(h => h.ActionAt)
                .Select(h => new LeaveRequestApprovalHistoryReadDto
                {
                    Id = h.Id,
                    LeaveRequestId = h.LeaveRequestId,
                    Action = h.Action,
                    ActionAt = h.ActionAt,
                    ActionBy = h.ActionBy,
                    Comment = h.Comment
                })
                .ToListAsync();

            return Ok(history);
        }

        /// <summary>
        /// Récupère une entrée d'historique par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveRequestApprovalHistoryReadDto>> GetById(int id)
        {
            var history = await _db.LeaveRequestApprovalHistories
                .AsNoTracking()
                .Where(h => h.Id == id)
                .Select(h => new LeaveRequestApprovalHistoryReadDto
                {
                    Id = h.Id,
                    LeaveRequestId = h.LeaveRequestId,
                    Action = h.Action,
                    ActionAt = h.ActionAt,
                    ActionBy = h.ActionBy,
                    Comment = h.Comment
                })
                .FirstOrDefaultAsync();

            if (history == null)
                return NotFound(new { Message = "Historique d'approbation non trouvé" });

            return Ok(history);
        }
    }
}
