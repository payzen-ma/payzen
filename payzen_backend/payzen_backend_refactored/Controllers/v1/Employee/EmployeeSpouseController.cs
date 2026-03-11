using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Employee.Dtos;
using payzen_backend.Models.Event;
using payzen_backend.Services;

namespace payzen_backend.Controllers.v1.Employees
{
    [Route("api/v{version:apiVersion}/employees/{employeeId}/spouse")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class EmployeeSpouseController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly EmployeeEventLogService _eventLogService;

        public EmployeeSpouseController(
            AppDbContext db, 
            EmployeeEventLogService EmployeeEventLog)
        {
            _db = db;
            _eventLogService = EmployeeEventLog;
        }

        /// <summary>
        /// R�cup�re le/la conjoint(e) d'un employ�
        /// GET /api/employees/{employeeId}/spouse
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeSpouseReadDto>> GetSpouse(int employeeId)
        {
            var employeeExists = await _db.Employees
                .AnyAsync(e => e.Id == employeeId && e.DeletedAt == null);

            if (!employeeExists)
                return NotFound(new { Message = "Employ� non trouv�" });

            var spouse = await _db.EmployeeSpouses
                .AsNoTracking()
                .Include(s => s.Employee)
                .Include(s => s.Gender)
                .Where(s => s.EmployeeId == employeeId && s.DeletedAt == null)
                .Select(s => new EmployeeSpouseReadDto
                {
                    Id = s.Id,
                    EmployeeId = s.EmployeeId,
                    EmployeeFirstName = s.Employee.FirstName,
                    EmployeeLastName = s.Employee.LastName,
                    EmployeeFullName = $"{s.Employee.FirstName} {s.Employee.LastName}",
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    FullName = $"{s.FirstName} {s.LastName}",
                    DateOfBirth = s.DateOfBirth,
                    Age = DateTime.Now.Year - s.DateOfBirth.Year,
                    GenderId = s.GenderId,
                    GenderName = s.Gender != null ? s.Gender.NameFr : null,
                    CinNumber = s.CinNumber,
                    IsDependent = s.IsDependent,
                    CreatedAt = s.CreatedAt.DateTime,
                    ModifiedAt = s.ModifiedAt.HasValue ? s.ModifiedAt.Value.DateTime : null
                })
                .FirstOrDefaultAsync();

            if (spouse == null)
                return NotFound(new { Message = "Aucun(e) conjoint(e) trouv�(e)" });

            return Ok(spouse);
        }

        /// <summary>
        /// Cr�e ou met � jour le/la conjoint(e) d'un employ�
        /// POST /api/employees/{employeeId}/spouse
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeSpouseReadDto>> CreateOrUpdateSpouse(int employeeId, [FromBody] EmployeeSpouseCreateDto dto)
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

            // V�rifier si un CIN existe d�j� (sauf pour le m�me employ�)
            if (!string.IsNullOrWhiteSpace(dto.CinNumber))
            {
                var cinExists = await _db.EmployeeSpouses
                    .AnyAsync(s => s.CinNumber == dto.CinNumber.Trim() && 
                                  s.EmployeeId != employeeId && 
                                  s.DeletedAt == null);

                if (cinExists)
                    return Conflict(new { Message = "Ce num�ro CIN est d�j� utilis�" });
            }

            var userId = User.GetUserId();

            // V�rifier si un conjoint existe d�j�
            var existingSpouse = await _db.EmployeeSpouses
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.DeletedAt == null);

