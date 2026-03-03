using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Dashboard.Dtos;
using System;
using System.Linq;
using System.Threading.Tasks;
using payzen_backend.Extensions;

namespace payzen_backend.Controllers.Dashboard
{
    [Route("api/dashboard/expert")]
    [ApiController]
    [Authorize]
    public class DashboardExpertController : ControllerBase
    {
        private readonly AppDbContext _db;

        public DashboardExpertController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Récupère le résumé pour un expert (cabinet)
        /// GET /api/dashboard/expert/summary?expertCompanyId=...
        /// Si expertCompanyId omis, la company de l'utilisateur courant est utilisée (doit être un cabinet).
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<ExpertDashboardDto>> GetExpertSummary([FromQuery] int? expertCompanyId = null)
        {
            // Déterminer l'ID du cabinet expert à utiliser
            int expertId;
            if (expertCompanyId.HasValue)
            {
                expertId = expertCompanyId.Value;

                var managingCompany = await _db.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == expertId && c.DeletedAt == null);

                if (managingCompany == null)
                    return NotFound(new { Message = "Cabinet expert introuvable" });

                if (!managingCompany.IsCabinetExpert)
                    return BadRequest(new { Message = "La société spécifiée n'est pas un cabinet expert" });
            }
            else
            {
                // Utiliser la company du user courant
                var userId = User.GetUserId();
                var currentUser = await _db.Users
                    .AsNoTracking()
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);

                if (currentUser == null || currentUser.Employee == null)
                    return BadRequest(new { Message = "Utilisateur courant non associé à une entreprise" });

                expertId = currentUser.Employee.CompanyId;

                var managerCompany = await _db.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == expertId && c.DeletedAt == null);

                if (managerCompany == null || !managerCompany.IsCabinetExpert)
                    return Forbid();
            }

            // Récupérer les sociétés gérées par ce cabinet (clients)
            var clientIds = await _db.Companies
                .AsNoTracking()
                .Where(c => c.DeletedAt == null && c.ManagedByCompanyId == expertId)
                .Select(c => c.Id)
                .ToListAsync();

            var totalClients = clientIds.Count;

            // Nombre total d'employés dans ces sociétés (non supprimés)
            var totalEmployees = 0;
            if (clientIds.Any())
            {
                totalEmployees = await _db.Employees
                    .AsNoTracking()
                    .Where(e => e.DeletedAt == null && e.CompanyId != null && clientIds.Contains(e.CompanyId))
                    .CountAsync();
            }
            Console.WriteLine($"Total Employees {totalEmployees}");

            var result = new ExpertDashboardDto
            {
                ExpertCompanyId = expertId,
                TotalClients = totalClients,
                TotalEmployees = totalEmployees,
                AsOf = DateTimeOffset.UtcNow
            };

            return Ok(result);
        }
    }
}