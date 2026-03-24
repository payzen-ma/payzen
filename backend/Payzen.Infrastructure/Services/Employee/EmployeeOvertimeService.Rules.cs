using Microsoft.EntityFrameworkCore;
using Payzen.Domain.Entities.Referentiel;
using Payzen.Domain.Enums;

namespace Payzen.Infrastructure.Services.Employee;

public partial class EmployeeOvertimeService
{
    private async Task<OvertimeRateRule?> FindBestRateRuleAsync(
        DateOnly date,
        OvertimeType overtimeType,
        TimeOnly? startTime,
        TimeOnly? endTime,
        decimal duration,
        CancellationToken ct)
    {
        var rules = await _db.OvertimeRateRules
            .Where(r => r.DeletedAt == null && r.IsActive)
            .ToListAsync(ct);

        if (rules.Count == 0)
            return null;

        var requestedSpecificFlags = overtimeType & ~OvertimeType.Standard;
        var requestIsPureStandard = requestedSpecificFlags == OvertimeType.None;

        bool IsRuleTypeApplicable(OvertimeType ruleAppliesTo)
        {
            var ruleSpecificFlags = ruleAppliesTo & ~OvertimeType.Standard;
            var ruleHasStandard = (ruleAppliesTo & OvertimeType.Standard) != 0;

            if (requestIsPureStandard)
                return ruleHasStandard && ruleSpecificFlags == OvertimeType.None;

            return (ruleSpecificFlags & requestedSpecificFlags) == ruleSpecificFlags;
        }

        var applicableRules = rules.Where(r => IsRuleTypeApplicable(r.AppliesTo)).ToList();

        if (applicableRules.Count == 0)
        {
            applicableRules = rules.Where(r =>
            {
                var ruleSpecificFlags = r.AppliesTo & ~OvertimeType.Standard;
                if (!requestIsPureStandard)
                    return (ruleSpecificFlags & requestedSpecificFlags) != OvertimeType.None;
                return (r.AppliesTo & OvertimeType.Standard) != 0;
            }).ToList();
        }

        if (applicableRules.Count == 0)
            return null;

        applicableRules = applicableRules
            .Where(r => (!r.MinimumDurationHours.HasValue || duration >= r.MinimumDurationHours.Value) &&
                        (!r.MaximumDurationHours.HasValue || duration <= r.MaximumDurationHours.Value))
            .ToList();

        if (applicableRules.Count == 0)
            return null;

        if (startTime.HasValue && endTime.HasValue)
        {
            applicableRules = applicableRules
                .Where(r =>
                {
                    if (r.TimeRangeType == TimeRangeType.AllDay)
                        return true;
                    if (!r.StartTime.HasValue || !r.EndTime.HasValue)
                        return false;
                    return CheckTimeRangeOverlap(
                        startTime.Value,
                        endTime.Value,
                        r.StartTime.Value,
                        r.EndTime.Value,
                        r.TimeRangeType);
                })
                .ToList();
        }

        if (applicableRules.Count == 0)
            return null;

        var dayOfWeek = (int)date.DayOfWeek;
        var dayBitmask = 1 << dayOfWeek;

        var dayFilteredRules = applicableRules
            .Where(r =>
            {
                if (!r.ApplicableDaysOfWeek.HasValue)
                    return true;

                var ruleSpecificFlags = r.AppliesTo & ~OvertimeType.Standard;
                var ruleIsOnlyStandardOrNight =
                    ((r.AppliesTo & (OvertimeType.Standard | OvertimeType.Night)) != 0) &&
                    (ruleSpecificFlags == OvertimeType.Night || ruleSpecificFlags == OvertimeType.None);

                if (ruleIsOnlyStandardOrNight)
                    return true;

                return (r.ApplicableDaysOfWeek.Value & dayBitmask) != 0;
            })
            .ToList();

        if (dayFilteredRules.Count == 0)
            return null;

        var rankedRules = dayFilteredRules
            .Select(r =>
            {
                var ruleSpecificFlags = r.AppliesTo & ~OvertimeType.Standard;
                var flagsCovered = CountFlags(r.AppliesTo & overtimeType);
                var exactMatchBonus =
                    (!requestIsPureStandard && ruleSpecificFlags == requestedSpecificFlags)
                        ? 1000
                        : 0;
                var specificityScore = CountFlags(ruleSpecificFlags) * 100;
                var standardPenalty =
                    (!requestIsPureStandard && (r.AppliesTo & OvertimeType.Standard) != 0)
                        ? -500
                        : 0;

                var timeRangeScore = 0;
                if (startTime.HasValue && endTime.HasValue && r.StartTime.HasValue && r.EndTime.HasValue &&
                    r.TimeRangeType != TimeRangeType.AllDay)
                {
                    var ruleDuration = r.EndTime.Value < r.StartTime.Value
                        ? (24 - (r.StartTime.Value - r.EndTime.Value).TotalHours)
                        : (r.EndTime.Value - r.StartTime.Value).TotalHours;
                    timeRangeScore = (int)(50 - Math.Abs(ruleDuration - (double)duration));
                }

                var totalScore = exactMatchBonus + specificityScore + standardPenalty + timeRangeScore - r.Priority;

                return new { Rule = r, TotalScore = totalScore, FlagsCovered = flagsCovered };
            })
            .OrderByDescending(x => x.TotalScore)
            .ThenBy(x => x.Rule.Priority)
            .ToList();

        return rankedRules[0].Rule;
    }

    private static int CountFlags(OvertimeType type)
    {
        if (type == OvertimeType.None)
            return 0;
        var count = 0;
        var value = (int)type;
        while (value != 0)
        {
            count++;
            value &= value - 1;
        }
        return count;
    }

    private static bool CheckTimeRangeOverlap(TimeOnly start1, TimeOnly end1, TimeOnly start2, TimeOnly end2, TimeRangeType rangeType)
    {
        if (rangeType == TimeRangeType.AllDay)
            return true;

        var range1CrossesMidnight = end1 < start1 || (start1 == end1 && start1 != new TimeOnly(0, 0));
        var range2CrossesMidnight = end2 < start2 || (start2 == end2 && start2 != new TimeOnly(0, 0));

        if (!range1CrossesMidnight && !range2CrossesMidnight)
            return start1 < end2 && end1 > start2;

        if (range1CrossesMidnight && range2CrossesMidnight)
            return true;

        if (range1CrossesMidnight)
            return start2 >= start1 || end2 <= end1;

        return start1 >= start2 || end1 <= end2;
    }
}