            if (existingSpouse != null)
            {
                // Mettre � jour le conjoint existant
                existingSpouse.FirstName = dto.FirstName.Trim();
                existingSpouse.LastName = dto.LastName.Trim();
                existingSpouse.DateOfBirth = dto.DateOfBirth;
                existingSpouse.GenderId = dto.GenderId;
                existingSpouse.CinNumber = dto.CinNumber?.Trim();
                existingSpouse.IsDependent = dto.IsDependent;
                existingSpouse.ModifiedAt = DateTimeOffset.UtcNow;
                existingSpouse.ModifiedBy = userId;

                await _db.SaveChangesAsync();

                var updatedResult = await GetSpouseDto(existingSpouse.Id);
                return Ok(updatedResult);
            }
            else
            {
                // Cr�er un nouveau conjoint
                var spouse = new EmployeeSpouse
                {
                    EmployeeId = dto.EmployeeId,
                    FirstName = dto.FirstName.Trim(),
                    LastName = dto.LastName.Trim(),
                    DateOfBirth = dto.DateOfBirth,
                    GenderId = dto.GenderId,
                    CinNumber = dto.CinNumber?.Trim(),
                    IsDependent = dto.IsDependent,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = userId
                };

                _db.EmployeeSpouses.Add(spouse);
                await _db.SaveChangesAsync();

                // Logging the creation du conjoint
                await _eventLogService.LogRelationEventAsync(
                    employeeId,
                    EmployeeEventLogService.EventNames.SpouseAdded,
                    null,
                    string.Empty,
                    spouse.Id,
                    spouse.FirstName + " " + spouse.LastName,
                    User.GetUserId()
                    );


                var result = await GetSpouseDto(spouse.Id);
                return CreatedAtAction(nameof(GetSpouse), new { employeeId = employeeId }, result);
            }
        }

        /// <summary>
        /// Met � jour le/la conjoint(e)
        /// PUT /api/employees/{employeeId}/spouse
        /// </summary>
        [HttpPut]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeSpouseReadDto>> UpdateSpouse(int employeeId, [FromBody] EmployeeSpouseUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var spouse = await _db.EmployeeSpouses
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.DeletedAt == null);

            if (spouse == null)
                return NotFound(new { Message = "Aucun(e) conjoint(e) trouv�(e)" });

            // Valider le genre si fourni
            if (dto.GenderId.HasValue)
            {
                var genderExists = await _db.Genders
                    .AnyAsync(g => g.Id == dto.GenderId.Value && g.IsActive);

                if (!genderExists)
                    return BadRequest(new { Message = "Genre invalide" });
            }

            // V�rifier si un CIN existe d�j� (sauf pour ce conjoint)
            if (!string.IsNullOrWhiteSpace(dto.CinNumber))
            {
                var cinExists = await _db.EmployeeSpouses
                    .AnyAsync(s => s.CinNumber == dto.CinNumber.Trim() && 
                                  s.Id != spouse.Id && 
                                  s.DeletedAt == null);

                if (cinExists)
                    return Conflict(new { Message = "Ce num�ro CIN est d�j� utilis�" });
            }

            var userId = User.GetUserId();

            spouse.FirstName = dto.FirstName.Trim();
            spouse.LastName = dto.LastName.Trim();
            spouse.DateOfBirth = dto.DateOfBirth;
            spouse.GenderId = dto.GenderId;
            spouse.CinNumber = dto.CinNumber?.Trim();
            spouse.IsDependent = dto.IsDependent;
            spouse.ModifiedAt = DateTimeOffset.UtcNow;
            spouse.ModifiedBy = userId;

            await _db.SaveChangesAsync();

            // Log the update of the spouse
            await _eventLogService.LogRelationEventAsync(
                employeeId,
                EmployeeEventLogService.EventNames.SpouseUpdated,
                null,
                string.Empty,
                spouse.Id,
                spouse.FirstName + " " + spouse.LastName,
                User.GetUserId()
                );

            var result = await GetSpouseDto(spouse.Id);
            return Ok(result);
        }

        /// <summary>
        /// Supprime le/la conjoint(e) (soft delete)
        /// DELETE /api/employees/{employeeId}/spouse
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteSpouse(int employeeId)
        {
            var spouse = await _db.EmployeeSpouses
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.DeletedAt == null);

            if (spouse == null)
                return NotFound(new { Message = "Aucun(e) conjoint(e) trouv�(e)" });

            var userId = User.GetUserId();

            spouse.DeletedAt = DateTimeOffset.UtcNow;
            spouse.DeletedBy = userId;

            await _db.SaveChangesAsync();

            // Log the deletion of the spouse
            await _eventLogService.LogRelationEventAsync(
                employeeId,
                EmployeeEventLogService.EventNames.SpouseDeleted,
                null,
                string.Empty,
                spouse.Id,
                spouse.FirstName + " " + spouse.LastName,
                User.GetUserId()
                );

            return NoContent();
        }

        // M�thode helper pour r�cup�rer le DTO
        private async Task<EmployeeSpouseReadDto> GetSpouseDto(int spouseId)
        {
            return await _db.EmployeeSpouses
                .AsNoTracking()
                .Include(s => s.Employee)
                .Include(s => s.Gender)
                .Where(s => s.Id == spouseId)
                .Select(s => new EmployeeSpouseReadDto
                {
                    Id = s.Id,
                    EmployeeId = s.EmployeeId,
                    EmployeeFirstName = s.Employee.FirstName,
                    EmployeeLastName = s.Employee.LastName,
                    EmployeeFullName = $"{s.Employee.FirstName} {s.Employee.LastName}",
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    FullName = $"{s.FirstName} {s.LastName}",
                    DateOfBirth = s.DateOfBirth,
                    Age = DateTime.Now.Year - s.DateOfBirth.Year,
                    GenderId = s.GenderId,
                    GenderName = s.Gender != null ? s.Gender.NameFr : null,
                    CinNumber = s.CinNumber,
                    IsDependent = s.IsDependent,
                    CreatedAt = s.CreatedAt.DateTime,
                    ModifiedAt = s.ModifiedAt.HasValue ? s.ModifiedAt.Value.DateTime : null
                })
                .FirstAsync();
        }
    }
}