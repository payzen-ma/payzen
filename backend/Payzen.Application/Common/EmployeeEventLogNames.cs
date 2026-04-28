namespace Payzen.Application.Common;

/// <summary>Noms d'événements persistés dans <c>EmployeeEventLogs</c> (alignés sur l'ancien monolithe).</summary>
public static class EmployeeEventLogNames
{
    /// <summary>Création d’employé (import Sage, etc.) — aligné ancien backend.</summary>
    public const string EmployeeCreated = "EmployeeCreated";

    public const string SalaryUpdated = "SalaryUpdated";
    public const string SalaryCreated = "SalaryCreated";
    public const string JobPositionChanged = "JobPositionChanged";
    public const string ContractCreated = "ContractCreated";
    public const string ContractTerminated = "ContractTerminated";
    public const string StatusChanged = "StatusChanged";
    public const string DepartmentChanged = "DepartmentChanged";
    public const string ManagerChanged = "ManagerChanged";
    public const string AddressUpdated = "AddressUpdated";
    public const string AddressCreated = "AddressCreated";

    public const string FirstNameChanged = "FirstNameChanged";
    public const string LastNameChanged = "LastNameChanged";
    public const string EmailChanged = "EmailChanged";
    public const string PhoneChanged = "PhoneChanged";
    public const string CinNumberChanged = "CinNumberChanged";
    public const string DateOfBirthChanged = "DateOfBirthChanged";
    public const string GenderChanged = "GenderChanged";
    public const string MaritalStatusChanged = "MaritalStatusChanged";
    public const string NationalityChanged = "NationalityChanged";
    public const string EducationLevelChanged = "EducationLevelChanged";
    public const string CnssNumberChanged = "CnssNumberChanged";
    public const string CimrNumberChanged = "CimrNumberChanged";
    public const string RibNumberChanged = "RibNumberChanged";
    public const string DocumentAdded = "DocumentAdded";
    public const string DocumentUpdated = "DocumentUpdated";
    public const string DocumentDeleted = "DocumentDeleted";
    public const string ContractTypeChanged = "ContractTypeChanged";
}
