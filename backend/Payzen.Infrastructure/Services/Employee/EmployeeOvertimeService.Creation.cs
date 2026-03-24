using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Employee;
using Payzen.Domain.Entities.Employee;
using Payzen.Domain.Entities.Referentiel;
using Payzen.Domain.Enums;
using DomainEmployee = Payzen.Domain.Entities.Employee.Employee;

namespace Payzen.Infrastructure.Services.Employee;

public partial class EmployeeOvertimeService
{
    private async Task<ServiceResult<EmployeeOvertimeCreateOutcomeDto>> CreateOvertimesFullAsync(
        EmployeeOvertimeCreateDto dto, int createdBy, CancellationToken ct)
    {
        var isRhOrAdmin = await UserIsRhOrAdminAsync(createdBy, ct);

        var employee = await _db.Employees
            .Include(e => e.Company)
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId && e.DeletedAt == null, ct);
        if (employee == null)
            return ServiceResult<EmployeeOvertimeCreateOutcomeDto>.Fail("Employé non trouvé");

        decimal calculatedDuration;
        TimeOnly? effectiveStartTime = null;
        TimeOnly? effectiveEndTime = null;
        var crossesMidnight = false;

        switch (dto.EntryMode)
        {
            case OvertimeEntryMode.HoursRange:
                if (dto.StartTime == null || dto.EndTime == null)
                    return ServiceResult<EmployeeOvertimeCreateOutcomeDto>.Fail("StartTime et EndTime requis pour mode HoursRange");
                effectiveStartTime = dto.StartTime.Value;
                effectiveEndTime = dto.EndTime.Value;
                var start = dto.StartTime.Value;
                var end = dto.EndTime.Value;
                if (end < start || (end == start && start != new TimeOnly(0, 0)))
                {
                    var duration = TimeSpan.FromHours(24) - (start - end);
                    calculatedDuration = (decimal)duration.TotalHours;
                    crossesMidnight = true;
                }
                else
                {
                    calculatedDuration = (decimal)(end - start).TotalHours;
                    crossesMidnight = false;
                }
                break;

            case OvertimeEntryMode.DurationOnly:
                if (!dto.DurationInHours.HasValue || dto.DurationInHours <= 0)
                    return ServiceResult<EmployeeOvertimeCreateOutcomeDto>.Fail("DurationInHours requis pour mode DurationOnly");
                calculatedDuration = dto.DurationInHours.Value;
                break;

            case OvertimeEntryMode.FullDay:
                if (!dto.StandardDayHours.HasValue || dto.StandardDayHours <= 0)
                    return ServiceResult<EmployeeOvertimeCreateOutcomeDto>.Fail("StandardDayHours requis pour mode FullDay");
                calculatedDuration = dto.StandardDayHours.Value;
                break;

            default:
                return ServiceResult<EmployeeOvertimeCreateOutcomeDto>.Fail("Mode de saisie invalide");
        }

        var holiday = await _db.Holidays
            .Where(h => h.DeletedAt == null && h.IsActive)
            .Where(h => h.HolidayDate == dto.OvertimeDate)
            .Where(h => h.CompanyId == null || h.CompanyId == employee.CompanyId)
            .OrderByDescending(h => h.CompanyId)
            .FirstOrDefaultAsync(ct);

