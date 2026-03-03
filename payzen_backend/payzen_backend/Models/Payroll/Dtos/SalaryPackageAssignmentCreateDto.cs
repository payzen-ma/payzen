using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Payroll.Dtos
{
    public class SalaryPackageAssignmentCreateDto
    {
        [Required(ErrorMessage = "Salary package is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Salary package id must be valid")]
        public int SalaryPackageId { get; set; }

        [Required(ErrorMessage = "Employee is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Employee id must be valid")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Contract is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Contract id must be valid")]
        public int ContractId { get; set; }

        [Required(ErrorMessage = "Effective date is required")]
        public DateTime EffectiveDate { get; set; }
    }
}
