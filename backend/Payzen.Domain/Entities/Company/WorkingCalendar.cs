using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Company;

public class WorkingCalendar : BaseEntity
{
    public int CompanyId { get; set; }
    public int DayOfWeek { get; set; } // 0=Dimanche, 1=Lundi, ..., 6=Samedi
    public bool IsWorkingDay { get; set; } = true;
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    // Navigation properties
    public Company? Company { get; set; } = null!;
}