        var dayOfWeek = (int)dto.OvertimeDate.DayOfWeek;
        var workingCalendarDay = await _db.WorkingCalendars
            .Where(wc => wc.CompanyId == employee.CompanyId && wc.DayOfWeek == dayOfWeek && wc.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        var isWeeklyRest = workingCalendarDay != null && !workingCalendarDay.IsWorkingDay;

        var overtimeType = OvertimeType.None;
        if (holiday != null)
            overtimeType |= OvertimeType.PublicHoliday;
        else if (isWeeklyRest)
            overtimeType |= OvertimeType.WeeklyRest;
        else
            overtimeType = OvertimeType.Standard;

        if (dto.EntryMode == OvertimeEntryMode.HoursRange && effectiveStartTime != null && effectiveEndTime != null)
        {
            var nightStart = new TimeOnly(21, 0);
            var nightEnd = new TimeOnly(6, 0);
            if (CheckNightWorkOverlap(effectiveStartTime.Value, effectiveEndTime.Value, crossesMidnight, nightStart, nightEnd))
                overtimeType |= OvertimeType.Night;
        }

        var overtimesToCreate = new List<EmployeeOvertime>();
        var needsSplit = false;
        if (dto.EntryMode == OvertimeEntryMode.HoursRange && effectiveStartTime != null && effectiveEndTime != null)
        {
            var dayStart = new TimeOnly(6, 0);
            var dayEnd = new TimeOnly(21, 0);
            if (crossesMidnight)
                needsSplit = true;
            else
            {
                var start = effectiveStartTime.Value;
                var end = effectiveEndTime.Value;
                if ((start < dayStart && end > dayStart) || (start < dayEnd && end > dayEnd))
                    needsSplit = true;
            }
        }

        if (needsSplit && effectiveStartTime != null && effectiveEndTime != null)
        {
            var splitResult = await CreateSplitOvertimesAsync(
                dto, employee, createdBy, effectiveStartTime.Value, effectiveEndTime.Value, holiday?.Id, ct);
            overtimesToCreate.AddRange(splitResult);
        }
        else
        {
            var rateRule = await FindBestRateRuleAsync(
                dto.OvertimeDate, overtimeType, effectiveStartTime, effectiveEndTime, calculatedDuration, ct);
            var overtime = new EmployeeOvertime
            {
                EmployeeId = dto.EmployeeId,
                OverTimeType = overtimeType,
                EntryMode = dto.EntryMode,
                HolidayId = holiday?.Id,
                OvertimeDate = dto.OvertimeDate,
                StartTime = effectiveStartTime,
                EndTime = effectiveEndTime,
                CrossesMidnight = crossesMidnight,
                DurationInHours = calculatedDuration,
                StandardDayHours = dto.StandardDayHours,
                RateRuleId = rateRule?.Id,
                RateRuleCodeApplied = rateRule?.Code,
                RateRuleNameApplied = rateRule?.NameFr,
                RateMultiplierApplied = rateRule?.Multiplier ?? 1.00m,
                MultiplierCalculationDetails = rateRule != null ? CreateCalculationDetails(rateRule) : null,
                Status = isRhOrAdmin ? OvertimeStatus.Approved : OvertimeStatus.Draft,
                ApprovedBy = isRhOrAdmin ? createdBy : null,
                ApprovedAt = isRhOrAdmin ? DateTimeOffset.UtcNow : null,
                EmployeeComment = dto.EmployeeComment?.Trim(),
                CreatedBy = createdBy,
                CreatedAt = DateTimeOffset.UtcNow
            };
            overtimesToCreate.Add(overtime);
        }

        if (isRhOrAdmin)
        {
            foreach (var ot in overtimesToCreate)
            {
                ot.Status = OvertimeStatus.Approved;
                ot.ApprovedBy = createdBy;
                ot.ApprovedAt = DateTimeOffset.UtcNow;
            }
        }

        _db.EmployeeOvertimes.AddRange(overtimesToCreate);
        await _db.SaveChangesAsync(ct);

        var ids = overtimesToCreate.Select(x => x.Id).ToList();
        var reloaded = await _db.EmployeeOvertimes.AsNoTracking()
            .Include(o => o.Employee)
            .Include(o => o.Holiday)
            .Where(o => ids.Contains(o.Id))
            .OrderBy(o => o.SplitSequence ?? int.MaxValue)
            .ThenBy(o => o.Id)
            .ToListAsync(ct);

        var readDtos = new List<EmployeeOvertimeReadDto>();
        foreach (var x in reloaded)
        {
            var approver = await GetApproverNameAsync(x.ApprovedBy, ct);
            readDtos.Add(MapToReadDto(x, approver));
        }

        var splitBatchId = reloaded.FirstOrDefault()?.SplitBatchId;
        return ServiceResult<EmployeeOvertimeCreateOutcomeDto>.Ok(new EmployeeOvertimeCreateOutcomeDto
        {
            Overtimes = readDtos,
            SplitBatchId = splitBatchId
        });
    }

