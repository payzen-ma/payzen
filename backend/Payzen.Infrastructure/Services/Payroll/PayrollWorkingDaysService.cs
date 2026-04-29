using Microsoft.EntityFrameworkCore;
using Payzen.Application.Interfaces;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Payroll;

public class PayrollWorkingDaysService : IPayrollWorkingDaysService
{
    private readonly AppDbContext _db;

    public PayrollWorkingDaysService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> CalculateWorkingDaysAsync(
        int employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default
    )
    {
        var absences = await _db
            .EmployeeAbsences.AsNoTracking()
            .Where(a =>
                a.EmployeeId == employeeId
                && a.Status == AbsenceStatus.Approved
                && a.AbsenceDate >= startDate
                && a.AbsenceDate <= endDate
                && a.DeletedAt == null
            )
            .Select(a => new
            {
                a.EmployeeId,
                a.DurationType,
                a.StartTime,
                a.EndTime,
            })
            .ToListAsync(ct);

        var leaves = await _db
            .LeaveRequests.AsNoTracking()
            .Where(l =>
                l.EmployeeId == employeeId
                && l.Status == LeaveRequestStatus.Approved
                && l.StartDate <= endDate
                && l.EndDate >= startDate
                && l.DeletedAt == null
            )
            .Select(l => new
            {
                l.EmployeeId,
                l.StartDate,
                l.EndDate,
                l.WorkingDaysDeducted,
            })
            .ToListAsync(ct);

        var deductedDays = 0m;

        foreach (var absence in absences)
            deductedDays += ToAbsenceDays(absence.DurationType, absence.StartTime, absence.EndTime);

        foreach (var leave in leaves)
            deductedDays += ProrateLeaveDaysToPeriod(
                leave.WorkingDaysDeducted,
                leave.StartDate,
                leave.EndDate,
                startDate,
                endDate
            );

        var remaining = 26m - Math.Max(0m, deductedDays);
        var rounded = (int)Math.Round(remaining, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, 0, 26);
    }

    private static decimal ToAbsenceDays(AbsenceDurationType durationType, TimeOnly? startTime, TimeOnly? endTime)
    {
        return durationType switch
        {
            AbsenceDurationType.FullDay => 1m,
            AbsenceDurationType.HalfDay => 0.5m,
            AbsenceDurationType.Hourly => ToHourlyAbsenceDays(startTime, endTime),
            _ => 0m,
        };
    }

    private static decimal ToHourlyAbsenceDays(TimeOnly? startTime, TimeOnly? endTime)
    {
        if (!startTime.HasValue || !endTime.HasValue)
            return 0m;

        var hours = (decimal)(endTime.Value.ToTimeSpan() - startTime.Value.ToTimeSpan()).TotalHours;
        if (hours <= 0m)
            return 0m;

        // Convention: 1 jour ouvré = 8 heures.
        return Math.Min(1m, hours / 8m);
    }

    private static decimal ProrateLeaveDaysToPeriod(
        decimal workingDaysDeducted,
        DateOnly leaveStart,
        DateOnly leaveEnd,
        DateOnly periodStart,
        DateOnly periodEnd
    )
    {
        if (workingDaysDeducted <= 0m || leaveEnd < leaveStart)
            return 0m;

        var overlapStart = leaveStart > periodStart ? leaveStart : periodStart;
        var overlapEnd = leaveEnd < periodEnd ? leaveEnd : periodEnd;
        if (overlapEnd < overlapStart)
            return 0m;

        var totalSpanDays = leaveEnd.DayNumber - leaveStart.DayNumber + 1;
        if (totalSpanDays <= 0)
            return 0m;

        var overlapDays = overlapEnd.DayNumber - overlapStart.DayNumber + 1;
        return workingDaysDeducted * overlapDays / totalSpanDays;
    }
}
