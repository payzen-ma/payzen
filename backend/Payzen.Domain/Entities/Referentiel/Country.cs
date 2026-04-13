using Payzen.Domain.Common;
using Payzen.Domain.Entities.Company;

namespace Payzen.Domain.Entities.Referentiel;

/// <summary>Référentiel des pays pour adresses et nationalités</summary>
public class Country : BaseEntity
{
    public required string CountryName
    {
        get; set;
    }
    public string? CountryNameAr
    {
        get; set;
    }
    public required string CountryCode
    {
        get; set;
    }
    public required string CountryPhoneCode
    {
        get; set;
    }

    // Navigation properties
    public ICollection<City>? Cities
    {
        get; set;
    }
    public ICollection<Company.Company>? Companies
    {
        get; set;
    }
    public ICollection<Holiday>? Holidays
    {
        get; set;
    }
    public ICollection<Employee.EmployeeAddress>? EmployeeAddresses
    {
        get; set;
    }
    public ICollection<Employee.Employee>? Employees
    {
        get; set;
    }
}
