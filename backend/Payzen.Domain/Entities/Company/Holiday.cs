using Payzen.Domain.Common;
using Payzen.Domain.Enums;
using Payzen.Domain.Entities.Referentiel;

namespace Payzen.Domain.Entities.Company;

public class Holiday : BaseEntity
{

    public required string NameFr
    {
        get; set;
    }
    public required string NameAr
    {
        get; set;
    }
    public required string NameEn
    {
        get; set;
    }

    public DateOnly HolidayDate
    {
        get; set;
    }
    public string? Description
    {
        get; set;
    }

    public int? CompanyId
    {
        get; set;
    }
    public int CountryId
    {
        get; set;
    }

    public HolidayScope Scope
    {
        get; set;
    }
    public string HolidayType { get; set; } = string.Empty;
    public bool IsMandatory { get; set; } = true;
    public bool IsPaid { get; set; } = true;

    public bool IsRecurring { get; set; } = false;
    public string? RecurrenceRule
    {
        get; set;
    }
    public int? Year
    {
        get; set;
    }

    public bool AffectPayroll { get; set; } = true;
    public bool AffectAttendance { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Company? Company
    {
        get; set;
    }
    public Country? Country
    {
        get; set;
    }
}
