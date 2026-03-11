using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Employee.Dtos;
using payzen_backend.Services;

namespace payzen_backend.Controllers.v1.Employees
{
    [Route("api/v{version:apiVersion}/employees/{employeeId}/children")]
    [ApiVersion("1.0")]
    [ApiController]
    [Authorize]
    public class EmployeeChildController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly EmployeeEventLogService _EmployeeEventLog;

        public EmployeeChildController(AppDbContext db, EmployeeEventLogService EmployeeEventLog)
        {
            _db = db;
            _EmployeeEventLog = EmployeeEventLog;
        }

        /// <summary>
        /// R�cup�re la liste des enfants d'un employ�
        /// GET /api/employees/{employeeId}/children
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<EmployeeChildReadDto>>> GetChildren(int employeeId)
        {
            var employeeExists = await _db.Employees
                .AnyAsync(e => e.Id == employeeId && e.DeletedAt == null);

            if (!employeeExists)
                return NotFound(new { Message = "Employ� non trouv�" });

            var children = await _db.EmployeeChildren
                .AsNoTracking()
                .Include(c => c.Employee)
                .Include(c => c.Gender)
                .Where(c => c.EmployeeId == employeeId && c.DeletedAt == null)
                .OrderByDescending(c => c.DateOfBirth)
                .Select(c => new EmployeeChildReadDto
                {
                    Id = c.Id,
                    EmployeeId = c.EmployeeId,
                    EmployeeFirstName = c.Employee.FirstName,
                    EmployeeLastName = c.Employee.LastName,
                    EmployeeFullName = $"{c.Employee.FirstName} {c.Employee.LastName}",
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    FullName = $"{c.FirstName} {c.LastName}",
                    DateOfBirth = c.DateOfBirth,
                    Age = DateTime.Now.Year - c.DateOfBirth.Year,
                    GenderId = c.GenderId,
                    GenderName = c.Gender != null ? c.Gender.NameFr : null,
                    IsDependent = c.IsDependent,
                    IsStudent = c.IsStudent,
                    CreatedAt = c.CreatedAt.DateTime,
                    ModifiedAt = c.ModifiedAt.HasValue ? c.ModifiedAt.Value.DateTime : null
                })
                .ToListAsync();

            return Ok(children);
        }

        /// <summary>
        /// R�cup�re un enfant par son ID
        /// GET /api/employees/{employeeId}/children/{id}
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeChildReadDto>> GetChildById(int employeeId, int id)
        {
            var child = await _db.EmployeeChildren
                .AsNoTracking()
                .Include(c => c.Employee)
                .Include(c => c.Gender)
                .Where(c => c.Id == id && c.EmployeeId == employeeId && c.DeletedAt == null)
                .Select(c => new EmployeeChildReadDto
                {
                    Id = c.Id,
                    EmployeeId = c.EmployeeId,
                    EmployeeFirstName = c.Employee.FirstName,
                    EmployeeLastName = c.Employee.LastName,
                    EmployeeFullName = $"{c.Employee.FirstName} {c.Employee.LastName}",
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    FullName = $"{c.FirstName} {c.LastName}",
                    DateOfBirth = c.DateOfBirth,
                    Age = DateTime.Now.Year - c.DateOfBirth.Year,
                    GenderId = c.GenderId,
                    GenderName = c.Gender != null ? c.Gender.NameFr : null,
                    IsDependent = c.IsDependent,
                    IsStudent = c.IsStudent,
                    CreatedAt = c.CreatedAt.DateTime,
                    ModifiedAt = c.ModifiedAt.HasValue ? c.ModifiedAt.Value.DateTime : null
                })
                .FirstOrDefaultAsync();

            if (child == null)
                return NotFound(new { Message = "Enfant non trouv�" });

            return Ok(child);
        }

        /// <summary>
        /// Cr�e un nouvel enfant pour un employ�
        /// POST /api/employees/{employeeId}/children
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeChildReadDto>> CreateChild(int employeeId, [FromBody] EmployeeChildCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.EmployeeId != employeeId)
                return BadRequest(new { Message = "L'ID de l'employ� ne correspond pas" });

