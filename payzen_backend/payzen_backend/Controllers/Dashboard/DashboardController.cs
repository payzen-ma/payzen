using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Authorization;
using payzen_backend.Data;
using payzen_backend.Models.Dashboard.Dtos;

namespace payzen_backend.Controllers.Dashboard
{
    [ApiController]
    [Route("api/dashboard/employees")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _db;

        public DashboardController(AppDbContext db) { _db = db; }


        /// <summary>
        /// R�cup�re les statistiques du dashboard avec la liste des employ�s
        /// </summary>
        /// <returns>Statistiques globales et liste d�taill�e des employ�s</returns>
        [HttpGet]
        //[HasPermission("VIEW_DASHBOARD")]
        [Produces("application/json")]
        public async Task<ActionResult<DashboardResponseDto>> GetDashboard()
        {
            // R�cup�rer le userId depuis les claims du token JWT
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");

            if (userIdClaim == null)
            {
                return Unauthorized(new { Message = "Utilisateur non authentifi�" });
            }

            if (!int.TryParse(userIdClaim.Value, out var userId))
            {
                return BadRequest(new { Message = "ID utilisateur invalide" });
            }

            // R�cup�rer l'utilisateur avec son employ� associ� pour obtenir la CompanyId
            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && u.DeletedAt == null);

            if (user == null)
            {
                return NotFound(new { Message = "Utilisateur non trouv�" });
            }

            if (user.Employee == null)
            {
                return BadRequest(new { Message = "L'utilisateur n'est pas associ� � un employ�" });
            }

            var companyId = user.Employee.CompanyId;

            // Comptage total des employ�s non supprim�s de la m�me entreprise
            var totalEmployees = await _db.Employees
                .Where(e => e.DeletedAt == null && e.CompanyId == companyId)
                .CountAsync();

            // Comptage des employ�s actifs de la m�me entreprise
            var activeEmployees = await _db.Employees
                .Where(e => e.DeletedAt == null && e.CompanyId == companyId && e.Status != null && e.Status.Code.ToLower() == "active")
                .CountAsync();

            // R�cup�ration des d�partements de la m�me entreprise
            var departements = await _db.Departement
                .Where(d => d.DeletedAt == null && d.CompanyId == companyId)
                .Select(d => d.DepartementName)
                .ToListAsync();

            // R�cup�ration des Status (codes)
            var statuses = await _db.Statuses
                .Select(s => s.Code)
                .ToListAsync();

            // R�cup�ration des employ�s avec toutes les relations n�cessaires (filtr�s par CompanyId)
            var employees = await _db.Employees
                .AsNoTracking()
                .AsSplitQuery()
                .Where(e => e.DeletedAt == null && e.CompanyId == companyId)
                .Include(e => e.Company)
                .Include(e => e.Departement)
                .Include(e => e.Status)
                .Include(e => e.Manager)
                .Include(e => e.Documents)
                .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                    .ThenInclude(c => c.JobPosition)
                .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                    .ThenInclude(c => c.ContractType)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .Select(e => new EmployeeDashboardItemDto
                {
                    Id = e.Id.ToString(),
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Position = e.Contracts != null
                        ? e.Contracts
                            .Where(c => c.DeletedAt == null && c.EndDate == null)
                            .OrderByDescending(c => c.StartDate)
                            .Select(c => c.JobPosition!.Name)
                            .FirstOrDefault() ?? "Non assign�"
                        : "Non assign�",
                    Department = e.Departement != null ? e.Departement.DepartementName : "Non assign�",
                    statuses = e.Status != null ? e.Status.Code : string.Empty,
                    NameFr = e.Status != null ? e.Status.NameFr : string.Empty,
                    NameAr = e.Status != null ? e.Status.NameAr : string.Empty,
                    NameEn = e.Status != null ? e.Status.NameEn : string.Empty,
                    StartDate = e.Contracts != null
                        ? e.Contracts
                            .Where(c => c.DeletedAt == null && c.EndDate == null)
                            .OrderByDescending(c => c.StartDate)
                            .Select(c => c.StartDate.ToString("yyyy-MM-dd"))
                            .FirstOrDefault() ?? ""
                        : "",
                    MissingDocuments = e.Documents != null
                        ? e.Documents.Count(d => d.DeletedAt == null && string.IsNullOrEmpty(d.FilePath))
                        : 0,
                    ContractType = e.Contracts != null
                        ? e.Contracts
                            .Where(c => c.DeletedAt == null && c.EndDate == null)
                            .OrderByDescending(c => c.StartDate)
                            .Select(c => c.ContractType!.ContractTypeName)
                            .FirstOrDefault() ?? ""
                        : "",
                    Manager = e.Manager != null
                        ? $"{e.Manager.FirstName} {e.Manager.LastName}"
                        : null
                })
                .ToListAsync();

            var response = new DashboardResponseDto
            {
                TotalEmployees = totalEmployees,
                ActiveEmployees = activeEmployees,
                Employees = employees,
                Departements = departements,
                statuses = statuses
            };

            return Ok(response);
        }
    }
}