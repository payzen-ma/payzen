using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Employee.Dtos;
using payzen_backend.Extensions;

namespace payzen_backend.Controllers.Employees
{
    [Route("api/employee-attendance")]
    [ApiController]
    [Authorize]
    public class EmployeeAttendanceController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EmployeeAttendanceController(AppDbContext db)
        {
            _db = db;
        }

        #region CRUD endpoints
        
        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeAttendanceReadDto>> GetById(int id)
        {
            var attendance = await _db.EmployeeAttendances
                .AsNoTracking()
                .Include(a => a.Employee)
                .Include(a => a.Breaks) // Inclure les pauses
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null)
                return NotFound($"Assiduité avec l'ID {id} non trouvée.");

            return Ok(ToDto(attendance));
        }

        /// <summary>
        /// Récupère les assiduités d'un employé avec ses pauses
        /// </summary>
        [HttpGet("employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<EmployeeAttendanceReadDto>>> GetByEmployeeId(
            int employeeId,
            [FromQuery] DateOnly? startDate = null,
            [FromQuery] DateOnly? endDate = null,
            [FromQuery] bool includeBreaks = true)
        {
            Console.WriteLine($"=========================APPEL Employee Attendance for Employee {employeeId}");
            Console.WriteLine($"Parameters: startDate={startDate}, endDate={endDate}, includeBreaks={includeBreaks}");

            // Vérifier que l'employé existe
            var employeeExists = await _db.Employees
                .AnyAsync(e => e.Id == employeeId && e.DeletedAt == null);

            var att = await _db.EmployeeAttendances
                .AnyAsync(a => a.EmployeeId == employeeId);

            Console.WriteLine($"Attendance With await function {att}");

            Console.WriteLine($"Employee {employeeId} exists: {employeeExists}");

            if (!employeeExists)
                return NotFound($"Employé avec l'ID {employeeId} non trouvé.");

            var query = _db.EmployeeAttendances.AsNoTracking()
                .Where(a => a.EmployeeId == employeeId);
            Console.WriteLine($"Employee Attendances From DB : {query}");
            //if (startDate.HasValue)
            //    query = query.Where(a => a.WorkDate >= startDate.Value);

            //if (endDate.HasValue)
            //    query = query.Where(a => a.WorkDate <= endDate.Value);

            // Inclure les pauses si demandé
            //if (includeBreaks)
            //    query = query.Include(a => a.Breaks);

            var attendances = await query
                .ToListAsync();

            Console.WriteLine($"Found {attendances.Count} attendances");

            var result = attendances.Select(a => ToDto(a, includeBreaks)).ToList();
            Console.WriteLine($"Converted to {result.Count} DTOs");

            return Ok(result);
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeAttendanceReadDto>>> GetAttendances(
            [FromQuery] DateOnly? startDate = null,
            [FromQuery] DateOnly? endDate = null,
            [FromQuery] int? employeeId = null,
            [FromQuery] AttendanceStatus? status = null,
            [FromQuery] bool includeBreaks = false)
        {
            var query = _db.EmployeeAttendances.AsNoTracking();

            if (startDate.HasValue) query = query.Where(a => a.WorkDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(a => a.WorkDate <= endDate.Value);
            if (employeeId.HasValue) query = query.Where(a => a.EmployeeId == employeeId.Value);
            if (status.HasValue) query = query.Where(a => a.Status == status.Value);

            if (includeBreaks)
                query = query.Include(a => a.Breaks);

            var attendances = await query
                .Include(a => a.Employee)
                .OrderByDescending(a => a.WorkDate)
                .ThenBy(a => a.Employee!.LastName)
                .ToListAsync();

            return Ok(attendances.Select(a => ToDto(a, includeBreaks)));
        }

        [HttpPost]
        public async Task<ActionResult<EmployeeAttendanceReadDto>> CreateAttendance([FromBody] EmployeeAttendanceCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == dto.EmployeeId && e.DeletedAt == null);
            if (employee == null) return BadRequest($"Employé avec l'ID {dto.EmployeeId} non trouvé.");

            var existingAttendance = await _db.EmployeeAttendances
                .FirstOrDefaultAsync(a => a.EmployeeId == dto.EmployeeId && a.WorkDate == dto.WorkDate);

            if (existingAttendance != null)
                return BadRequest($"Une assiduité existe déjà pour l'employé {dto.EmployeeId} à la date {dto.WorkDate}.");

            var currentUserId = User.GetUserId();
            var workedHours = CalculateWorkedHours(dto.CheckIn, dto.CheckOut);

            var attendance = new EmployeeAttendance
            {
                EmployeeId = dto.EmployeeId,
                WorkDate = dto.WorkDate,
                CheckIn = dto.CheckIn,
                CheckOut = dto.CheckOut,
                Status = DetermineStatus(dto.CheckIn, dto.CheckOut),
                Source = dto.Source,
                WorkedHours = workedHours,
                BreakMinutesApplied = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUserId
            };

            _db.EmployeeAttendances.Add(attendance);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = attendance.Id }, ToDto(attendance));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<EmployeeAttendanceReadDto>> UpdateAttendance(int id, [FromBody] EmployeeAttendanceCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var attendance = await _db.EmployeeAttendances.FirstOrDefaultAsync(a => a.Id == id);
            if (attendance == null) return NotFound($"Assiduité avec l'ID {id} non trouvée.");

            var currentUserId = User.GetUserId();
            var workedHours = CalculateWorkedHours(dto.CheckIn, dto.CheckOut);

            attendance.CheckIn = dto.CheckIn;
            attendance.CheckOut = dto.CheckOut;
            attendance.Status = DetermineStatus(dto.CheckIn, dto.CheckOut);
            attendance.Source = dto.Source;
            attendance.WorkedHours = workedHours;
            attendance.ModifiedAt = DateTimeOffset.UtcNow;
            attendance.ModifiedBy = currentUserId;

            await _db.SaveChangesAsync();
            return Ok(ToDto(attendance));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            var attendance = await _db.EmployeeAttendances.FirstOrDefaultAsync(a => a.Id == id);
            if (attendance == null) return NotFound($"Assiduité avec l'ID {id} non trouvée.");

            _db.EmployeeAttendances.Remove(attendance);
            await _db.SaveChangesAsync();

            return NoContent();
        }

#endregion

        #region Check-In / Check-Out

        [HttpPost("check-in")]
        public async Task<ActionResult<EmployeeAttendanceReadDto>> CheckIn([FromBody] EmployeeAttendanceCheckDto dto)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var now = TimeOnly.FromDateTime(DateTime.Now);

            var attendance = await _db.EmployeeAttendances
                .FirstOrDefaultAsync(a => a.EmployeeId == dto.EmployeeId && a.WorkDate == today);

            var currentUserId = User.GetUserId();

            if (attendance != null && attendance.CheckIn.HasValue)
                return BadRequest("L'employé a déjà pointé son entrée aujourd'hui.");

            if (attendance == null)
            {
                attendance = new EmployeeAttendance
                {
                    EmployeeId = dto.EmployeeId,
                    WorkDate = today,
                    CheckIn = now,
                    Status = AttendanceStatus.Present,
                    Source = AttendanceSource.System,
                    WorkedHours = 0,
                    BreakMinutesApplied = 0,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = currentUserId
                };
                _db.EmployeeAttendances.Add(attendance);
            }
            else
            {
                attendance.CheckIn = now;
                attendance.Status = AttendanceStatus.Present;
                attendance.ModifiedAt = DateTimeOffset.UtcNow;
                attendance.ModifiedBy = currentUserId;
            }

            await _db.SaveChangesAsync();
            return Ok(ToDto(attendance));
        }

        [HttpPost("check-out")]
        public async Task<ActionResult<EmployeeAttendanceReadDto>> CheckOut([FromBody] EmployeeAttendanceCheckDto dto)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var now = TimeOnly.FromDateTime(DateTime.Now);

            var attendance = await _db.EmployeeAttendances
                .FirstOrDefaultAsync(a => a.EmployeeId == dto.EmployeeId && a.WorkDate == today);

            if (attendance == null || !attendance.CheckIn.HasValue)
                return BadRequest("Aucune entrée trouvée pour aujourd'hui. L'employé doit d'abord pointer son entrée.");

            if (attendance.CheckOut.HasValue)
                return BadRequest("L'employé a déjà pointé sa sortie aujourd'hui.");

            var currentUserId = User.GetUserId();
            attendance.CheckOut = now;
            attendance.WorkedHours = CalculateWorkedHours(attendance.CheckIn, attendance.CheckOut);
            attendance.ModifiedAt = DateTimeOffset.UtcNow;
            attendance.ModifiedBy = currentUserId;

            await _db.SaveChangesAsync();
            return Ok(ToDto(attendance));
        }

        #endregion

        #region Utilities

        private static decimal CalculateWorkedHours(TimeOnly? checkIn, TimeOnly? checkOut)
        {
            if (!checkIn.HasValue || !checkOut.HasValue) return 0;
            return (decimal)(checkOut.Value - checkIn.Value).TotalHours;
        }

        private static AttendanceStatus DetermineStatus(TimeOnly? checkIn, TimeOnly? checkOut)
        {
            if (checkIn.HasValue) return AttendanceStatus.Present;
            return AttendanceStatus.Absent;
        }

        private static EmployeeAttendanceReadDto ToDto(EmployeeAttendance a, bool includeBreaks = false)
        {
            Console.WriteLine("=========== Function TODto est appelé ==========");
            var dto = new EmployeeAttendanceReadDto
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                WorkDate = a.WorkDate,
                CheckIn = a.CheckIn,
                CheckOut = a.CheckOut,
                WorkedHours = a.WorkedHours,
                Status = a.Status,
                Source = a.Source,
                BreakMinutesApplied = a.BreakMinutesApplied
            };

            // Ajouter les pauses si demandé et disponibles
            if (includeBreaks && a.Breaks != null && a.Breaks.Any())
            {
                dto.Breaks = a.Breaks
                    .OrderBy(b => b.BreakStart)
                    .Select(b => new EmployeeAttendanceBreakReadDto
                    {
                        Id = b.Id,
                        BreakStart = b.BreakStart,
                        BreakEnd = b.BreakEnd,
                        BreakType = b.BreakType ?? string.Empty,
                        CreatedAt = b.CreatedAt,
                        ModifiedAt = b.ModifiedAt
                    })
                    .ToList();
            }
            else
            {
                dto.Breaks = new List<EmployeeAttendanceBreakReadDto>();
            }
            Console.WriteLine($"Break : {dto.WorkedHours}");

            return dto;
        }

        #endregion
    }
}