    private async Task<bool> UserIsRhOrAdminAsync(int userId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking()
            .Include(u => u.UsersRoles!).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, ct);
        return user?.UsersRoles?.Any(ur =>
            ur.Role != null &&
            (ur.Role.Name.Equals("RH", StringComparison.OrdinalIgnoreCase) ||
             ur.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase))) ?? false;
    }

    private async Task<List<EmployeeOvertime>> CreateSplitOvertimesAsync(
        EmployeeOvertimeCreateDto dto,
        DomainEmployee employee,
        int userId,
        TimeOnly startTime,
        TimeOnly endTime,
        int? holidayId,
        CancellationToken ct)
    {
        var splitBatchId = Guid.NewGuid();
        var overtimes = new List<EmployeeOvertime>();
        var dayStart = new TimeOnly(6, 0);
        var dayEnd = new TimeOnly(21, 0);
        var midnight = new TimeOnly(0, 0);
        var currentDate = dto.OvertimeDate;
        var currentStart = startTime;
        var sequence = 1;
        var crossesMidnight = endTime < startTime;
        var breakPoints = new List<TimeOnly> { dayStart, dayEnd, midnight }.OrderBy(t => t).ToList();

        while (true)
        {
            TimeOnly segmentEnd;
            var isLastSegment = false;
            var segmentDate = currentDate;

            if (crossesMidnight)
            {
                if (currentDate == dto.OvertimeDate)
                {
                    var nextBreak = breakPoints.Where(bp => bp > currentStart && bp <= dayEnd).FirstOrDefault();
                    segmentEnd = nextBreak == default ? new TimeOnly(23, 59, 59) : nextBreak;
                    if (segmentEnd > new TimeOnly(23, 0, 0))
                        segmentEnd = new TimeOnly(23, 59, 59);
                }
                else
                {
                    var nextBreak = breakPoints.Where(bp => bp > currentStart && bp <= dayEnd).FirstOrDefault();
                    if (nextBreak == default || nextBreak >= endTime)
                    {
                        segmentEnd = endTime;
                        isLastSegment = true;
                    }
                    else
                        segmentEnd = nextBreak;
                }
            }
            else
            {
                var nextBreak = breakPoints.Where(bp => bp > currentStart && bp < endTime).FirstOrDefault();
                if (nextBreak == default)
                {
                    segmentEnd = endTime;
                    isLastSegment = true;
                }
                else
                    segmentEnd = nextBreak;
            }

            decimal segmentDuration;
            if (currentStart > segmentEnd)
                segmentDuration = (decimal)(TimeSpan.FromHours(24) - (currentStart - segmentEnd)).TotalHours;
            else
                segmentDuration = (decimal)(segmentEnd - currentStart).TotalHours;

            if (segmentDuration <= 0)
                break;

            var isNightSegment = false;
            if (currentStart >= dayEnd)
                isNightSegment = true;
            else if (currentStart < dayStart && segmentEnd <= dayStart)
                isNightSegment = true;
            else if (currentStart >= dayStart && segmentEnd <= dayEnd)
                isNightSegment = false;
            else if (currentStart == midnight && segmentEnd < dayStart)
                isNightSegment = true;
            else if (currentStart == dayStart)
                isNightSegment = false;
            else
                isNightSegment = currentStart < dayStart || currentStart >= dayEnd;

            int? segmentHolidayId;
            if (segmentDate == dto.OvertimeDate)
                segmentHolidayId = holidayId;
            else
            {
                var nextDayHoliday = await _db.Holidays
                    .Where(h => h.DeletedAt == null && h.IsActive)
                    .Where(h => h.HolidayDate == segmentDate)
                    .Where(h => h.CompanyId == null || h.CompanyId == employee.CompanyId)
                    .OrderByDescending(h => h.CompanyId)
                    .FirstOrDefaultAsync(ct);
                segmentHolidayId = nextDayHoliday?.Id;
            }

            var segmentType = await DetermineOvertimeTypeAsync(segmentDate, employee.CompanyId, segmentHolidayId, ct);
            if (isNightSegment)
                segmentType |= OvertimeType.Night;

            var rateRule = await FindBestRateRuleAsync(segmentDate, segmentType, currentStart, segmentEnd, segmentDuration, ct);

            overtimes.Add(new EmployeeOvertime
            {
                EmployeeId = dto.EmployeeId,
                OverTimeType = segmentType,
                EntryMode = dto.EntryMode,
                HolidayId = segmentHolidayId,
                OvertimeDate = segmentDate,
                StartTime = currentStart,
                EndTime = segmentEnd,
                CrossesMidnight = false,
                DurationInHours = segmentDuration,
                StandardDayHours = dto.StandardDayHours,
                RateRuleId = rateRule?.Id,
                RateRuleCodeApplied = rateRule?.Code,
                RateRuleNameApplied = rateRule?.NameFr,
                RateMultiplierApplied = rateRule?.Multiplier ?? 1.00m,
                MultiplierCalculationDetails = rateRule != null ? CreateCalculationDetails(rateRule) : null,
                SplitBatchId = splitBatchId,
                SplitSequence = sequence,
                SplitTotalSegments = 0,
                Status = OvertimeStatus.Draft,
                EmployeeComment = dto.EmployeeComment?.Trim(),
                CreatedBy = userId,
                CreatedAt = DateTimeOffset.UtcNow
            });

            sequence++;
            if (isLastSegment)
                break;

            if (segmentEnd == new TimeOnly(23, 59, 59))
            {
                currentDate = currentDate.AddDays(1);
                currentStart = midnight;
            }
            else
                currentStart = segmentEnd;
        }

        foreach (var ot in overtimes)
            ot.SplitTotalSegments = overtimes.Count;

        return overtimes;
    }

    private async Task<OvertimeType> DetermineOvertimeTypeAsync(DateOnly date, int companyId, int? holidayId, CancellationToken ct)
    {
        var type = OvertimeType.None;
        if (holidayId.HasValue || await _db.Holidays.AnyAsync(h =>
                h.HolidayDate == date && h.DeletedAt == null && h.IsActive &&
                (h.CompanyId == null || h.CompanyId == companyId), ct))
        {
            type |= OvertimeType.PublicHoliday;
            return type;
        }

        var dow = (int)date.DayOfWeek;
        var workingCalendarDay = await _db.WorkingCalendars
            .Where(wc => wc.CompanyId == companyId && wc.DayOfWeek == dow && wc.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (workingCalendarDay != null && !workingCalendarDay.IsWorkingDay)
        {
            type |= OvertimeType.WeeklyRest;
            return type;
        }

        return OvertimeType.Standard;
    }

    private static bool CheckNightWorkOverlap(TimeOnly start, TimeOnly end, bool crossesMidnight, TimeOnly nightStart, TimeOnly nightEnd)
    {
        if (!crossesMidnight)
        {
            if (start >= nightStart) return true;
            if (end <= nightEnd) return true;
            if (start < nightEnd && end > nightEnd) return true;
            if (start < nightStart && end > nightStart) return true;
            return false;
        }
        return true;
    }

    private static string CreateCalculationDetails(OvertimeRateRule rule)
    {
        var details = new
        {
            RuleCode = rule.Code,
            RuleName = rule.NameFr,
            Multiplier = rule.Multiplier,
            AppliedOn = DateTime.UtcNow,
            Category = rule.Category
        };
        return JsonSerializer.Serialize(details);
    }
}
