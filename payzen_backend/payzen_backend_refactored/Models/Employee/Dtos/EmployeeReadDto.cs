using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeReadDto
    {
        public int Id { get; set; }
        public int? Matricule { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string CinNumber { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int? DepartementId { get; set; }
        public string? DepartementName { get; set; } = string.Empty;
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public int? StatusId { get; set; }
        public string? StatusName { get; set; } = string.Empty;
        public int? GenderId { get; set; }
        public int? NationalityId { get; set; }
        public int? EducationLevelId { get; set; }
        public int? MaritalStatusId { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CnssNumber { get; set; }
        public string? CimrNumber { get; set; }
        public string? CimrEmployeeRate { get; set; }
        public string? CimrCompanyRate { get; set; }
        public bool HasPrivateInsurance { get; set; } = false;
        public bool DisableAmo { get; set; } = false;
        public string? PrivateInsuranceNumber { get; set; }
        public decimal? PrivateInsuranceRate { get; set; }
        public string? JobPostionName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}