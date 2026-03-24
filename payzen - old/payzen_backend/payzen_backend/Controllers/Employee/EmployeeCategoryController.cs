using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Employee.Dtos;

namespace payzen_backend.Controllers.Employees
{
    [Route("api/employee-categories")]
    [ApiController]
    [Authorize]
    public class EmployeeCategoryController : ControllerBase
    {
        private readonly AppDbContext _db;
        private static readonly string[] ValidPayrollPeriodicities = { "Mensuelle", "Bimensuelle" };

        public EmployeeCategoryController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// R�cup�re toutes les cat�gories d'employ�s
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<EmployeeCategoryReadDto>>> GetAll([FromQuery] int? companyId = null)
        {
            var query = _db.EmployeeCategories
                .AsNoTracking()
                .Where(c => c.DeletedAt == null);

            // Filtrer par companyId si fourni
            if (companyId.HasValue)
            {
                query = query.Where(c => c.CompanyId == companyId.Value);
            }

            var categories = await query
                .Include(c => c.Company)
                .OrderBy(c => c.Company.CompanyName)
                .ThenBy(c => c.Name)
                .Select(c => new EmployeeCategoryReadDto
                {
                    Id = c.Id,
                    CompanyId = c.CompanyId,
                    CompanyName = c.Company.CompanyName,
                    Name = c.Name,
                    Mode = c.Mode,
                    PayrollPeriodicity = c.PayrollPeriodicity,
                    ModeDescription = c.Mode == EmployeeCategoryMode.Attendance ? "Pr�sence" : "Absence",
                    CreatedAt = c.CreatedAt.DateTime
                })
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// R�cup�re une cat�gorie d'employ� par ID
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeCategoryReadDto>> GetById(int id)
        {
            var category = await _db.EmployeeCategories
                .AsNoTracking()
                .Where(c => c.Id == id && c.DeletedAt == null)
                .Include(c => c.Company)
                .Select(c => new EmployeeCategoryReadDto
                {
                    Id = c.Id,
                    CompanyId = c.CompanyId,
                    CompanyName = c.Company.CompanyName,
                    Name = c.Name,
                    Mode = c.Mode,
                    PayrollPeriodicity = c.PayrollPeriodicity,
                    ModeDescription = c.Mode == EmployeeCategoryMode.Attendance ? "Pr�sence" : "Absence",
                    CreatedAt = c.CreatedAt.DateTime
                })
                .FirstOrDefaultAsync();

            if (category == null)
                return NotFound(new { Message = "Cat�gorie d'employ� non trouv�e" });

            return Ok(category);
        }

        /// <summary>
        /// R�cup�re les cat�gories d'une entreprise sp�cifique
        /// </summary>
        [HttpGet("company/{companyId}")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<EmployeeCategorySimpleDto>>> GetByCompany(int companyId)
        {
            // V�rifier que l'entreprise existe
            var companyExists = await _db.Companies
                .AnyAsync(c => c.Id == companyId && c.DeletedAt == null);

            if (!companyExists)
                return NotFound(new { Message = "Entreprise non trouv�e" });

            var categories = await _db.EmployeeCategories
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId && c.DeletedAt == null)
                .OrderBy(c => c.Name)
                .Select(c => new EmployeeCategorySimpleDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Mode = c.Mode,
                    PayrollPeriodicity = c.PayrollPeriodicity
                })
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// R�cup�re les cat�gories par mode (Attendance ou Absence)
        /// </summary>
        [HttpGet("by-mode/{mode}")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<EmployeeCategoryReadDto>>> GetByMode(EmployeeCategoryMode mode, [FromQuery] int? companyId = null)
        {
            var query = _db.EmployeeCategories
                .AsNoTracking()
                .Where(c => c.DeletedAt == null && c.Mode == mode);

            if (companyId.HasValue)
            {
                query = query.Where(c => c.CompanyId == companyId.Value);
            }

            var categories = await query
                .Include(c => c.Company)
                .OrderBy(c => c.Company.CompanyName)
                .ThenBy(c => c.Name)
                .Select(c => new EmployeeCategoryReadDto
                {
                    Id = c.Id,
                    CompanyId = c.CompanyId,
                    CompanyName = c.Company.CompanyName,
                    Name = c.Name,
                    Mode = c.Mode,
                    PayrollPeriodicity = c.PayrollPeriodicity,
                    ModeDescription = c.Mode == EmployeeCategoryMode.Attendance ? "Pr�sence" : "Absence",
                    CreatedAt = c.CreatedAt.DateTime
                })
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// Cr�e une nouvelle cat�gorie d'employ�
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeCategoryReadDto>> Create([FromBody] EmployeeCategoryCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.GetUserId();

            // V�rifier que l'entreprise existe
            var companyExists = await _db.Companies
                .AnyAsync(c => c.Id == dto.CompanyId && c.DeletedAt == null);

            if (!companyExists)
                return NotFound(new { Message = "Entreprise non trouv�e" });

            // V�rifier qu'une cat�gorie avec ce nom n'existe pas d�j� pour cette entreprise
            var duplicateName = await _db.EmployeeCategories
                .AnyAsync(c => c.CompanyId == dto.CompanyId &&
                              c.Name.ToLower() == dto.Name.Trim().ToLower() &&
                              c.DeletedAt == null);

            if (duplicateName)
                return Conflict(new { Message = "Une cat�gorie avec ce nom existe d�j� pour cette entreprise" });

            var periodicity = string.IsNullOrWhiteSpace(dto.PayrollPeriodicity)
                ? "Mensuelle"
                : dto.PayrollPeriodicity.Trim();

            if (!ValidPayrollPeriodicities.Contains(periodicity))
                return BadRequest(new { Message = "P�riodicit� invalide. Valeurs accept�es : Mensuelle, Bimensuelle" });

            var category = new EmployeeCategory
            {
                CompanyId = dto.CompanyId,
                Name = dto.Name.Trim(),
                Mode = dto.Mode,
                PayrollPeriodicity = periodicity,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUserId
            };

            _db.EmployeeCategories.Add(category);
            await _db.SaveChangesAsync();

            // R�cup�rer la cat�gorie cr��e avec les donn�es de navigation
            var createdCategory = await _db.EmployeeCategories
                .AsNoTracking()
                .Include(c => c.Company)
                .Where(c => c.Id == category.Id)
                .Select(c => new EmployeeCategoryReadDto
                {
                    Id = c.Id,
                    CompanyId = c.CompanyId,
                    CompanyName = c.Company.CompanyName,
                    Name = c.Name,
                    Mode = c.Mode,
                    PayrollPeriodicity = c.PayrollPeriodicity,
                    ModeDescription = c.Mode == EmployeeCategoryMode.Attendance ? "Pr�sence" : "Absence",
                    CreatedAt = c.CreatedAt.DateTime
                })
                .FirstAsync();

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, createdCategory);
        }

        /// <summary>
        /// Met � jour une cat�gorie d'employ�
        /// </summary>
        [HttpPatch("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeCategoryReadDto>> Update(int id, [FromBody] EmployeeCategoryUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.GetUserId();

            var category = await _db.EmployeeCategories
                .Include(c => c.Company)
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (category == null)
                return NotFound(new { Message = "Cat�gorie d'employ� non trouv�e" });

            // Mise � jour du nom si fourni
            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name.Trim() != category.Name)
            {
                // V�rifier l'unicit� du nouveau nom pour cette entreprise
                var nameTaken = await _db.EmployeeCategories
                    .AnyAsync(c => c.CompanyId == category.CompanyId &&
                                  c.Name.ToLower() == dto.Name.Trim().ToLower() &&
                                  c.Id != id &&
                                  c.DeletedAt == null);

                if (nameTaken)
                    return Conflict(new { Message = "Une cat�gorie avec ce nom existe d�j� pour cette entreprise" });

                category.Name = dto.Name.Trim();
            }

            // Mise � jour du mode si fourni
            if (dto.Mode.HasValue && dto.Mode.Value != category.Mode)
            {
                category.Mode = dto.Mode.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.PayrollPeriodicity))
            {
                var periodicity = dto.PayrollPeriodicity.Trim();

                if (!ValidPayrollPeriodicities.Contains(periodicity))
                    return BadRequest(new { Message = "P�riodicit� invalide. Valeurs accept�es : Mensuelle, Bimensuelle" });

                if (periodicity != category.PayrollPeriodicity)
                {
                    category.PayrollPeriodicity = periodicity;
                }
            }

            category.ModifiedAt = DateTimeOffset.UtcNow;
            category.ModifiedBy = currentUserId;

            await _db.SaveChangesAsync();

            var updatedCategory = new EmployeeCategoryReadDto
            {
                Id = category.Id,
                CompanyId = category.CompanyId,
                CompanyName = category.Company.CompanyName,
                Name = category.Name,
                Mode = category.Mode,
                PayrollPeriodicity = category.PayrollPeriodicity,
                ModeDescription = category.Mode == EmployeeCategoryMode.Attendance ? "Pr�sence" : "Absence",
                CreatedAt = category.CreatedAt.DateTime
            };

            return Ok(updatedCategory);
        }

        /// <summary>
        /// Supprime une cat�gorie d'employ� (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = User.GetUserId();

            var category = await _db.EmployeeCategories
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (category == null)
                return NotFound(new { Message = "Cat�gorie d'employ� non trouv�e" });

            // V�rifier si la cat�gorie est utilis�e par des employ�s
            var isUsed = await _db.Employees
                .AnyAsync(e => e.CategoryId == id && e.DeletedAt == null);

            if (isUsed)
            {
                return BadRequest(new { Message = "Impossible de supprimer cette cat�gorie car elle est utilis�e par des employ�s" });
            }

            category.DeletedAt = DateTimeOffset.UtcNow;
            category.DeletedBy = currentUserId;

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
