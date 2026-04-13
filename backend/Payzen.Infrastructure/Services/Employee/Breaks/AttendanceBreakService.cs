using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Employee;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Employee;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Employee.Breaks;

public class AttendanceBreakService : IEmployeeAttendanceBreakService
{
    private readonly AppDbContext _db;
    public AttendanceBreakService(AppDbContext db) => _db = db;

    public async Task<ServiceResult<EmployeeAttendanceBreakReadDto>> GetByIdAsync(
        int id, CancellationToken ct = default)
    {
        var b = await _db.EmployeeAttendanceBreaks.FindAsync(new object[] { id }, ct);
        return b == null
            ? ServiceResult<EmployeeAttendanceBreakReadDto>.Fail("Pause introuvable.")
            : ServiceResult<EmployeeAttendanceBreakReadDto>.Ok(ToDto(b));
    }

    public async Task<ServiceResult<IEnumerable<EmployeeAttendanceBreakReadDto>>> GetByAttendanceAsync(
        int attendanceId, CancellationToken ct = default)
    {
        var list = await _db.EmployeeAttendanceBreaks
            .AsNoTracking()
            .Where(b => b.EmployeeAttendanceId == attendanceId)
            .OrderBy(b => b.BreakStart)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeAttendanceBreakReadDto>>.Ok(list.Select(ToDto));
    }

    public async Task<ServiceResult<EmployeeAttendanceBreakReadDto>> StartBreakAsync(
        StartBreakDto dto, int userId, CancellationToken ct = default)
    {
        var attendance = await _db.EmployeeAttendances.FindAsync(new object[] { dto.AttendanceId }, ct);
        if (attendance == null)
            return ServiceResult<EmployeeAttendanceBreakReadDto>.Fail("Pointage introuvable.");
        if (!attendance.CheckIn.HasValue)
            return ServiceResult<EmployeeAttendanceBreakReadDto>.Fail("Impossible de démarrer une pause avant le check-in.");
        var hasOpen = await _db.EmployeeAttendanceBreaks
            .AnyAsync(b => b.EmployeeAttendanceId == dto.AttendanceId && b.BreakEnd == null, ct);
        if (hasOpen)
            return ServiceResult<EmployeeAttendanceBreakReadDto>.Fail("Une pause est déjà en cours.");

        var breakRecord = new EmployeeAttendanceBreak
        {
            EmployeeAttendanceId = dto.AttendanceId,
            BreakStart = dto.BreakStart,
            BreakType = dto.BreakType,
            CreatedBy = userId
        };
        _db.EmployeeAttendanceBreaks.Add(breakRecord);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeAttendanceBreakReadDto>.Ok(ToDto(breakRecord));
    }

    public async Task<ServiceResult<EmployeeAttendanceBreakReadDto>> EndBreakAsync(
        int attendanceId, EndBreakDto dto, int userId, CancellationToken ct = default)
    {
        var openBreak = await _db.EmployeeAttendanceBreaks
            .OrderByDescending(b => b.BreakStart)
            .FirstOrDefaultAsync(b => b.EmployeeAttendanceId == attendanceId && b.BreakEnd == null, ct);
        if (openBreak == null)
            return ServiceResult<EmployeeAttendanceBreakReadDto>.Fail("Aucune pause ouverte.");
        if (dto.BreakEnd <= openBreak.BreakStart)
            return ServiceResult<EmployeeAttendanceBreakReadDto>.Fail("L'heure de fin doit être après l'heure de début.");

        openBreak.BreakEnd = dto.BreakEnd;
        openBreak.UpdatedBy = userId;
        openBreak.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        await RecalculateWorkedHoursAsync(attendanceId, ct);
        return ServiceResult<EmployeeAttendanceBreakReadDto>.Ok(ToDto(openBreak));
    }

    public async Task<ServiceResult<EmployeeAttendanceBreakReadDto>> UpdateAsync(
        int id, EmployeeAttendanceBreakUpdateDto dto, int updatedBy, CancellationToken ct = default)
    {
        var b = await _db.EmployeeAttendanceBreaks.FindAsync(new object[] { id }, ct);
        if (b == null)
            return ServiceResult<EmployeeAttendanceBreakReadDto>.Fail("Pause introuvable.");
        if (dto.BreakEnd <= dto.BreakStart)
            return ServiceResult<EmployeeAttendanceBreakReadDto>.Fail("L'heure de fin doit être après l'heure de début.");

        b.BreakStart = dto.BreakStart;
        b.BreakEnd = dto.BreakEnd;
        b.BreakType = dto.BreakType;
        b.UpdatedBy = updatedBy;
        b.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        await RecalculateWorkedHoursAsync(b.EmployeeAttendanceId, ct);
        return ServiceResult<EmployeeAttendanceBreakReadDto>.Ok(ToDto(b));
    }

    public async Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var b = await _db.EmployeeAttendanceBreaks.FindAsync(new object[] { id }, ct);
        if (b == null)
            return ServiceResult.Fail("Pause introuvable.");
        var attendanceId = b.EmployeeAttendanceId;
        _db.EmployeeAttendanceBreaks.Remove(b);
        await _db.SaveChangesAsync(ct);
        await RecalculateWorkedHoursAsync(attendanceId, ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<object>> GetTotalBreakTimeAsync(
        int attendanceId, CancellationToken ct = default)
    {
        var breaks = await _db.EmployeeAttendanceBreaks
            .Where(b => b.EmployeeAttendanceId == attendanceId && b.BreakEnd.HasValue)
            .ToListAsync(ct);

        var totalMinutes = breaks.Sum(b => (b.BreakEnd!.Value - b.BreakStart).TotalMinutes);
        return ServiceResult<object>.Ok(new
        {
            attendanceId,
            totalBreakMinutes = (int)totalMinutes,
            totalBreakTime = TimeSpan.FromMinutes(totalMinutes).ToString(@"hh\:mm"),
            breakCount = breaks.Count
        });
    }

    private async Task RecalculateWorkedHoursAsync(int attendanceId, CancellationToken ct)
    {
        var attendance = await _db.EmployeeAttendances
            .FirstOrDefaultAsync(a => a.Id == attendanceId, ct);
        if (attendance == null || !attendance.CheckIn.HasValue || !attendance.CheckOut.HasValue)
            return;

        var totalMinutes = (attendance.CheckOut.Value - attendance.CheckIn.Value).TotalMinutes;
        var breakMinutes = await _db.EmployeeAttendanceBreaks
            .Where(b => b.EmployeeAttendanceId == attendanceId && b.BreakEnd.HasValue)
            .SumAsync(b => EF.Functions.DateDiffMinute(b.BreakStart, b.BreakEnd!.Value), ct);

        attendance.BreakMinutesApplied = (int)breakMinutes;
        attendance.WorkedHours = (decimal)((totalMinutes - breakMinutes) / 60);
        await _db.SaveChangesAsync(ct);
    }

    private static EmployeeAttendanceBreakReadDto ToDto(EmployeeAttendanceBreak b) => new()
    {
        Id = b.Id,
        BreakStart = b.BreakStart,
        BreakEnd = b.BreakEnd,
        BreakType = b.BreakType ?? string.Empty,
        CreatedAt = b.CreatedAt,
        ModifiedAt = b.UpdatedAt
    };
}
