using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Employee.Dtos;

namespace payzen_backend.Controllers.Employees
{
    [Route("api/employee-attendance-break")]
    [ApiController]
    [Authorize]
    public class EmployeeAttendanceBreakController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EmployeeAttendanceBreakController(AppDbContext db)
        {
            _db = db;
        }

        #region START / END BREAK (REAL FLOW)

        [HttpPost("start")]
        public async Task<ActionResult<EmployeeAttendanceBreakReadDto>> StartBreak(
            [FromBody] StartBreakDto dto)
        {
            var attendance = await GetAttendance(dto.AttendanceId);
            if (attendance == null)
                return BadRequest("Attendance not found.");

            if (!attendance.CheckIn.HasValue)
                return BadRequest("Cannot start a break before check-in.");

            if (attendance.CheckOut.HasValue && dto.BreakStart >= attendance.CheckOut.Value)
                return BadRequest("Break start must be before check-out.");

            if (await HasOpenBreak(dto.AttendanceId))
                return BadRequest("An open break already exists.");

            var breakRecord = new EmployeeAttendanceBreak
            {
                EmployeeAttendanceId = dto.AttendanceId,
                BreakStart = dto.BreakStart,
                BreakType = dto.BreakType,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = User.GetUserId()
            };

            _db.EmployeeAttendanceBreaks.Add(breakRecord);
            await _db.SaveChangesAsync();

            return Ok(ToDto(breakRecord));
        }

        [HttpPost("end/{attendanceId}")]
        public async Task<ActionResult<EmployeeAttendanceBreakReadDto>> EndBreak(
            int attendanceId,
            [FromBody] EndBreakDto dto)
        {
            var attendance = await GetAttendance(attendanceId);
            if (attendance == null)
                return BadRequest("Attendance not found.");

            var openBreak = await GetOpenBreak(attendanceId);
            if (openBreak == null)
                return BadRequest("No open break found.");

            if (dto.BreakEnd <= openBreak.BreakStart)
                return BadRequest("Break end must be after break start.");

            if (attendance.CheckOut.HasValue && dto.BreakEnd > attendance.CheckOut.Value)
                return BadRequest("Break end must be before check-out.");

            openBreak.BreakEnd = dto.BreakEnd;
            openBreak.ModifiedAt = DateTimeOffset.UtcNow;
            openBreak.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();
            await RecalculateWorkedHours(attendanceId);

            return Ok(ToDto(openBreak));
        }

        #endregion

        #region READ

        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeAttendanceBreakReadDto>> GetById(int id)
        {
            var record = await _db.EmployeeAttendanceBreaks
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (record == null)
                return NotFound();

            return Ok(ToDto(record));
        }

        [HttpGet("attendance/{attendanceId}")]
        public async Task<ActionResult<IEnumerable<EmployeeAttendanceBreakReadDto>>> GetByAttendance(
            int attendanceId)
        {
            var breaks = await _db.EmployeeAttendanceBreaks
                .AsNoTracking()
                .Where(b => b.EmployeeAttendanceId == attendanceId)
                .OrderBy(b => b.BreakStart)
                .ToListAsync();

            return Ok(breaks.Select(ToDto));
        }

        #endregion

        #region UPDATE / DELETE (ADMIN / MANUAL)

        [HttpPut("{id}")]
        public async Task<ActionResult<EmployeeAttendanceBreakReadDto>> UpdateBreak(
            int id,
            [FromBody] EmployeeAttendanceBreakCreateDto dto)
        {
            var record = await _db.EmployeeAttendanceBreaks.FindAsync(id);
            if (record == null)
                return NotFound();

            if (dto.BreakEnd <= dto.BreakStart)
                return BadRequest("Break end must be after break start.");

            record.BreakStart = dto.BreakStart;
            record.BreakEnd = dto.BreakEnd;
            record.BreakType = dto.BreakType;
            record.ModifiedAt = DateTimeOffset.UtcNow;
            record.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();
            await RecalculateWorkedHours(record.EmployeeAttendanceId);

            return Ok(ToDto(record));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBreak(int id)
        {
            var record = await _db.EmployeeAttendanceBreaks.FindAsync(id);
            if (record == null)
                return NotFound();

            var attendanceId = record.EmployeeAttendanceId;

            _db.EmployeeAttendanceBreaks.Remove(record);
            await _db.SaveChangesAsync();
            await RecalculateWorkedHours(attendanceId);

            return NoContent();
        }

        #endregion

        #region STATISTICS

        [HttpGet("attendance/{attendanceId}/total-break-time")]
        public async Task<ActionResult<object>> GetTotalBreakTime(int attendanceId)
        {
            var breaks = await _db.EmployeeAttendanceBreaks
                .Where(b =>
                    b.EmployeeAttendanceId == attendanceId &&
                    b.BreakEnd.HasValue)
                .ToListAsync();

            var totalMinutes = breaks.Sum(b =>
                (b.BreakEnd!.Value - b.BreakStart).TotalMinutes);

            return Ok(new
            {
                attendanceId,
                totalBreakMinutes = (int)totalMinutes,
                totalBreakTime = TimeSpan.FromMinutes(totalMinutes).ToString(@"hh\:mm"),
                breakCount = breaks.Count
            });
        }

        #endregion

        #region HELPERS

        private async Task<EmployeeAttendance?> GetAttendance(int attendanceId)
        {
            return await _db.EmployeeAttendances
                .FirstOrDefaultAsync(a => a.Id == attendanceId);
        }

        private async Task<bool> HasOpenBreak(int attendanceId)
        {
            return await _db.EmployeeAttendanceBreaks
                .AnyAsync(b =>
                    b.EmployeeAttendanceId == attendanceId &&
                    b.BreakEnd == null);
        }

        private async Task<EmployeeAttendanceBreak?> GetOpenBreak(int attendanceId)
        {
            return await _db.EmployeeAttendanceBreaks
                .OrderByDescending(b => b.BreakStart)
                .FirstOrDefaultAsync(b =>
                    b.EmployeeAttendanceId == attendanceId &&
                    b.BreakEnd == null);
        }

        private async Task RecalculateWorkedHours(int attendanceId)
        {
            var attendance = await _db.EmployeeAttendances
                .FirstOrDefaultAsync(a => a.Id == attendanceId);

            if (attendance == null ||
                !attendance.CheckIn.HasValue ||
                !attendance.CheckOut.HasValue)
                return;

            var totalMinutes =
                (attendance.CheckOut.Value - attendance.CheckIn.Value).TotalMinutes;

            var breakMinutes = await _db.EmployeeAttendanceBreaks
                .Where(b =>
                    b.EmployeeAttendanceId == attendanceId &&
                    b.BreakEnd.HasValue)
                .SumAsync(b =>
                    EF.Functions.DateDiffMinute(b.BreakStart, b.BreakEnd.Value));

            attendance.BreakMinutesApplied = (int)breakMinutes;
            attendance.WorkedHours =
                (decimal)((totalMinutes - breakMinutes) / 60);

            await _db.SaveChangesAsync();
        }

        private static EmployeeAttendanceBreakReadDto ToDto(
            EmployeeAttendanceBreak b)
        {
            return new EmployeeAttendanceBreakReadDto
            {
                Id = b.Id,
                BreakStart = b.BreakStart,
                BreakEnd = b.BreakEnd,
                BreakType = b.BreakType ?? string.Empty
            };
        }

        #endregion
    }
}
