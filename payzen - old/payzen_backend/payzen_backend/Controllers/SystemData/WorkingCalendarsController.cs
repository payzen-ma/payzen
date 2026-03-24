using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Company;
using payzen_backend.Models.Company.Dtos;
using payzen_backend.Authorization;
using payzen_backend.Extensions;
using System.Globalization;

namespace payzen_backend.Controllers.SystemData
{
    [Route("api/working-calendar")]
    [ApiController]
    [Authorize]
    public class WorkingCalendarsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public WorkingCalendarsController(AppDbContext db) => _db = db;

        /// <summary>
        /// R�cup�re tous les calendriers de travail actifs
        /// </summary>
        [HttpGet]
        //[HasPermission("READ_WORKING_CALENDAR")]
        public async Task<ActionResult<IEnumerable<WorkingCalendarReadDto>>> GetAll()
        {
            var calendars = await _db.WorkingCalendars
                .AsNoTracking()
                .Where(wc => wc.DeletedAt == null)
                .Include(wc => wc.Company)
                .OrderBy(wc => wc.CompanyId)
                .ThenBy(wc => wc.DayOfWeek)
                .ToListAsync();

            var result = calendars.Select(wc => new WorkingCalendarReadDto
            {
                Id = wc.Id,
                CompanyId = wc.CompanyId,
                CompanyName = wc.Company?.CompanyName ?? "",
                DayOfWeek = wc.DayOfWeek,
                DayOfWeekName = GetDayOfWeekName(wc.DayOfWeek),
                IsWorkingDay = wc.IsWorkingDay,
                StartTime = wc.StartTime,
                EndTime = wc.EndTime,
                CreatedAt = wc.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re un calendrier de travail par ID
        /// </summary>
        [HttpGet("{id}")]
        //[HasPermission("VIEW_WORKING_CALENDAR")]
        public async Task<ActionResult<WorkingCalendarReadDto>> GetById(int id)
        {
            var calendar = await _db.WorkingCalendars
                .AsNoTracking()
                .Where(wc => wc.DeletedAt == null)
                .Include(wc => wc.Company)
                .FirstOrDefaultAsync(wc => wc.Id == id);

            if (calendar == null)
                return NotFound(new { Message = "Calendrier de travail non trouv�" });

            var result = new WorkingCalendarReadDto
            {
                Id = calendar.Id,
                CompanyId = calendar.CompanyId,
                CompanyName = calendar.Company?.CompanyName ?? "",
                DayOfWeek = calendar.DayOfWeek,
                DayOfWeekName = GetDayOfWeekName(calendar.DayOfWeek),
                IsWorkingDay = calendar.IsWorkingDay,
                StartTime = calendar.StartTime,
                EndTime = calendar.EndTime,
                CreatedAt = calendar.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re le calendrier de travail d'une soci�t�
        /// </summary>
        [HttpGet("company/{companyId}")]
        //[HasPermission("READ_WORKING_CALENDAR")]
        public async Task<ActionResult<IEnumerable<WorkingCalendarReadDto>>> GetByCompanyId(int companyId)
        {
            var companyExists = await _db.Companies.AnyAsync(c => c.Id == companyId && c.DeletedAt == null);
            if (!companyExists)
                return NotFound(new { Message = "Soci�t� non trouv�e" });

            var calendars = await _db.WorkingCalendars
                .AsNoTracking()
                .Where(wc => wc.CompanyId == companyId && wc.DeletedAt == null)
                .Include(wc => wc.Company)
                .OrderBy(wc => wc.DayOfWeek)
                .ToListAsync();

            var result = calendars.Select(wc => new WorkingCalendarReadDto
            {
                Id = wc.Id,
                CompanyId = wc.CompanyId,
                CompanyName = wc.Company?.CompanyName ?? "",
                DayOfWeek = wc.DayOfWeek,
                DayOfWeekName = GetDayOfWeekName(wc.DayOfWeek),
                IsWorkingDay = wc.IsWorkingDay,
                StartTime = wc.StartTime,
                EndTime = wc.EndTime,
                CreatedAt = wc.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// Cr�e un nouveau calendrier de travail
        /// </summary>
        [HttpPost]
        //[HasPermission("CREATE_WORKING_CALENDAR")]
        public async Task<ActionResult<WorkingCalendarReadDto>> Create([FromBody] WorkingCalendarCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new 
                { 
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            // V�rifier que la soci�t� existe
            var companyExists = await _db.Companies.AnyAsync(c => c.Id == dto.CompanyId && c.DeletedAt == null);
            if (!companyExists)
                return NotFound(new { Message = "Soci�t� non trouv�e" });

            // V�rifier qu'un calendrier pour ce jour n'existe pas d�j� pour cette soci�t�
            var calendarExists = await _db.WorkingCalendars
                .AnyAsync(wc => wc.CompanyId == dto.CompanyId 
                             && wc.DayOfWeek == dto.DayOfWeek 
                             && wc.DeletedAt == null);

            if (calendarExists)
                return Conflict(new { Message = "Un calendrier existe d�j� pour ce jour de la semaine dans cette soci�t�" });

            // Validation des horaires si c'est un jour travaill�
            if (dto.IsWorkingDay)
            {
                if (dto.StartTime == null || dto.EndTime == null)
                    return BadRequest(new { Message = "Les horaires de d�but et de fin sont requis pour un jour travaill�" });

                if (dto.StartTime >= dto.EndTime)
                    return BadRequest(new { Message = "L'heure de d�but doit �tre avant l'heure de fin" });
            }

            var calendar = new WorkingCalendar
            {
                CompanyId = dto.CompanyId,
                DayOfWeek = dto.DayOfWeek,
                IsWorkingDay = dto.IsWorkingDay,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                CreatedBy = User.GetUserId(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.WorkingCalendars.Add(calendar);
            await _db.SaveChangesAsync();

            var createdCalendar = await _db.WorkingCalendars
                .AsNoTracking()
                .Include(wc => wc.Company)
                .FirstAsync(wc => wc.Id == calendar.Id);

            var result = new WorkingCalendarReadDto
            {
                Id = createdCalendar.Id,
                CompanyId = createdCalendar.CompanyId,
                CompanyName = createdCalendar.Company?.CompanyName ?? "",
                DayOfWeek = createdCalendar.DayOfWeek,
                DayOfWeekName = GetDayOfWeekName(createdCalendar.DayOfWeek),
                IsWorkingDay = createdCalendar.IsWorkingDay,
                StartTime = createdCalendar.StartTime,
                EndTime = createdCalendar.EndTime,
                CreatedAt = createdCalendar.CreatedAt.DateTime
            };

            return CreatedAtAction(nameof(GetById), new { id = calendar.Id }, result);
        }

        /// <summary>
        /// Met � jour un calendrier de travail
        /// </summary>
        [HttpPut("{id}")]
        //[HasPermission("UPDATE_WORKING_CALENDAR")]
        public async Task<ActionResult<WorkingCalendarReadDto>> Update(int id, [FromBody] WorkingCalendarUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new 
                { 
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var calendar = await _db.WorkingCalendars.FirstOrDefaultAsync(wc => wc.Id == id && wc.DeletedAt == null);
            if (calendar == null)
                return NotFound(new { Message = "Calendrier de travail non trouv�" });

            if (dto.CompanyId.HasValue && dto.CompanyId.Value != calendar.CompanyId)
            {
                var companyExists = await _db.Companies.AnyAsync(c => c.Id == dto.CompanyId.Value && c.DeletedAt == null);
                if (!companyExists)
                    return NotFound(new { Message = "Soci�t� non trouv�e" });
                
                calendar.CompanyId = dto.CompanyId.Value;
            }

            if (dto.DayOfWeek.HasValue && dto.DayOfWeek.Value != calendar.DayOfWeek)
            {
                var currentCompanyId = dto.CompanyId ?? calendar.CompanyId;
                var dayExists = await _db.WorkingCalendars
                    .AnyAsync(wc => wc.CompanyId == currentCompanyId 
                                 && wc.DayOfWeek == dto.DayOfWeek.Value 
                                 && wc.Id != id 
                                 && wc.DeletedAt == null);

                if (dayExists)
                    return Conflict(new { Message = "Un calendrier existe d�j� pour ce jour de la semaine dans cette soci�t�" });
                
                calendar.DayOfWeek = dto.DayOfWeek.Value;
            }

            if (dto.IsWorkingDay.HasValue)
            {
                calendar.IsWorkingDay = dto.IsWorkingDay.Value;

                // Si c'est un jour travaill�, v�rifier les horaires
                if (dto.IsWorkingDay.Value)
                {
                    var startTime = dto.StartTime ?? calendar.StartTime;
                    var endTime = dto.EndTime ?? calendar.EndTime;

                    if (startTime == null || endTime == null)
                        return BadRequest(new { Message = "Les horaires de d�but et de fin sont requis pour un jour travaill�" });

                    if (startTime >= endTime)
                        return BadRequest(new { Message = "L'heure de d�but doit �tre avant l'heure de fin" });
                }
            }

            if (dto.StartTime.HasValue)
            {
                if (calendar.IsWorkingDay)
                {
                    var endTime = dto.EndTime ?? calendar.EndTime;
                    if (endTime != null && dto.StartTime >= endTime)
                        return BadRequest(new { Message = "L'heure de d�but doit �tre avant l'heure de fin" });
                }
                calendar.StartTime = dto.StartTime;
            }

            if (dto.EndTime.HasValue)
            {
                if (calendar.IsWorkingDay)
                {
                    var startTime = dto.StartTime ?? calendar.StartTime;
                    if (startTime != null && startTime >= dto.EndTime)
                        return BadRequest(new { Message = "L'heure de d�but doit �tre avant l'heure de fin" });
                }
                calendar.EndTime = dto.EndTime;
            }

            calendar.ModifiedAt = DateTimeOffset.UtcNow;
            calendar.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            var updatedCalendar = await _db.WorkingCalendars
                .AsNoTracking()
                .Include(wc => wc.Company)
                .FirstAsync(wc => wc.Id == id);

            var result = new WorkingCalendarReadDto
            {
                Id = updatedCalendar.Id,
                CompanyId = updatedCalendar.CompanyId,
                CompanyName = updatedCalendar.Company?.CompanyName ?? "",
                DayOfWeek = updatedCalendar.DayOfWeek,
                DayOfWeekName = GetDayOfWeekName(updatedCalendar.DayOfWeek),
                IsWorkingDay = updatedCalendar.IsWorkingDay,
                StartTime = updatedCalendar.StartTime,
                EndTime = updatedCalendar.EndTime,
                CreatedAt = updatedCalendar.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Supprime un calendrier de travail (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        //[HasPermission("DELETE_WORKING_CALENDAR")]
        public async Task<IActionResult> Delete(int id)
        {
            var calendar = await _db.WorkingCalendars.FirstOrDefaultAsync(wc => wc.Id == id && wc.DeletedAt == null);
            if (calendar == null)
                return NotFound(new { Message = "Calendrier de travail non trouv�" });

            calendar.DeletedAt = DateTimeOffset.UtcNow;
            calendar.DeletedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// M�thode helper pour obtenir le nom du jour de la semaine
        /// </summary>
        private static string GetDayOfWeekName(int dayOfWeek)
        {
            var culture = new CultureInfo("fr-FR");
            return culture.DateTimeFormat.GetDayName((DayOfWeek)dayOfWeek);
        }
    }
}