            var employee = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId && e.DeletedAt == null);

            if (employee == null)
                return NotFound(new { Message = "Employ� non trouv�" });

            // Valider le genre si fourni
            if (dto.GenderId.HasValue)
            {
                var genderExists = await _db.Genders
                    .AnyAsync(g => g.Id == dto.GenderId.Value && g.IsActive);

                if (!genderExists)
                    return BadRequest(new { Message = "Genre invalide" });
            }

            var userId = User.GetUserId();

            var child = new EmployeeChild
            {
                EmployeeId = dto.EmployeeId,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                DateOfBirth = dto.DateOfBirth,
                GenderId = dto.GenderId,
                IsDependent = dto.IsDependent,
                IsStudent = dto.IsStudent,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            
            _db.EmployeeChildren.Add(child);
            await _db.SaveChangesAsync();

            // Log Creation of child apres sauvegarde pour avoir l'ID
            await _EmployeeEventLog.LogRelationEventAsync(
                employeeId, 
                EmployeeEventLogService.EventNames.ChildAdded, 
                null, 
                string.Empty,
                child.Id, 
                child.FirstName + " " + child.LastName, 
                User.GetUserId()
             );

            var result = await _db.EmployeeChildren
                .AsNoTracking()
                .Include(c => c.Employee)
                .Include(c => c.Gender)
                .Where(c => c.Id == child.Id)
                .Select(c => new EmployeeChildReadDto
                {
                    Id = c.Id,
                    EmployeeId = c.EmployeeId,
                    EmployeeFirstName = c.Employee.FirstName,
                    EmployeeLastName = c.Employee.LastName,
                    EmployeeFullName = $"{c.Employee.FirstName} {c.Employee.LastName}",
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    FullName = $"{c.FirstName} {c.LastName}",
                    DateOfBirth = c.DateOfBirth,
                    Age = DateTime.Now.Year - c.DateOfBirth.Year,
                    GenderId = c.GenderId,
                    GenderName = c.Gender != null ? c.Gender.NameFr : null,
                    IsDependent = c.IsDependent,
                    IsStudent = c.IsStudent,
                    CreatedAt = c.CreatedAt.DateTime,
                    ModifiedAt = c.ModifiedAt.HasValue ? c.ModifiedAt.Value.DateTime : null
                })
                .FirstAsync();

            return CreatedAtAction(nameof(GetChildById), new { employeeId = employeeId, id = child.Id }, result);
        }

        /// <summary>
        /// Met � jour un enfant
        /// PUT /api/employees/{employeeId}/children/{id}
        /// </summary>
        [HttpPut("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeChildReadDto>> UpdateChild(int employeeId, int id, [FromBody] EmployeeChildUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var child = await _db.EmployeeChildren
                .FirstOrDefaultAsync(c => c.Id == id && c.EmployeeId == employeeId && c.DeletedAt == null);

            if (child == null)
                return NotFound(new { Message = "Enfant non trouv�" });

            // Valider le genre si fourni
            if (dto.GenderId.HasValue)
            {
                var genderExists = await _db.Genders
                    .AnyAsync(g => g.Id == dto.GenderId.Value && g.IsActive);

                if (!genderExists)
                    return BadRequest(new { Message = "Genre invalide" });
            }

            var userId = User.GetUserId();

            child.FirstName = dto.FirstName.Trim();
            child.LastName = dto.LastName.Trim();
            child.DateOfBirth = dto.DateOfBirth;
            child.GenderId = dto.GenderId;
            child.IsDependent = dto.IsDependent;
            child.IsStudent = dto.IsStudent;
            child.ModifiedAt = DateTimeOffset.UtcNow;
            child.ModifiedBy = userId;

            await _db.SaveChangesAsync();

            // Log mise a jour d'un enfants

            await _EmployeeEventLog.LogRelationEventAsync(
                employeeId, 
                EmployeeEventLogService.EventNames.ChildUpdated, 
                id, 
                dto.FirstName + " " + dto.FirstName, 
                child.Id, 
                child.FirstName + " " + child.LastName, 
                User.GetUserId()
             );

            var result = await _db.EmployeeChildren
                .AsNoTracking()
                .Include(c => c.Employee)
                .Include(c => c.Gender)
                .Where(c => c.Id == id)
                .Select(c => new EmployeeChildReadDto
                {
                    Id = c.Id,
                    EmployeeId = c.EmployeeId,
                    EmployeeFirstName = c.Employee.FirstName,
                    EmployeeLastName = c.Employee.LastName,
                    EmployeeFullName = $"{c.Employee.FirstName} {c.Employee.LastName}",
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    FullName = $"{c.FirstName} {c.LastName}",
                    DateOfBirth = c.DateOfBirth,
                    Age = DateTime.Now.Year - c.DateOfBirth.Year,
                    GenderId = c.GenderId,
                    GenderName = c.Gender != null ? c.Gender.NameFr : null,
                    IsDependent = c.IsDependent,
                    IsStudent = c.IsStudent,
                    CreatedAt = c.CreatedAt.DateTime,
                    ModifiedAt = c.ModifiedAt.HasValue ? c.ModifiedAt.Value.DateTime : null
                })
                .FirstAsync();

            return Ok(result);
        }

        /// <summary>
        /// Supprime un enfant (soft delete)
        /// DELETE /api/employees/{employeeId}/children/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChild(int employeeId, int id)
        {
            var child = await _db.EmployeeChildren
                .FirstOrDefaultAsync(c => c.Id == id && c.EmployeeId == employeeId && c.DeletedAt == null);

            if (child == null)
                return NotFound(new { Message = "Enfant non trouv�" });

            var userId = User.GetUserId();

            child.DeletedAt = DateTimeOffset.UtcNow;
            child.DeletedBy = userId;

            // Log suppression d'un enfant
            await _EmployeeEventLog.LogRelationEventAsync(
                employeeId, 
                EmployeeEventLogService.EventNames.ChildDeleted, 
                id, 
                child.FirstName + " " + child.LastName, 
                child.Id, 
                child.FirstName + " " + child.LastName, 
                User.GetUserId()
             );

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}