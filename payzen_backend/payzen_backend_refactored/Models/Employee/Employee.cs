using payzen_backend.Models.Company;
using payzen_backend.Models.Referentiel;

namespace payzen_backend.Models.Employee
{
    public class Employee
    {
        public int Id { get; set; }
        public int? Matricule { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string CinNumber { get; set; }

        public required DateOnly DateOfBirth { get; set; }
        public required string Phone { get; set; }
        public required string Email { get; set; }
        public string? PersonalEmail { get; set; }
        public int CompanyId { get; set; }
        public int? ManagerId { get; set; }
        public int? DepartementId { get; set; }
        public int? StatusId { get; set; }
        public int? GenderId { get; set; }
        public int? NationalityId { get; set; }
        public int? EducationLevelId { get; set; }
        public int? MaritalStatusId { get; set; }
        public string? CnssNumber { get; set; }
        public string? CimrNumber { get; set; }
        public decimal? CimrEmployeeRate { get; set; }
        public decimal? CimrCompanyRate { get; set; }
        public bool HasPrivateInsurance { get; set; } = false;
        public string? PrivateInsuranceNumber { get; set; }
        public decimal? PrivateInsuranceRate { get; set; }
        public bool DisableAmo { get; set; } = false;

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public Company.Company? Company { get; set; }
        public Employee? Manager { get; set; }
        public Company.Departement? Departement { get; set; }

        // Nouvelles relations
        public Status? Status { get; set; }
        public Gender? Gender { get; set; }
        public Nationality? Nationality { get; set; }
        public EducationLevel? EducationLevel { get; set; }
        public MaritalStatus? MaritalStatus { get; set; }
        public int? CategoryId { get; set; }
        public EmployeeCategory? Category { get; set; }

        // Collections
        public ICollection<EmployeeContract> Contracts { get; set; } = new List<EmployeeContract>();
        public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();
        public ICollection<EmployeeSalary> Salaries { get; set; } = new List<EmployeeSalary>();
        public ICollection<EmployeeAddress> Addresses { get; set; } = new List<EmployeeAddress>();
        public ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();
        public ICollection<EmployeeAttendance> Attendances { get; set; } = new List<EmployeeAttendance>();
        public ICollection<EmployeeSpouse>? Spouses { get; set; }
        public ICollection<EmployeeChild>? Children { get; set; }
    }
}